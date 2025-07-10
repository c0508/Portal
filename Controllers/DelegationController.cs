using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Controllers;

[Authorize]
public class DelegationController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DelegationController> _logger;

    public DelegationController(ApplicationDbContext context, ILogger<DelegationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Delegation
    public async Task<IActionResult> Index()
    {
        var dashboardModel = await BuildDelegationDashboardAsync();
        return View(dashboardModel);
    }

    // GET: /Delegation/History
    public async Task<IActionResult> History(string filterStatus = "All", string filterType = "All", 
        DateTime? filterDateFrom = null, DateTime? filterDateTo = null, int page = 1)
    {
        var model = new DelegationHistoryViewModel
        {
            FilterStatus = filterStatus,
            FilterType = filterType,
            FilterDateFrom = filterDateFrom,
            FilterDateTo = filterDateTo,
            Page = page,
            PageSize = 20
        };

        var query = BuildDelegationHistoryQuery(filterStatus, filterType, filterDateFrom, filterDateTo);
        
        model.TotalItems = await query.CountAsync();
        model.History = await query
            .Skip((page - 1) * model.PageSize)
            .Take(model.PageSize)
            .Select(d => new DelegationHistoryItemViewModel
            {
                Id = d.Id,
                QuestionText = d.Question.QuestionText,
                CampaignName = d.CampaignAssignment.Campaign.Name,
                QuestionnaireTitle = d.CampaignAssignment.QuestionnaireVersion.Questionnaire.Title,
                FromUserName = d.FromUser.FullName,
                ToUserName = d.ToUser.FullName,
                Instructions = d.Instructions,
                Status = GetDelegationStatus(d),
                CreatedAt = d.CreatedAt,
                CompletedAt = d.CompletedAt,
                Deadline = d.CampaignAssignment.Campaign.Deadline,
                DelegationType = d.FromUserId == CurrentUserId ? "Given" : "Received",
                HasResponse = d.CampaignAssignment.Responses.Any(r => r.QuestionId == d.QuestionId),
                ResponseSummary = GetResponseSummary(d)
            })
            .ToListAsync();

        return View(model);
    }

    // GET: /Delegation/Timeline
    public async Task<IActionResult> Timeline(DateTime? filterDateFrom = null, DateTime? filterDateTo = null, 
        string filterUser = "All")
    {
        var model = new DelegationTimelineViewModel
        {
            FilterDateFrom = filterDateFrom,
            FilterDateTo = filterDateTo,
            FilterUser = filterUser
        };

        // Load team members for filter
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!
            })
            .ToListAsync();

        // Build timeline query
        var query = _context.Delegations
            .Include(d => d.Question)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.FromUser)
            .Include(d => d.ToUser)
            .Where(d => d.FromUserId == CurrentUserId || d.ToUserId == CurrentUserId);

        // Apply filters
        if (filterDateFrom.HasValue)
            query = query.Where(d => d.CreatedAt >= filterDateFrom.Value);
        if (filterDateTo.HasValue)
            query = query.Where(d => d.CreatedAt <= filterDateTo.Value);
        if (filterUser != "All")
            query = query.Where(d => d.FromUserId == filterUser || d.ToUserId == filterUser);

        var delegations = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

        // Build timeline items
        var timelineItems = new List<DelegationTimelineItemViewModel>();
        
        foreach (var delegation in delegations)
        {
            // Add creation event
            timelineItems.Add(new DelegationTimelineItemViewModel
            {
                Id = delegation.Id,
                ActionType = "Created",
                Description = $"Question delegated {(delegation.FromUserId == CurrentUserId ? "to" : "from")} {(delegation.FromUserId == CurrentUserId ? delegation.ToUser.FullName : delegation.FromUser.FullName)}",
                UserName = delegation.FromUser.FullName,
                Timestamp = delegation.CreatedAt,
                QuestionText = delegation.Question.QuestionText,
                CampaignName = delegation.CampaignAssignment.Campaign.Name,
                Status = GetDelegationStatus(delegation),
                Icon = "bi-share",
                Color = "primary"
            });

            // Add completion event if completed
            if (delegation.CompletedAt.HasValue)
            {
                timelineItems.Add(new DelegationTimelineItemViewModel
                {
                    Id = delegation.Id,
                    ActionType = "Completed",
                    Description = $"Delegation completed by {delegation.ToUser.FullName}",
                    UserName = delegation.ToUser.FullName,
                    Timestamp = delegation.CompletedAt.Value,
                    QuestionText = delegation.Question.QuestionText,
                    CampaignName = delegation.CampaignAssignment.Campaign.Name,
                    Status = GetDelegationStatus(delegation),
                    Icon = "bi-check-circle",
                    Color = "success"
                });
            }
        }

        model.TimelineItems = timelineItems.OrderByDescending(t => t.Timestamp).ToList();
        return View(model);
    }

    // GET: /Delegation/BulkDelegate/5
    public async Task<IActionResult> BulkDelegate(int id)
    {
        var assignment = await GetAssignmentWithAccessCheckAsync(id);
        if (assignment == null) return NotFound();

        var model = new BulkDelegationViewModel
        {
            CampaignAssignmentId = id,
            CampaignName = assignment.Campaign.Name,
            QuestionnaireTitle = assignment.QuestionnaireVersion.Questionnaire.Title
        };

        // Load questions through the questionnaire relationship
        model.Questions = await _context.Questions
            .Where(q => q.QuestionnaireId == assignment.QuestionnaireVersion.QuestionnaireId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new BulkDelegationQuestionViewModel
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                HelpText = q.HelpText,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                DisplayOrder = q.DisplayOrder
            })
            .ToListAsync();

        // Load team members
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive && u.Id != CurrentUserId)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!
            })
            .ToListAsync();

        return View(model);
    }

    // POST: /Delegation/BulkDelegate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelegate(BulkDelegationViewModel model)
    {
        var assignment = await GetAssignmentWithAccessCheckAsync(model.CampaignAssignmentId);
        if (assignment == null) return NotFound();

        // Check permissions
        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        var selectedQuestions = model.Questions.Where(q => q.IsSelected && !string.IsNullOrEmpty(q.ToUserId)).ToList();
        
        if (!selectedQuestions.Any())
        {
            TempData["Error"] = "Please select at least one question to delegate.";
            return await ReloadBulkDelegationView(model);
        }

        var delegations = new List<Delegation>();
        
        foreach (var question in selectedQuestions)
        {
            var delegation = new Delegation
            {
                CampaignAssignmentId = model.CampaignAssignmentId,
                QuestionId = question.QuestionId,
                FromUserId = CurrentUserId!,
                ToUserId = question.ToUserId!,
                Instructions = string.IsNullOrEmpty(question.Instructions) ? model.GlobalInstructions : question.Instructions,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            delegations.Add(delegation);
        }

        _context.Delegations.AddRange(delegations);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk delegation created: {Count} questions delegated by {UserId}", 
            delegations.Count, CurrentUserId);

        TempData["Success"] = $"{delegations.Count} questions delegated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Delegation/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var delegation = await _context.Delegations
            .Include(d => d.Question)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(d => d.FromUser)
            .Include(d => d.ToUser)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
                    .ThenInclude(r => r.FileUploads)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delegation == null) return NotFound();

        // Check access
        if (delegation.FromUserId != CurrentUserId && delegation.ToUserId != CurrentUserId)
        {
            return Forbid();
        }

        var model = new DelegationSummaryViewModel
        {
            Id = delegation.Id,
            QuestionId = delegation.QuestionId,
            CampaignAssignmentId = delegation.CampaignAssignmentId,
            QuestionText = delegation.Question.QuestionText,
            CampaignName = delegation.CampaignAssignment.Campaign.Name,
            QuestionnaireTitle = delegation.CampaignAssignment.QuestionnaireVersion.Questionnaire.Title,
            FromUserId = delegation.FromUserId,
            FromUserName = delegation.FromUser.FullName,
            ToUserId = delegation.ToUserId,
            ToUserName = delegation.ToUser.FullName,
            Instructions = delegation.Instructions,
            IsActive = delegation.IsActive,
            CreatedAt = delegation.CreatedAt,
            CompletedAt = delegation.CompletedAt,
            Deadline = delegation.CampaignAssignment.Campaign.Deadline
        };

        // Check if there's a response
        var response = delegation.CampaignAssignment.Responses.FirstOrDefault(r => r.QuestionId == delegation.QuestionId);
        if (response != null)
        {
            model.HasResponse = true;
            model.ResponseSummary = GetResponseDisplayText(response, delegation.Question.QuestionType);
        }

        return View(model);
    }

    // POST: /Delegation/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var delegation = await _context.Delegations
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delegation == null) return NotFound();

        // Check permissions - only the delegated user can complete
        if (delegation.ToUserId != CurrentUserId)
        {
            return Forbid();
        }

        // Check if there's a response for this question
        var hasResponse = delegation.CampaignAssignment.Responses.Any(r => r.QuestionId == delegation.QuestionId);
        if (!hasResponse)
        {
            TempData["Error"] = "Please provide an answer to the question before marking delegation as complete.";
            return RedirectToAction("AnswerQuestionnaire", "Response", new { id = delegation.CampaignAssignmentId });
        }

        delegation.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Delegation completed: {DelegationId} by {UserId}", delegation.Id, CurrentUserId);

        TempData["Success"] = "Delegation marked as completed!";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Delegation/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null) return NotFound();

        // Check permissions - only the delegator can cancel
        if (delegation.FromUserId != CurrentUserId)
        {
            return Forbid();
        }

        delegation.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Delegation cancelled: {DelegationId} by {UserId}", delegation.Id, CurrentUserId);

        TempData["Success"] = "Delegation cancelled successfully!";
        return RedirectToAction(nameof(Index));
    }

    #region Private Methods

    private async Task<DelegationDashboardViewModel> BuildDelegationDashboardAsync()
    {
        var model = new DelegationDashboardViewModel();

        // Get delegations received by current user
        var delegationsReceived = await _context.Delegations
            .Include(d => d.Question)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(d => d.FromUser)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
            .Where(d => d.ToUserId == CurrentUserId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        // Get delegations given by current user
        var delegationsGiven = await _context.Delegations
            .Include(d => d.Question)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(d => d.ToUser)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
            .Where(d => d.FromUserId == CurrentUserId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        // Convert to view models
        model.DelegationsReceived = delegationsReceived.Select(d => MapToSummaryViewModel(d, "Received")).ToList();
        model.DelegationsGiven = delegationsGiven.Select(d => MapToSummaryViewModel(d, "Given")).ToList();

        // Calculate statistics
        model.Statistics = new DelegationStatisticsViewModel
        {
            TotalDelegationsReceived = delegationsReceived.Count,
            TotalDelegationsGiven = delegationsGiven.Count,
            PendingReceived = delegationsReceived.Count(d => !d.CompletedAt.HasValue),
            PendingGiven = delegationsGiven.Count(d => !d.CompletedAt.HasValue),
            CompletedReceived = delegationsReceived.Count(d => d.CompletedAt.HasValue),
            CompletedGiven = delegationsGiven.Count(d => d.CompletedAt.HasValue),
            OverdueReceived = delegationsReceived.Count(d => !d.CompletedAt.HasValue && d.CampaignAssignment.Campaign.Deadline.HasValue && d.CampaignAssignment.Campaign.Deadline.Value < DateTime.Now),
            OverdueGiven = delegationsGiven.Count(d => !d.CompletedAt.HasValue && d.CampaignAssignment.Campaign.Deadline.HasValue && d.CampaignAssignment.Campaign.Deadline.Value < DateTime.Now)
        };

        // Load team members
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive && u.Id != CurrentUserId)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!
            })
            .ToListAsync();

        return model;
    }

    private DelegationSummaryViewModel MapToSummaryViewModel(Delegation delegation, string type)
    {
        var response = delegation.CampaignAssignment.Responses.FirstOrDefault(r => r.QuestionId == delegation.QuestionId);
        
        return new DelegationSummaryViewModel
        {
            Id = delegation.Id,
            QuestionId = delegation.QuestionId,
            CampaignAssignmentId = delegation.CampaignAssignmentId,
            QuestionText = delegation.Question.QuestionText,
            CampaignName = delegation.CampaignAssignment.Campaign.Name,
            QuestionnaireTitle = delegation.CampaignAssignment.QuestionnaireVersion.Questionnaire.Title,
            FromUserId = delegation.FromUserId,
            FromUserName = delegation.FromUser?.FullName ?? "Unknown",
            ToUserId = delegation.ToUserId,
            ToUserName = delegation.ToUser?.FullName ?? "Unknown",
            Instructions = delegation.Instructions,
            IsActive = delegation.IsActive,
            CreatedAt = delegation.CreatedAt,
            CompletedAt = delegation.CompletedAt,
            Deadline = delegation.CampaignAssignment.Campaign.Deadline,
            HasResponse = response != null,
            ResponseSummary = response != null ? GetResponseDisplayText(response, delegation.Question.QuestionType) : null
        };
    }

    private IQueryable<Delegation> BuildDelegationHistoryQuery(string filterStatus, string filterType, 
        DateTime? filterDateFrom, DateTime? filterDateTo)
    {
        var query = _context.Delegations
            .Include(d => d.Question)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(d => d.FromUser)
            .Include(d => d.ToUser)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
            .Where(d => d.FromUserId == CurrentUserId || d.ToUserId == CurrentUserId);

        // Apply filters
        if (filterType == "Received")
            query = query.Where(d => d.ToUserId == CurrentUserId);
        else if (filterType == "Given")
            query = query.Where(d => d.FromUserId == CurrentUserId);

        if (filterStatus != "All")
        {
            switch (filterStatus)
            {
                case "Pending":
                    query = query.Where(d => d.IsActive && !d.CompletedAt.HasValue);
                    break;
                case "Completed":
                    query = query.Where(d => d.CompletedAt.HasValue);
                    break;
                case "Overdue":
                    query = query.Where(d => d.IsActive && !d.CompletedAt.HasValue && 
                        d.CampaignAssignment.Campaign.Deadline.HasValue && 
                        d.CampaignAssignment.Campaign.Deadline.Value < DateTime.Now);
                    break;
                case "Cancelled":
                    query = query.Where(d => !d.IsActive);
                    break;
            }
        }

        if (filterDateFrom.HasValue)
            query = query.Where(d => d.CreatedAt >= filterDateFrom.Value);
        if (filterDateTo.HasValue)
            query = query.Where(d => d.CreatedAt <= filterDateTo.Value);

        return query.OrderByDescending(d => d.CreatedAt);
    }

    private async Task<CampaignAssignment?> GetAssignmentWithAccessCheckAsync(int assignmentId)
    {
        // First, get the assignment with basic access control
        var assignmentQuery = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
            .Where(ca => ca.Id == assignmentId);

        // Apply organization-level access control
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see assignments targeted to them
                assignmentQuery = assignmentQuery.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see assignments for campaigns they created
                assignmentQuery = assignmentQuery.Where(ca => ca.Campaign.OrganizationId == CurrentOrganizationId);
            }
            else
            {
                // If user is not a platform admin and doesn't have a recognized organization type, deny access
                return null;
            }
        }

        var assignment = await assignmentQuery.FirstOrDefaultAsync();
        
        if (assignment == null)
        {
            return null;
        }

        // Additional user-specific authorization checks
        if (!await HasUserAccessToAssignment(assignmentId, CurrentUserId))
        {
            return null;
        }

        return assignment;
    }

    /// <summary>
    /// Checks if the current user has access to the specified assignment
    /// </summary>
    private async Task<bool> HasUserAccessToAssignment(int assignmentId, string userId)
    {
        // Platform admins have access to everything
        if (IsPlatformAdmin)
        {
            return true;
        }

        // Check if user is the lead responder for this assignment
        var assignment = await _context.CampaignAssignments
            .Where(ca => ca.Id == assignmentId)
            .Select(ca => new { ca.LeadResponderId, ca.TargetOrganizationId, ca.Campaign.OrganizationId })
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            return false;
        }

        // Lead responder has access
        if (assignment.LeadResponderId == userId)
        {
            return true;
        }

        // Check organization-level access
        if (IsCurrentOrgSupplierType && assignment.TargetOrganizationId != CurrentOrganizationId)
        {
            return false;
        }

        if (IsCurrentOrgPlatformType && assignment.Campaign.OrganizationId != CurrentOrganizationId)
        {
            return false;
        }

        // Check if user has any delegations for this assignment
        var hasDelegations = await _context.Delegations
            .AnyAsync(d => d.CampaignAssignmentId == assignmentId && 
                          d.ToUserId == userId && 
                          d.IsActive);

        if (hasDelegations)
        {
            return true;
        }

        // Check if user has any question assignments for this assignment
        var hasQuestionAssignments = await _context.QuestionAssignments
            .AnyAsync(qa => qa.CampaignAssignmentId == assignmentId && 
                           qa.AssignedUserId == userId && 
                           qa.IsActive);

        if (hasQuestionAssignments)
        {
            return true;
        }

        // Check if user is a reviewer for this assignment
        var isReviewer = await _context.ReviewAssignments
            .AnyAsync(ra => ra.CampaignAssignmentId == assignmentId && ra.ReviewerId == userId);

        if (isReviewer)
        {
            return true;
        }

        return false;
    }

    private async Task<IActionResult> ReloadBulkDelegationView(BulkDelegationViewModel model)
    {
        // Reload team members and questions
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive && u.Id != CurrentUserId)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!
            })
            .ToListAsync();

        return View("BulkDelegate", model);
    }

    private static DelegationStatus GetDelegationStatus(Delegation delegation)
    {
        if (!delegation.IsActive) return DelegationStatus.Cancelled;
        if (delegation.CompletedAt.HasValue) return DelegationStatus.Completed;
        if (delegation.CampaignAssignment.Campaign.Deadline.HasValue && 
            delegation.CampaignAssignment.Campaign.Deadline.Value < DateTime.Now) 
            return DelegationStatus.Overdue;
        return DelegationStatus.Pending;
    }

    private static string? GetResponseSummary(Delegation delegation)
    {
        var response = delegation.CampaignAssignment.Responses.FirstOrDefault(r => r.QuestionId == delegation.QuestionId);
        return response != null ? GetResponseDisplayText(response, delegation.Question.QuestionType) : null;
    }

    private static string GetResponseDisplayText(Response response, QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.Text => response.TextValue ?? "",
            QuestionType.LongText => response.TextValue ?? "",
            QuestionType.Number => response.NumericValue?.ToString() ?? "",
            QuestionType.Date => response.DateValue?.ToString("yyyy-MM-dd") ?? "",
            QuestionType.YesNo => response.BooleanValue?.ToString() ?? "",
            QuestionType.Select => response.TextValue ?? "",
            QuestionType.Radio => response.TextValue ?? "",
            QuestionType.MultiSelect => response.SelectedValues ?? "",
            QuestionType.Checkbox => response.SelectedValues ?? "",
            QuestionType.FileUpload => response.FileUploads?.Count > 0 ? $"{response.FileUploads.Count} file(s)" : "No files",
            _ => "Unknown"
        };
    }

    #endregion
} 