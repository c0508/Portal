using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Services;

namespace ESGPlatform.Controllers;

[Authorize]
public class ReviewController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ReviewService _reviewService;

    public ReviewController(ApplicationDbContext context, ReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    #region Review Assignment Management (Lead Responders Only)

    // GET: Review/AssignReviewer/5 (Campaign Assignment ID)
    [Authorize(Policy = "LeadResponder")]
    public async Task<IActionResult> AssignReviewer(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await GetCampaignAssignmentWithAccessCheck(id.Value);
        if (assignment == null) return NotFound();

        // Only lead responder can assign reviewers
        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        var model = new AssignReviewerViewModel
        {
            CampaignAssignmentId = assignment.Id,
            CampaignName = assignment.Campaign.Name,
            QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown",
            TargetOrganizationName = assignment.TargetOrganization.Name
        };

        await LoadReviewerAssignmentData(model);
        return View(model);
    }

    // POST: Review/AssignQuestionReviewer
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "LeadResponder")]
    public async Task<IActionResult> AssignQuestionReviewer(AssignQuestionReviewerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var assignment = await GetCampaignAssignmentWithAccessCheck(request.CampaignAssignmentId);
        if (assignment == null) return NotFound();

        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        try
        {
            await _reviewService.AssignQuestionReviewerAsync(
                request.CampaignAssignmentId,
                request.QuestionId,
                request.ReviewerId,
                CurrentUserId,
                request.Instructions
            );

            TempData["Success"] = "Reviewer assigned successfully!";
            return RedirectToAction(nameof(AssignReviewer), new { id = request.CampaignAssignmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error assigning reviewer: {ex.Message}";
            return RedirectToAction(nameof(AssignReviewer), new { id = request.CampaignAssignmentId });
        }
    }

    // POST: Review/AssignSectionReviewer
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "LeadResponder")]
    public async Task<IActionResult> AssignSectionReviewer(AssignSectionReviewerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var assignment = await GetCampaignAssignmentWithAccessCheck(request.CampaignAssignmentId);
        if (assignment == null) return NotFound();

        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        try
        {
            await _reviewService.AssignSectionReviewerAsync(
                request.CampaignAssignmentId,
                request.SectionName,
                request.ReviewerId,
                CurrentUserId,
                request.Instructions
            );

            TempData["Success"] = "Section reviewer assigned successfully!";
            return RedirectToAction(nameof(AssignReviewer), new { id = request.CampaignAssignmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error assigning section reviewer: {ex.Message}";
            return RedirectToAction(nameof(AssignReviewer), new { id = request.CampaignAssignmentId });
        }
    }

    #endregion

    #region Reviewer Interface

    // GET: Review/MyReviews
    [Authorize(Policy = "Reviewer")]
    public async Task<IActionResult> MyReviews()
    {
        var reviewAssignments = await _reviewService.GetReviewAssignmentsForUserAsync(CurrentUserId);
        
        var model = new MyReviewsViewModel
        {
            ReviewAssignments = reviewAssignments.Select(ra => new ReviewAssignmentSummaryViewModel
            {
                Id = ra.Id,
                CampaignName = ra.CampaignAssignment.Campaign.Name,
                QuestionnaireTitle = ra.CampaignAssignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown",
                OrganizationName = ra.CampaignAssignment.TargetOrganization.Name,
                Scope = ra.Scope,
                QuestionText = ra.Question?.QuestionText,
                SectionName = ra.SectionName,
                Status = ra.Status,
                Instructions = ra.Instructions,
                CreatedAt = ra.CreatedAt,
                PendingCommentsCount = ra.Comments.Count(c => !c.IsResolved)
            }).ToList()
        };

        return View(model);
    }

    // GET: Review/ReviewQuestions/5 (Review Assignment ID)
    [Authorize(Policy = "Reviewer")]
    public async Task<IActionResult> ReviewQuestions(int? id)
    {
        if (id == null) return NotFound();

        var reviewAssignment = await _context.ReviewAssignments
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
                        .ThenInclude(q => q.Questions)
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.Responses)
                    .ThenInclude(r => r.Question)
            .Include(ra => ra.Question)
            .Include(ra => ra.Comments)
                .ThenInclude(c => c.Reviewer)
            .FirstOrDefaultAsync(ra => ra.Id == id);

        if (reviewAssignment == null) return NotFound();

        // Check if current user is the assigned reviewer
        if (reviewAssignment.ReviewerId != CurrentUserId)
        {
            return Forbid();
        }

        // Update status to InReview if it's still Pending
        if (reviewAssignment.Status == ReviewStatus.Pending)
        {
            reviewAssignment.Status = ReviewStatus.InReview;
            reviewAssignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var model = await BuildReviewQuestionsViewModel(reviewAssignment);
        return View(model);
    }

    // POST: Review/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Reviewer")]
    public async Task<IActionResult> AddComment(AddReviewCommentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verify the reviewer has permission for this review assignment
        var reviewAssignment = await _context.ReviewAssignments
            .FirstOrDefaultAsync(ra => ra.Id == request.ReviewAssignmentId);

        if (reviewAssignment == null || reviewAssignment.ReviewerId != CurrentUserId)
        {
            return Forbid();
        }

        try
        {
            await _reviewService.AddReviewCommentAsync(
                request.ReviewAssignmentId,
                request.ResponseId,
                CurrentUserId,
                request.Comment,
                request.ActionTaken,
                request.RequiresChange
            );

            TempData["Success"] = "Review comment added successfully!";
            return RedirectToAction(nameof(ReviewQuestions), new { id = request.ReviewAssignmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error adding comment: {ex.Message}";
            return RedirectToAction(nameof(ReviewQuestions), new { id = request.ReviewAssignmentId });
        }
    }

    // POST: Review/CompleteReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Reviewer")]
    public async Task<IActionResult> CompleteReview(int reviewAssignmentId, ReviewStatus finalStatus)
    {
        var reviewAssignment = await _context.ReviewAssignments
            .FirstOrDefaultAsync(ra => ra.Id == reviewAssignmentId);

        if (reviewAssignment == null || reviewAssignment.ReviewerId != CurrentUserId)
        {
            return Forbid();
        }

        try
        {
            reviewAssignment.Status = finalStatus;
            reviewAssignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string statusMessage = finalStatus switch
            {
                ReviewStatus.Approved => "Review approved successfully!",
                ReviewStatus.ChangesRequested => "Changes requested successfully!",
                ReviewStatus.Completed => "Review completed successfully!",
                _ => "Review status updated successfully!"
            };

            TempData["Success"] = statusMessage;
            return RedirectToAction(nameof(MyReviews));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error completing review: {ex.Message}";
            return RedirectToAction(nameof(ReviewQuestions), new { id = reviewAssignmentId });
        }
    }

    #endregion

    #region Review Status and Audit

    // GET: Review/AuditLog/5 (Campaign Assignment ID)
    [Authorize(Policy = "LeadResponder")]
    public async Task<IActionResult> AuditLog(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await GetCampaignAssignmentWithAccessCheck(id.Value);
        if (assignment == null) return NotFound();

        // Only lead responder or platform admin can view audit logs
        if (assignment.LeadResponderId != CurrentUserId && !IsPlatformAdmin)
        {
            return Forbid();
        }

        var auditLogs = await _reviewService.GetAuditLogAsync(id.Value);
        var reviewSummary = await _reviewService.GetReviewSummaryAsync(id.Value);

        var model = new ReviewAuditViewModel
        {
            CampaignAssignment = assignment,
            AuditLogs = auditLogs,
            ReviewSummary = reviewSummary
        };

        return View(model);
    }

    #endregion

    #region Helper Methods

    private async Task<CampaignAssignment?> GetCampaignAssignmentWithAccessCheck(int id)
    {
        IQueryable<CampaignAssignment> query = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
            .Include(ca => ca.LeadResponder);

        // Apply access control
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                query = query.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                query = query.Where(ca => ca.Campaign.OrganizationId == CurrentOrganizationId);
            }
        }

        return await query.FirstOrDefaultAsync(ca => ca.Id == id);
    }

    private async Task LoadReviewerAssignmentData(AssignReviewerViewModel model)
    {
        // Get available reviewers (users with Reviewer role in the organization)
        var reviewerUsers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive)
            .ToListAsync();

        // Filter to only users with Reviewer role
        var reviewers = new List<User>();
        foreach (var user in reviewerUsers)
        {
            var roles = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.UserId == user.Id && x.Name == "Reviewer")
                .ToListAsync();
            
            if (roles.Any())
            {
                reviewers.Add(user);
            }
        }

        model.AvailableReviewers = reviewers;

        // Get questions for this assignment
        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .FirstOrDefaultAsync(ca => ca.Id == model.CampaignAssignmentId);

        model.Questions = assignment?.QuestionnaireVersion?.Questionnaire?.Questions?.ToList() ?? new List<Question>();

        // Get existing review assignments
        model.ExistingAssignments = await _context.ReviewAssignments
            .Include(ra => ra.Reviewer)
            .Include(ra => ra.Question)
            .Where(ra => ra.CampaignAssignmentId == model.CampaignAssignmentId)
            .ToListAsync();

        // Get available sections
        model.AvailableSections = model.Questions
            .Where(q => !string.IsNullOrEmpty(q.Section))
            .Select(q => q.Section!)
            .Distinct()
            .ToList();
    }

    private async Task<ReviewQuestionsViewModel> BuildReviewQuestionsViewModel(ReviewAssignment reviewAssignment)
    {
        var questions = new List<Question>();
        var responses = new List<Response>();

        switch (reviewAssignment.Scope)
        {
            case ReviewScope.Question:
                if (reviewAssignment.Question != null)
                {
                    questions.Add(reviewAssignment.Question);
                    var response = reviewAssignment.CampaignAssignment.Responses
                        .FirstOrDefault(r => r.QuestionId == reviewAssignment.QuestionId);
                    if (response != null) responses.Add(response);
                }
                break;

            case ReviewScope.Section:
                questions = reviewAssignment.CampaignAssignment.QuestionnaireVersion?
                    .Questionnaire?.Questions?
                    .Where(q => q.Section == reviewAssignment.SectionName)
                    .ToList() ?? new List<Question>();
                responses = reviewAssignment.CampaignAssignment.Responses
                    .Where(r => questions.Any(q => q.Id == r.QuestionId))
                    .ToList();
                break;

            case ReviewScope.Assignment:
                questions = reviewAssignment.CampaignAssignment.QuestionnaireVersion?
                    .Questionnaire?.Questions?.ToList() ?? new List<Question>();
                responses = reviewAssignment.CampaignAssignment.Responses.ToList();
                break;
        }

        var questionReviews = questions.Select(q =>
        {
            var response = responses.FirstOrDefault(r => r.QuestionId == q.Id);
            var comments = reviewAssignment.Comments
                .Where(c => c.ResponseId == response?.Id)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            return new QuestionReviewViewModel
            {
                Question = q,
                Response = response,
                Comments = comments,
                HasResponse = response != null,
                ResponseText = response != null ? GetResponseDisplayText(response, q.QuestionType) : "No response provided"
            };
        }).ToList();

        return new ReviewQuestionsViewModel
        {
            ReviewAssignment = reviewAssignment,
            QuestionReviews = questionReviews,
            CampaignName = reviewAssignment.CampaignAssignment.Campaign.Name,
            QuestionnaireTitle = reviewAssignment.CampaignAssignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown"
        };
    }

    private string GetResponseDisplayText(Response response, QuestionType questionType)
    {
        // Check if response has any value
        bool hasValue = !string.IsNullOrEmpty(response.TextValue) || 
                       response.NumericValue.HasValue || 
                       response.DateValue.HasValue || 
                       response.BooleanValue.HasValue || 
                       !string.IsNullOrEmpty(response.SelectedValues);

        if (!hasValue && (response.FileUploads?.Count ?? 0) == 0)
            return "No response provided";

        switch (questionType)
        {
            case Models.Entities.QuestionType.Select:
            case Models.Entities.QuestionType.MultiSelect:
            case Models.Entities.QuestionType.Radio:
            case Models.Entities.QuestionType.Checkbox:
                return response.SelectedValues ?? "No selection";
            case Models.Entities.QuestionType.FileUpload:
                return $"{response.FileUploads?.Count ?? 0} file(s) uploaded";
            case Models.Entities.QuestionType.YesNo:
                return response.BooleanValue?.ToString() ?? "No response";
            case Models.Entities.QuestionType.Number:
                return response.NumericValue?.ToString() ?? "No response";
            case Models.Entities.QuestionType.Date:
                return response.DateValue?.ToString("yyyy-MM-dd") ?? "No response";
            default:
                return response.TextValue ?? "No response";
        }
    }

    #endregion
} 