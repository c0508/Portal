using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IResponseWorkflowService
{
    Task<bool> CanTransitionToStatusAsync(int responseId, ResponseStatus newStatus, string userId);
    Task<bool> TransitionToStatusAsync(int responseId, ResponseStatus newStatus, string userId, string? reason = null);
    Task<ResponseStatus> GetCurrentStatusAsync(int responseId);
    Task<List<ResponseStatusHistory>> GetStatusHistoryAsync(int responseId);
    Task<Dictionary<ResponseStatus, int>> GetStatusCountsAsync(int campaignAssignmentId);
    Task<bool> UpdateStatusForPrePopulationAsync(int responseId, string userId);
    Task<bool> UpdateStatusForAnswerAsync(int responseId, string userId);
    Task<bool> UpdateStatusForReviewAsync(int responseId, string userId);
}

public class ResponseWorkflowService : IResponseWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResponseWorkflowService> _logger;

    public ResponseWorkflowService(ApplicationDbContext context, ILogger<ResponseWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CanTransitionToStatusAsync(int responseId, ResponseStatus newStatus, string userId)
    {
        var response = await _context.Responses
            .Include(r => r.Question)
            .Include(r => r.CampaignAssignment)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null)
            return false;

        var currentStatus = response.Status;

        // Define valid status transitions based on business rules
        var validTransitions = new Dictionary<ResponseStatus, List<ResponseStatus>>
        {
            [ResponseStatus.NotStarted] = new List<ResponseStatus> 
            { 
                ResponseStatus.PrePopulated, 
                ResponseStatus.Draft, 
                ResponseStatus.Answered 
            },
            [ResponseStatus.PrePopulated] = new List<ResponseStatus> 
            { 
                ResponseStatus.Draft, 
                ResponseStatus.Answered 
            },
            [ResponseStatus.Draft] = new List<ResponseStatus> 
            { 
                ResponseStatus.Answered, 
                ResponseStatus.NotStarted 
            },
            [ResponseStatus.Answered] = new List<ResponseStatus> 
            { 
                ResponseStatus.Draft, 
                ResponseStatus.SubmittedForReview, 
                ResponseStatus.Final 
            },
            [ResponseStatus.SubmittedForReview] = new List<ResponseStatus> 
            { 
                ResponseStatus.UnderReview, 
                ResponseStatus.Answered 
            },
            [ResponseStatus.UnderReview] = new List<ResponseStatus> 
            { 
                ResponseStatus.ChangesRequested, 
                ResponseStatus.ReviewApproved 
            },
            [ResponseStatus.ChangesRequested] = new List<ResponseStatus> 
            { 
                ResponseStatus.Draft, 
                ResponseStatus.Answered 
            },
            [ResponseStatus.ReviewApproved] = new List<ResponseStatus> 
            { 
                ResponseStatus.Final 
            },
            [ResponseStatus.Final] = new List<ResponseStatus>() // Final status - no transitions allowed
        };

        if (!validTransitions.ContainsKey(currentStatus))
            return false;

        if (!validTransitions[currentStatus].Contains(newStatus))
            return false;

        // Additional business rules can be added here
        // For example: Check user permissions, question requirements, etc.

        return true;
    }

    public async Task<bool> TransitionToStatusAsync(int responseId, ResponseStatus newStatus, string userId, string? reason = null)
    {
        if (!await CanTransitionToStatusAsync(responseId, newStatus, userId))
            return false;

        var response = await _context.Responses.FindAsync(responseId);
        if (response == null)
            return false;

        var oldStatus = response.Status;

        // Update response status
        response.Status = newStatus;
        response.StatusUpdatedAt = DateTime.UtcNow;
        response.StatusUpdatedById = userId;
        response.UpdatedAt = DateTime.UtcNow;

        // Create status history record
        var statusHistory = new ResponseStatusHistory
        {
            ResponseId = responseId,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            ChangedById = userId,
            ChangeReason = reason,
            ChangedAt = DateTime.UtcNow
        };

        _context.ResponseStatusHistories.Add(statusHistory);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Response {ResponseId} status changed from {OldStatus} to {NewStatus} by user {UserId}", 
                responseId, oldStatus, newStatus, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition response {ResponseId} status from {OldStatus} to {NewStatus}", 
                responseId, oldStatus, newStatus);
            return false;
        }
    }

    public async Task<ResponseStatus> GetCurrentStatusAsync(int responseId)
    {
        var response = await _context.Responses
            .Where(r => r.Id == responseId)
            .Select(r => r.Status)
            .FirstOrDefaultAsync();

        return response;
    }

    public async Task<List<ResponseStatusHistory>> GetStatusHistoryAsync(int responseId)
    {
        return await _context.ResponseStatusHistories
            .Include(h => h.ChangedBy)
            .Where(h => h.ResponseId == responseId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<ResponseStatus, int>> GetStatusCountsAsync(int campaignAssignmentId)
    {
        var statusCounts = await _context.Responses
            .Where(r => r.CampaignAssignmentId == campaignAssignmentId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Ensure all status types are represented
        foreach (ResponseStatus status in Enum.GetValues<ResponseStatus>())
        {
            if (!statusCounts.ContainsKey(status))
                statusCounts[status] = 0;
        }

        return statusCounts;
    }

    public async Task<bool> UpdateStatusForPrePopulationAsync(int responseId, string userId)
    {
        var response = await _context.Responses.FindAsync(responseId);
        if (response == null)
            return false;

        // Only transition to PrePopulated if currently NotStarted
        if (response.Status == ResponseStatus.NotStarted)
        {
            return await TransitionToStatusAsync(responseId, ResponseStatus.PrePopulated, userId, "Answer pre-populated from previous period");
        }

        return true; // Already in a valid state
    }

    public async Task<bool> UpdateStatusForAnswerAsync(int responseId, string userId)
    {
        var response = await _context.Responses.FindAsync(responseId);
        if (response == null)
            return false;

        // Check if response has actual content
        bool hasContent = !string.IsNullOrWhiteSpace(response.TextValue) ||
                         response.NumericValue.HasValue ||
                         response.DateValue.HasValue ||
                         response.BooleanValue.HasValue ||
                         !string.IsNullOrWhiteSpace(response.SelectedValues);

        if (hasContent)
        {
            // Transition to Answered if currently in Draft, PrePopulated, or NotStarted
            if (response.Status == ResponseStatus.NotStarted ||
                response.Status == ResponseStatus.PrePopulated ||
                response.Status == ResponseStatus.Draft)
            {
                return await TransitionToStatusAsync(responseId, ResponseStatus.Answered, userId, "Response provided");
            }
        }
        else
        {
            // Transition to Draft if no content but was previously answered
            if (response.Status == ResponseStatus.Answered)
            {
                return await TransitionToStatusAsync(responseId, ResponseStatus.Draft, userId, "Response content cleared");
            }
        }

        return true;
    }

    public async Task<bool> UpdateStatusForReviewAsync(int responseId, string userId)
    {
        var response = await _context.Responses.FindAsync(responseId);
        if (response == null)
            return false;

        // Transition to UnderReview if currently SubmittedForReview
        if (response.Status == ResponseStatus.SubmittedForReview)
        {
            return await TransitionToStatusAsync(responseId, ResponseStatus.UnderReview, userId, "Review started");
        }

        return true;
    }
} 