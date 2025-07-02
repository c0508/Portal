using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Services;

public class ReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Review Assignment Management

    /// <summary>
    /// Assigns a reviewer to a specific question
    /// </summary>
    public async Task<ReviewAssignment> AssignQuestionReviewerAsync(
        int campaignAssignmentId,
        int questionId,
        string reviewerId,
        string assignedById,
        string? instructions = null)
    {
        // Check if this reviewer is already assigned to this question
        var existingAssignment = await _context.ReviewAssignments
            .FirstOrDefaultAsync(ra => ra.CampaignAssignmentId == campaignAssignmentId && 
                                      ra.ReviewerId == reviewerId && 
                                      ra.QuestionId == questionId &&
                                      ra.Scope == ReviewScope.Question);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException($"Reviewer is already assigned to this question. Existing assignment ID: {existingAssignment.Id}");
        }

        var reviewAssignment = new ReviewAssignment
        {
            CampaignAssignmentId = campaignAssignmentId,
            ReviewerId = reviewerId,
            Scope = ReviewScope.Question,
            QuestionId = questionId,
            SectionName = null, // Explicitly set to null for Question scope
            Status = ReviewStatus.Pending,
            Instructions = instructions,
            AssignedById = assignedById,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewAssignments.Add(reviewAssignment);
        
        try
        {
            await _context.SaveChangesAsync();
            
            // Log the assignment after successful save
            await LogReviewActionAsync(campaignAssignmentId, questionId, null, reviewAssignment.Id, 
                assignedById, "ReviewAssigned", null, "Pending", 
                $"Reviewer {reviewerId} assigned to question {questionId}");

            await _context.SaveChangesAsync();
            return reviewAssignment;
        }
        catch (Exception ex)
        {
            // Remove the added entity if save failed
            _context.ReviewAssignments.Remove(reviewAssignment);
            throw new InvalidOperationException($"Failed to save review assignment: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Assigns a reviewer to an entire section
    /// </summary>
    public async Task<ReviewAssignment> AssignSectionReviewerAsync(
        int campaignAssignmentId,
        string sectionName,
        string reviewerId,
        string assignedById,
        string? instructions = null)
    {
        var reviewAssignment = new ReviewAssignment
        {
            CampaignAssignmentId = campaignAssignmentId,
            ReviewerId = reviewerId,
            Scope = ReviewScope.Section,
            SectionName = sectionName,
            Status = ReviewStatus.Pending,
            Instructions = instructions,
            AssignedById = assignedById,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewAssignments.Add(reviewAssignment);
        
        // Log the assignment
        await LogReviewActionAsync(campaignAssignmentId, null, null, reviewAssignment.Id, 
            assignedById, "ReviewAssigned", null, "Pending", 
            $"Reviewer {reviewerId} assigned to section '{sectionName}'");

        await _context.SaveChangesAsync();
        return reviewAssignment;
    }

    /// <summary>
    /// Assigns a reviewer to the entire assignment
    /// </summary>
    public async Task<ReviewAssignment> AssignAssignmentReviewerAsync(
        int campaignAssignmentId,
        string reviewerId,
        string assignedById,
        string? instructions = null)
    {
        // Check if this reviewer is already assigned to this assignment with the same scope
        var existingAssignment = await _context.ReviewAssignments
            .FirstOrDefaultAsync(ra => ra.CampaignAssignmentId == campaignAssignmentId && 
                                      ra.ReviewerId == reviewerId && 
                                      ra.Scope == ReviewScope.Assignment);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException($"Reviewer is already assigned to this assignment. Existing assignment ID: {existingAssignment.Id}");
        }

        var reviewAssignment = new ReviewAssignment
        {
            CampaignAssignmentId = campaignAssignmentId,
            ReviewerId = reviewerId,
            Scope = ReviewScope.Assignment,
            QuestionId = null,  // Explicitly set to null for Assignment scope
            SectionName = null, // Explicitly set to null for Assignment scope
            Status = ReviewStatus.Pending,
            Instructions = instructions,
            AssignedById = assignedById,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewAssignments.Add(reviewAssignment);
        
        try
        {
            await _context.SaveChangesAsync();
            
            // Log the assignment after successful save
            await LogReviewActionAsync(campaignAssignmentId, null, null, reviewAssignment.Id, 
                assignedById, "ReviewAssigned", null, "Pending", 
                $"Reviewer {reviewerId} assigned to entire assignment");
                
            await _context.SaveChangesAsync();
            return reviewAssignment;
        }
        catch (Exception ex)
        {
            // Remove the added entity if save failed
            _context.ReviewAssignments.Remove(reviewAssignment);
            throw new InvalidOperationException($"Failed to save review assignment: {ex.Message}", ex);
        }
    }

    #endregion

    #region Review Comments and Feedback

    /// <summary>
    /// Adds a review comment to a response
    /// </summary>
    public async Task<ReviewComment> AddReviewCommentAsync(
        int reviewAssignmentId,
        int responseId,
        string reviewerId,
        string comment,
        ReviewStatus actionTaken,
        bool requiresChange = false)
    {
        var reviewComment = new ReviewComment
        {
            ReviewAssignmentId = reviewAssignmentId,
            ResponseId = responseId,
            ReviewerId = reviewerId,
            Comment = comment,
            ActionTaken = actionTaken,
            RequiresChange = requiresChange,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewComments.Add(reviewComment);

        // Update the review assignment status
        var reviewAssignment = await _context.ReviewAssignments
            .FirstOrDefaultAsync(ra => ra.Id == reviewAssignmentId);
        
        if (reviewAssignment != null)
        {
            var oldStatus = reviewAssignment.Status.ToString();
            reviewAssignment.Status = actionTaken;
            reviewAssignment.UpdatedAt = DateTime.UtcNow;

            // Log the status change
            await LogReviewActionAsync(reviewAssignment.CampaignAssignmentId, 
                reviewAssignment.QuestionId, responseId, reviewAssignmentId, 
                reviewerId, "CommentAdded", oldStatus, actionTaken.ToString(), 
                $"Comment added: {comment.Substring(0, Math.Min(comment.Length, 100))}...");
        }

        // Create or update response workflow
        await UpdateResponseWorkflowAsync(responseId, actionTaken, reviewerId);

        await _context.SaveChangesAsync();
        return reviewComment;
    }

    /// <summary>
    /// Resolves a review comment
    /// </summary>
    public async Task<bool> ResolveCommentAsync(int commentId, string resolvedById)
    {
        var comment = await _context.ReviewComments
            .FirstOrDefaultAsync(rc => rc.Id == commentId);

        if (comment == null) return false;

        comment.IsResolved = true;
        comment.ResolvedById = resolvedById;
        comment.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Review Status and Workflow

    /// <summary>
    /// Gets all review assignments for a user
    /// </summary>
    public async Task<List<ReviewAssignment>> GetReviewAssignmentsForUserAsync(string userId)
    {
        return await _context.ReviewAssignments
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(ra => ra.Question)
            .Include(ra => ra.Comments)
            .Where(ra => ra.ReviewerId == userId && ra.Status != ReviewStatus.Completed)
            .OrderBy(ra => ra.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all pending review assignments for a campaign assignment
    /// </summary>
    public async Task<List<ReviewAssignment>> GetPendingReviewsForAssignmentAsync(int campaignAssignmentId)
    {
        return await _context.ReviewAssignments
            .Include(ra => ra.Reviewer)
            .Include(ra => ra.Question)
            .Include(ra => ra.Comments)
            .Where(ra => ra.CampaignAssignmentId == campaignAssignmentId && 
                        ra.Status != ReviewStatus.Completed)
            .OrderBy(ra => ra.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Updates response workflow status
    /// </summary>
    private async Task UpdateResponseWorkflowAsync(int responseId, ReviewStatus newStatus, string? reviewerId = null)
    {
        var workflow = await _context.ResponseWorkflows
            .FirstOrDefaultAsync(rw => rw.ResponseId == responseId);

        if (workflow == null)
        {
            // Create new workflow
            workflow = new ResponseWorkflow
            {
                ResponseId = responseId,
                CurrentStatus = newStatus,
                CurrentReviewerId = reviewerId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ResponseWorkflows.Add(workflow);
        }
        else
        {
            // Update existing workflow
            workflow.CurrentStatus = newStatus;
            workflow.CurrentReviewerId = reviewerId;
            workflow.UpdatedAt = DateTime.UtcNow;
        }

        // Update timing fields based on status
        switch (newStatus)
        {
            case ReviewStatus.InReview:
                if (workflow.ReviewStartedAt == null)
                    workflow.ReviewStartedAt = DateTime.UtcNow;
                break;
            case ReviewStatus.ChangesRequested:
                workflow.RevisionCount++;
                break;
            case ReviewStatus.Approved:
            case ReviewStatus.Completed:
                workflow.ReviewCompletedAt = DateTime.UtcNow;
                break;
        }
    }

    #endregion

    #region Review Audit Logging

    /// <summary>
    /// Logs a review action for audit purposes
    /// </summary>
    private async Task LogReviewActionAsync(
        int campaignAssignmentId,
        int? questionId,
        int? responseId,
        int? reviewAssignmentId,
        string userId,
        string action,
        string? fromStatus,
        string? toStatus,
        string? details)
    {
        var auditLog = new ReviewAuditLog
        {
            CampaignAssignmentId = campaignAssignmentId,
            QuestionId = questionId,
            ResponseId = responseId,
            ReviewAssignmentId = reviewAssignmentId,
            UserId = userId,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewAuditLogs.Add(auditLog);
    }

    /// <summary>
    /// Gets audit log for a campaign assignment
    /// </summary>
    public async Task<List<ReviewAuditLog>> GetAuditLogAsync(int campaignAssignmentId)
    {
        return await _context.ReviewAuditLogs
            .Include(ral => ral.User)
            .Include(ral => ral.Question)
            .Include(ral => ral.Response)
            .Where(ral => ral.CampaignAssignmentId == campaignAssignmentId)
            .OrderByDescending(ral => ral.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if a user has review permissions for a specific response
    /// </summary>
    public async Task<bool> HasReviewPermissionAsync(string userId, int responseId)
    {
        var response = await _context.Responses
            .Include(r => r.Question)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null) return false;

        // Check if user has any review assignments for this response
        return await _context.ReviewAssignments
            .AnyAsync(ra => ra.ReviewerId == userId && 
                           ra.CampaignAssignmentId == response.CampaignAssignmentId &&
                           (ra.Scope == ReviewScope.Assignment || // Full assignment review
                            (ra.Scope == ReviewScope.Question && ra.QuestionId == response.QuestionId) || // Specific question
                            (ra.Scope == ReviewScope.Section && ra.SectionName == response.Question.Section))); // Section review
    }

    /// <summary>
    /// Gets summary statistics for review assignments
    /// </summary>
    public async Task<ReviewSummaryStats> GetReviewSummaryAsync(int campaignAssignmentId)
    {
        var reviews = await _context.ReviewAssignments
            .Where(ra => ra.CampaignAssignmentId == campaignAssignmentId)
            .ToListAsync();

        return new ReviewSummaryStats
        {
            TotalReviews = reviews.Count,
            PendingReviews = reviews.Count(r => r.Status == ReviewStatus.Pending),
            InProgressReviews = reviews.Count(r => r.Status == ReviewStatus.InReview),
            ApprovedReviews = reviews.Count(r => r.Status == ReviewStatus.Approved),
            ChangesRequestedReviews = reviews.Count(r => r.Status == ReviewStatus.ChangesRequested),
            CompletedReviews = reviews.Count(r => r.Status == ReviewStatus.Completed)
        };
    }

    #endregion
} 