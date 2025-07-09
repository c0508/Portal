using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Controllers;

[Authorize(Policy = "CampaignManagerOrHigher")] // Same access as Question Changes
public class ActivityHistoryController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityHistoryController> _logger;

    public ActivityHistoryController(ApplicationDbContext context, ILogger<ActivityHistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /ActivityHistory
    public async Task<IActionResult> Index(ActivityHistoryFilterViewModel filter, int page = 1, int pageSize = 20)
    {
        var activities = new List<ActivityHistoryItemViewModel>();

        // Get response changes
        var responseChanges = await GetResponseChanges(filter);
        activities.AddRange(responseChanges);

        // Get delegation activities
        var delegationActivities = await GetDelegationActivities(filter);
        activities.AddRange(delegationActivities);

        // Get review activities
        var reviewActivities = await GetReviewActivities(filter);
        activities.AddRange(reviewActivities);

        // Get question assignment changes
        var assignmentChanges = await GetQuestionAssignmentChanges(filter);
        activities.AddRange(assignmentChanges);

        // Sort by timestamp descending
        activities = activities.OrderByDescending(a => a.Timestamp).ToList();

        // Apply pagination
        var totalCount = activities.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (page - 1) * pageSize;
        var pagedActivities = activities.Skip(skip).Take(pageSize).ToList();

        // Populate filter dropdowns
        await PopulateFilterDropdowns(filter);

        var model = new ActivityHistoryViewModel
        {
            Activities = pagedActivities,
            Filter = filter,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };

        return View(model);
    }

    // GET: /ActivityHistory/Question/5
    public async Task<IActionResult> Question(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Questionnaire)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound();
        }

        var activities = new List<ActivityHistoryItemViewModel>();

        // Get response changes for this question
        var filter = new ActivityHistoryFilterViewModel { QuestionId = id };
        var responseChanges = await GetResponseChanges(filter);
        activities.AddRange(responseChanges);

        // Get delegation activities for this question
        var delegationActivities = await GetDelegationActivities(filter);
        activities.AddRange(delegationActivities);

        // Get review activities for this question
        var reviewActivities = await GetReviewActivities(filter);
        activities.AddRange(reviewActivities);

        // Get assignment changes for this question
        var assignmentChanges = await GetQuestionAssignmentChanges(filter);
        activities.AddRange(assignmentChanges);

        // Sort by timestamp descending
        activities = activities.OrderByDescending(a => a.Timestamp).ToList();

        var model = new QuestionActivityHistoryViewModel
        {
            QuestionId = question.Id,
            QuestionText = question.QuestionText,
            QuestionnaireName = question.Questionnaire.Title,
            Section = question.Section,
            Activities = activities,
            TotalResponseChanges = activities.Count(a => a.ActivityType == "ResponseChange"),
            TotalDelegations = activities.Count(a => a.ActivityType == "Delegation"),
            TotalReviewActions = activities.Count(a => a.ActivityType == "Review"),
            TotalAssignmentChanges = activities.Count(a => a.ActivityType == "QuestionAssignment"),
            LastActivity = activities.FirstOrDefault()?.Timestamp
        };

        return View(model);
    }

    private async Task<List<ActivityHistoryItemViewModel>> GetResponseChanges(ActivityHistoryFilterViewModel filter)
    {
        var query = _context.ResponseChanges
            .Include(rc => rc.Response)
                .ThenInclude(r => r.Question)
                    .ThenInclude(q => q.Questionnaire)
            .Include(rc => rc.Response)
                .ThenInclude(r => r.CampaignAssignment)
                    .ThenInclude(ca => ca.Campaign)
            .Include(rc => rc.ChangedBy)
            .AsQueryable();

        // Apply filters
        if (filter.QuestionId.HasValue)
        {
            query = query.Where(rc => rc.Response.QuestionId == filter.QuestionId.Value);
        }

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(rc => rc.Response.CampaignAssignment.CampaignId == filter.CampaignId.Value);
        }

        if (filter.QuestionnaireId.HasValue)
        {
            query = query.Where(rc => rc.Response.Question.QuestionnaireId == filter.QuestionnaireId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Section))
        {
            query = query.Where(rc => rc.Response.Question.Section == filter.Section);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(rc => rc.ChangedById == filter.UserId);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(rc => rc.ChangedAt.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(rc => rc.ChangedAt.Date <= filter.ToDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            query = query.Where(rc => rc.Response.Question.QuestionText.Contains(filter.SearchText) ||
                                     rc.Response.CampaignAssignment.Campaign.Name.Contains(filter.SearchText));
        }

        if (filter.ActivityType != "All" && filter.ActivityType == "ResponseChange")
        {
            // Already filtered to response changes
        }
        else if (filter.ActivityType != "All" && filter.ActivityType != "ResponseChange")
        {
            return new List<ActivityHistoryItemViewModel>(); // Return empty for other types
        }

        var changes = await query.OrderByDescending(rc => rc.ChangedAt).ToListAsync();

        return changes.Select(rc => new ActivityHistoryItemViewModel
        {
            Id = rc.Id,
            ActivityType = "ResponseChange",
            Action = "Updated",
            Description = $"Changed response to question: {rc.Response.Question.QuestionText}",
            UserName = $"{rc.ChangedBy?.FirstName} {rc.ChangedBy?.LastName}",
            UserEmail = rc.ChangedBy?.Email ?? "",
            Timestamp = rc.ChangedAt,
            QuestionId = rc.Response.QuestionId,
            QuestionText = rc.Response.Question.QuestionText,
            CampaignName = rc.Response.CampaignAssignment.Campaign.Name,
            QuestionnaireName = rc.Response.Question.Questionnaire.Title,
            Section = rc.Response.Question.Section,
            OldValue = rc.OldValue,
            NewValue = rc.NewValue,
            Reason = rc.ChangeReason,
            IconClass = "bi-pencil-square",
            BadgeClass = "bg-primary",
            BadgeText = "Response"
        }).ToList();
    }

    private async Task<List<ActivityHistoryItemViewModel>> GetDelegationActivities(ActivityHistoryFilterViewModel filter)
    {
        var activities = new List<ActivityHistoryItemViewModel>();

        // Get delegation creation events
        var delegationsQuery = _context.Delegations
            .Include(d => d.Question)
                .ThenInclude(q => q.Questionnaire)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.FromUser)
            .Include(d => d.ToUser)
            .AsQueryable();

        // Apply filters
        if (filter.QuestionId.HasValue)
        {
            delegationsQuery = delegationsQuery.Where(d => d.QuestionId == filter.QuestionId.Value);
        }

        if (filter.CampaignId.HasValue)
        {
            delegationsQuery = delegationsQuery.Where(d => d.CampaignAssignment.CampaignId == filter.CampaignId.Value);
        }

        if (filter.QuestionnaireId.HasValue)
        {
            delegationsQuery = delegationsQuery.Where(d => d.Question.QuestionnaireId == filter.QuestionnaireId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Section))
        {
            delegationsQuery = delegationsQuery.Where(d => d.Question.Section == filter.Section);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            delegationsQuery = delegationsQuery.Where(d => d.FromUserId == filter.UserId || d.ToUserId == filter.UserId);
        }

        if (filter.FromDate.HasValue)
        {
            delegationsQuery = delegationsQuery.Where(d => d.CreatedAt.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            delegationsQuery = delegationsQuery.Where(d => d.CreatedAt.Date <= filter.ToDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            delegationsQuery = delegationsQuery.Where(d => d.Question.QuestionText.Contains(filter.SearchText) ||
                                                           d.CampaignAssignment.Campaign.Name.Contains(filter.SearchText));
        }

        if (filter.ActivityType != "All" && filter.ActivityType == "Delegation")
        {
            // Already filtered to delegations
        }
        else if (filter.ActivityType != "All" && filter.ActivityType != "Delegation")
        {
            return activities; // Return empty for other types
        }

        var delegations = await delegationsQuery.OrderByDescending(d => d.CreatedAt).ToListAsync();

        // Add delegation creation events
        foreach (var delegation in delegations)
        {
            activities.Add(new ActivityHistoryItemViewModel
            {
                Id = delegation.Id,
                ActivityType = "Delegation",
                Action = "Delegated",
                Description = $"Delegated question to {delegation.ToUser?.FirstName} {delegation.ToUser?.LastName}: {delegation.Question.QuestionText}",
                UserName = $"{delegation.FromUser?.FirstName} {delegation.FromUser?.LastName}",
                UserEmail = delegation.FromUser?.Email ?? "",
                Timestamp = delegation.CreatedAt,
                QuestionId = delegation.QuestionId,
                QuestionText = delegation.Question.QuestionText,
                CampaignName = delegation.CampaignAssignment.Campaign.Name,
                QuestionnaireName = delegation.Question.Questionnaire.Title,
                Section = delegation.Question.Section,
                Details = delegation.Instructions,
                IconClass = "bi-share",
                BadgeClass = "bg-warning",
                BadgeText = "Delegation"
            });

            // Add completion events if completed
            if (delegation.CompletedAt.HasValue)
            {
                activities.Add(new ActivityHistoryItemViewModel
                {
                    Id = delegation.Id + 10000, // Offset to avoid ID conflicts
                    ActivityType = "Delegation",
                    Action = "Completed",
                    Description = $"Completed delegated question: {delegation.Question.QuestionText}",
                    UserName = $"{delegation.ToUser?.FirstName} {delegation.ToUser?.LastName}",
                    UserEmail = delegation.ToUser?.Email ?? "",
                    Timestamp = delegation.CompletedAt.Value,
                    QuestionId = delegation.QuestionId,
                    QuestionText = delegation.Question.QuestionText,
                    CampaignName = delegation.CampaignAssignment.Campaign.Name,
                    QuestionnaireName = delegation.Question.Questionnaire.Title,
                    Section = delegation.Question.Section,
                    IconClass = "bi-check-circle",
                    BadgeClass = "bg-success",
                    BadgeText = "Completed"
                });
            }
        }

        return activities;
    }

    private async Task<List<ActivityHistoryItemViewModel>> GetReviewActivities(ActivityHistoryFilterViewModel filter)
    {
        var query = _context.ReviewAuditLogs
            .Include(ral => ral.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(ral => ral.Question)
                .ThenInclude(q => q.Questionnaire)
            .Include(ral => ral.User)
            .AsQueryable();

        // Apply filters
        if (filter.QuestionId.HasValue)
        {
            query = query.Where(ral => ral.QuestionId == filter.QuestionId.Value);
        }

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(ral => ral.CampaignAssignment.CampaignId == filter.CampaignId.Value);
        }

        if (filter.QuestionnaireId.HasValue && filter.QuestionId.HasValue)
        {
            query = query.Where(ral => ral.Question != null && ral.Question.QuestionnaireId == filter.QuestionnaireId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Section) && filter.QuestionId.HasValue)
        {
            query = query.Where(ral => ral.Question != null && ral.Question.Section == filter.Section);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(ral => ral.UserId == filter.UserId);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(ral => ral.CreatedAt.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(ral => ral.CreatedAt.Date <= filter.ToDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            query = query.Where(ral => ral.CampaignAssignment.Campaign.Name.Contains(filter.SearchText) ||
                                      (ral.Question != null && ral.Question.QuestionText.Contains(filter.SearchText)));
        }

        if (filter.ActivityType != "All" && filter.ActivityType == "Review")
        {
            // Already filtered to reviews
        }
        else if (filter.ActivityType != "All" && filter.ActivityType != "Review")
        {
            return new List<ActivityHistoryItemViewModel>(); // Return empty for other types
        }

        var auditLogs = await query.OrderByDescending(ral => ral.CreatedAt).ToListAsync();

        return auditLogs.Select(ral => new ActivityHistoryItemViewModel
        {
            Id = ral.Id,
            ActivityType = "Review",
            Action = ral.Action,
            Description = GetReviewDescription(ral),
            UserName = $"{ral.User?.FirstName} {ral.User?.LastName}",
            UserEmail = ral.User?.Email ?? "",
            Timestamp = ral.CreatedAt,
            QuestionId = ral.QuestionId,
            QuestionText = ral.Question?.QuestionText,
            CampaignName = ral.CampaignAssignment.Campaign.Name,
            QuestionnaireName = ral.Question?.Questionnaire.Title,
            Section = ral.Question?.Section,
            OldValue = ral.FromStatus,
            NewValue = ral.ToStatus,
            Details = ral.Details,
            IconClass = GetReviewIconClass(ral.Action),
            BadgeClass = GetReviewBadgeClass(ral.Action),
            BadgeText = "Review"
        }).ToList();
    }

    private async Task<List<ActivityHistoryItemViewModel>> GetQuestionAssignmentChanges(ActivityHistoryFilterViewModel filter)
    {
        var query = _context.QuestionAssignmentChanges
            .Include(qac => qac.Question)
                .ThenInclude(q => q.Questionnaire)
            .Include(qac => qac.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(qac => qac.ChangedBy)
            .Include(qac => qac.OldAssignedUser)
            .Include(qac => qac.NewAssignedUser)
            .AsQueryable();

        // Apply filters
        if (filter.QuestionId.HasValue)
        {
            query = query.Where(qac => qac.QuestionId == filter.QuestionId.Value);
        }

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(qac => qac.CampaignAssignment.CampaignId == filter.CampaignId.Value);
        }

        if (filter.QuestionnaireId.HasValue && filter.QuestionId.HasValue)
        {
            query = query.Where(qac => qac.Question != null && qac.Question.QuestionnaireId == filter.QuestionnaireId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Section))
        {
            // For section assignments or questions in specific sections
            query = query.Where(qac => qac.SectionName == filter.Section || 
                                      (qac.Question != null && qac.Question.Section == filter.Section));
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(qac => qac.ChangedById == filter.UserId || 
                                      qac.OldAssignedUserId == filter.UserId || 
                                      qac.NewAssignedUserId == filter.UserId);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(qac => qac.ChangedAt.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(qac => qac.ChangedAt.Date <= filter.ToDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            query = query.Where(qac => (qac.Question != null && qac.Question.QuestionText.Contains(filter.SearchText)) ||
                                      qac.CampaignAssignment.Campaign.Name.Contains(filter.SearchText) ||
                                      (qac.SectionName != null && qac.SectionName.Contains(filter.SearchText)));
        }

        if (filter.ActivityType != "All" && filter.ActivityType == "QuestionAssignment")
        {
            // Already filtered to question assignment changes
        }
        else if (filter.ActivityType != "All" && filter.ActivityType != "QuestionAssignment")
        {
            return new List<ActivityHistoryItemViewModel>(); // Return empty for other types
        }

        var changes = await query.OrderByDescending(qac => qac.ChangedAt).ToListAsync();

        return changes.Select(qac => new ActivityHistoryItemViewModel
        {
            Id = qac.Id,
            ActivityType = "QuestionAssignment",
            Action = qac.ChangeType, // "Created", "Modified", "Removed"
            Description = GetAssignmentChangeDescription(qac),
            UserName = $"{qac.ChangedBy?.FirstName} {qac.ChangedBy?.LastName}",
            UserEmail = qac.ChangedBy?.Email ?? "",
            Timestamp = qac.ChangedAt,
            QuestionId = qac.QuestionId,
            QuestionText = qac.Question?.QuestionText ?? $"Section: {qac.SectionName}",
            CampaignName = qac.CampaignAssignment.Campaign.Name,
            QuestionnaireName = qac.Question?.Questionnaire.Title ?? "N/A",
            Section = qac.SectionName ?? qac.Question?.Section,
            OldValue = GetAssignmentOldValue(qac),
            NewValue = GetAssignmentNewValue(qac),
            Reason = qac.ChangeReason,
            IconClass = GetAssignmentIconClass(qac.ChangeType),
            BadgeClass = GetAssignmentBadgeClass(qac.ChangeType),
            BadgeText = "Assignment"
        }).ToList();
    }

    private string GetAssignmentChangeDescription(QuestionAssignmentChange qac)
    {
        var target = qac.Question?.QuestionText ?? $"Section '{qac.SectionName}'";
        
        return qac.ChangeType switch
        {
            "Created" => $"Assigned {target} to {qac.NewAssignedUser?.FirstName} {qac.NewAssignedUser?.LastName}",
            "Modified" => $"Modified assignment for {target}",
            "Removed" => $"Removed assignment for {target} from {qac.OldAssignedUser?.FirstName} {qac.OldAssignedUser?.LastName}",
            _ => $"Changed assignment for {target}"
        };
    }

    private string GetAssignmentOldValue(QuestionAssignmentChange qac)
    {
        if (qac.ChangeType == "Created") return "Unassigned";
        if (qac.OldAssignedUser != null) return $"{qac.OldAssignedUser.FirstName} {qac.OldAssignedUser.LastName}";
        return "Unknown";
    }

    private string GetAssignmentNewValue(QuestionAssignmentChange qac)
    {
        if (qac.ChangeType == "Removed") return "Unassigned";
        if (qac.NewAssignedUser != null) return $"{qac.NewAssignedUser.FirstName} {qac.NewAssignedUser.LastName}";
        return "Unknown";
    }

    private string GetAssignmentIconClass(string changeType)
    {
        return changeType switch
        {
            "Created" => "bi-person-plus",
            "Modified" => "bi-person-gear",
            "Removed" => "bi-person-dash",
            _ => "bi-person"
        };
    }

    private string GetAssignmentBadgeClass(string changeType)
    {
        return changeType switch
        {
            "Created" => "bg-success",
            "Modified" => "bg-info",
            "Removed" => "bg-warning",
            _ => "bg-secondary"
        };
    }

    private async Task PopulateFilterDropdowns(ActivityHistoryFilterViewModel filter)
    {
        // Get available campaigns
        filter.AvailableCampaigns = await _context.Campaigns
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Get available questionnaires
        filter.AvailableQuestionnaires = await _context.Questionnaires
            .OrderBy(q => q.Title)
            .ToListAsync();

        // Get available sections
        filter.AvailableSections = await _context.Questions
            .Where(q => !string.IsNullOrEmpty(q.Section))
            .Select(q => q.Section!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        // Get available users
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
        
        filter.AvailableUsers = users.Select(u => new UserSummaryViewModel
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}",
            Email = u.Email!
        }).ToList();

        // Available actions
        filter.AvailableActions = new List<string>
        {
            "Updated", "Delegated", "Completed", "ReviewAssigned", "CommentAdded", "StatusChanged"
        };
    }

    private string GetReviewDescription(ReviewAuditLog auditLog)
    {
        return auditLog.Action switch
        {
            "ReviewAssigned" => $"Review assigned for: {auditLog.Question?.QuestionText ?? "Assignment"}",
            "CommentAdded" => $"Comment added to: {auditLog.Question?.QuestionText ?? "Assignment"}",
            "StatusChanged" => $"Review status changed from {auditLog.FromStatus} to {auditLog.ToStatus}",
            _ => $"{auditLog.Action} - {auditLog.Question?.QuestionText ?? auditLog.CampaignAssignment.Campaign.Name}"
        };
    }

    private string GetReviewIconClass(string action)
    {
        return action switch
        {
            "ReviewAssigned" => "bi-person-plus",
            "CommentAdded" => "bi-chat-text",
            "StatusChanged" => "bi-arrow-repeat",
            _ => "bi-eye"
        };
    }

    private string GetReviewBadgeClass(string action)
    {
        return action switch
        {
            "ReviewAssigned" => "bg-info",
            "CommentAdded" => "bg-secondary",
            "StatusChanged" => "bg-warning",
            _ => "bg-light text-dark"
        };
    }
} 