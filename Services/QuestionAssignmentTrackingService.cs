using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public class QuestionAssignmentTrackingService : IQuestionAssignmentTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionAssignmentTrackingService> _logger;

    public QuestionAssignmentTrackingService(ApplicationDbContext context, ILogger<QuestionAssignmentTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TrackAssignmentCreatedAsync(QuestionAssignment assignment, string createdById, string? reason = null)
    {
        try
        {
            _logger.LogInformation("Starting assignment creation tracking for Assignment ID {AssignmentId}", assignment.Id);

            var change = new QuestionAssignmentChange
            {
                CampaignAssignmentId = assignment.CampaignAssignmentId,
                QuestionId = assignment.QuestionId,
                SectionName = assignment.SectionName,
                ChangedById = createdById,
                OldAssignedUserId = null, // No previous assignment
                NewAssignedUserId = assignment.AssignedUserId,
                OldInstructions = null,
                NewInstructions = assignment.Instructions,
                ChangeType = "Created",
                ChangeReason = reason ?? "Assignment created",
                ChangedAt = DateTime.UtcNow
            };

            _context.QuestionAssignmentChanges.Add(change);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Tracked assignment creation for Assignment ID {AssignmentId}", assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track assignment creation for Assignment ID {AssignmentId}", assignment.Id);
        }
    }

    public async Task TrackAssignmentModifiedAsync(QuestionAssignment oldAssignment, QuestionAssignment newAssignment, string changedById, string? reason = null)
    {
        try
        {
            _logger.LogInformation("Starting assignment modification tracking for Assignment ID {AssignmentId}", newAssignment.Id);

            // Check if anything actually changed
            bool hasChanges = oldAssignment.AssignedUserId != newAssignment.AssignedUserId ||
                             oldAssignment.Instructions != newAssignment.Instructions;

            if (!hasChanges)
            {
                _logger.LogInformation("❌ No changes detected for Assignment ID {AssignmentId} - tracking skipped", newAssignment.Id);
                return;
            }

            var change = new QuestionAssignmentChange
            {
                CampaignAssignmentId = newAssignment.CampaignAssignmentId,
                QuestionId = newAssignment.QuestionId,
                SectionName = newAssignment.SectionName,
                ChangedById = changedById,
                OldAssignedUserId = oldAssignment.AssignedUserId,
                NewAssignedUserId = newAssignment.AssignedUserId,
                OldInstructions = oldAssignment.Instructions,
                NewInstructions = newAssignment.Instructions,
                ChangeType = "Modified",
                ChangeReason = reason ?? "Assignment modified",
                ChangedAt = DateTime.UtcNow
            };

            _context.QuestionAssignmentChanges.Add(change);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Tracked assignment modification for Assignment ID {AssignmentId}", newAssignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track assignment modification for Assignment ID {AssignmentId}", newAssignment.Id);
        }
    }

    public async Task TrackAssignmentRemovedAsync(QuestionAssignment assignment, string removedById, string? reason = null)
    {
        try
        {
            _logger.LogInformation("Starting assignment removal tracking for Assignment ID {AssignmentId}", assignment.Id);

            var change = new QuestionAssignmentChange
            {
                CampaignAssignmentId = assignment.CampaignAssignmentId,
                QuestionId = assignment.QuestionId,
                SectionName = assignment.SectionName,
                ChangedById = removedById,
                OldAssignedUserId = assignment.AssignedUserId,
                NewAssignedUserId = null, // Assignment removed
                OldInstructions = assignment.Instructions,
                NewInstructions = null,
                ChangeType = "Removed",
                ChangeReason = reason ?? "Assignment removed",
                ChangedAt = DateTime.UtcNow
            };

            _context.QuestionAssignmentChanges.Add(change);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Tracked assignment removal for Assignment ID {AssignmentId}", assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track assignment removal for Assignment ID {AssignmentId}", assignment.Id);
        }
    }
} 