using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ESGPlatform.Services;

public interface IResponseChangeTrackingService
{
    Task TrackResponseChangeAsync(Response originalResponse, Response newResponse, string userId, string reason);
    Task TrackDelegationChangeAsync(string action, Delegation delegation, string userId, string? reason = null);
    Task TrackReviewStatusChangeAsync(string action, int assignmentId, int? questionId, int? responseId, string userId, string fromStatus, string toStatus, string? details = null);
}

public class ResponseChangeTrackingService : IResponseChangeTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResponseChangeTrackingService> _logger;

    public ResponseChangeTrackingService(ApplicationDbContext context, ILogger<ResponseChangeTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TrackResponseChangeAsync(Response originalResponse, Response newResponse, string userId, string reason)
    {
        try
        {
            _logger.LogInformation("Starting change tracking for Response ID {ResponseId}", newResponse.Id);
            _logger.LogInformation("Original: Text='{OldText}', Numeric={OldNumeric}, Date={OldDate}, Boolean={OldBoolean}, Selected='{OldSelected}'", 
                originalResponse.TextValue, originalResponse.NumericValue, originalResponse.DateValue, originalResponse.BooleanValue, originalResponse.SelectedValues);
            _logger.LogInformation("New: Text='{NewText}', Numeric={NewNumeric}, Date={NewDate}, Boolean={NewBoolean}, Selected='{NewSelected}'", 
                newResponse.TextValue, newResponse.NumericValue, newResponse.DateValue, newResponse.BooleanValue, newResponse.SelectedValues);

            var changes = new List<ResponseChange>();

            // Track TextValue changes
            if (originalResponse.TextValue != newResponse.TextValue)
            {
                _logger.LogInformation("TextValue changed from '{OldValue}' to '{NewValue}'", originalResponse.TextValue, newResponse.TextValue);
                changes.Add(new ResponseChange
                {
                    ResponseId = newResponse.Id,
                    ChangedById = userId,
                    OldValue = originalResponse.TextValue,
                    NewValue = newResponse.TextValue,
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                });
            }

            // Track NumericValue changes
            if (originalResponse.NumericValue != newResponse.NumericValue)
            {
                _logger.LogInformation("NumericValue changed from '{OldValue}' to '{NewValue}'", originalResponse.NumericValue, newResponse.NumericValue);
                changes.Add(new ResponseChange
                {
                    ResponseId = newResponse.Id,
                    ChangedById = userId,
                    OldValue = originalResponse.NumericValue?.ToString(),
                    NewValue = newResponse.NumericValue?.ToString(),
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                });
            }

            // Track DateValue changes
            if (originalResponse.DateValue != newResponse.DateValue)
            {
                _logger.LogInformation("DateValue changed from '{OldValue}' to '{NewValue}'", originalResponse.DateValue, newResponse.DateValue);
                changes.Add(new ResponseChange
                {
                    ResponseId = newResponse.Id,
                    ChangedById = userId,
                    OldValue = originalResponse.DateValue?.ToString("yyyy-MM-dd"),
                    NewValue = newResponse.DateValue?.ToString("yyyy-MM-dd"),
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                });
            }

            // Track BooleanValue changes
            if (originalResponse.BooleanValue != newResponse.BooleanValue)
            {
                _logger.LogInformation("BooleanValue changed from '{OldValue}' to '{NewValue}'", originalResponse.BooleanValue, newResponse.BooleanValue);
                changes.Add(new ResponseChange
                {
                    ResponseId = newResponse.Id,
                    ChangedById = userId,
                    OldValue = originalResponse.BooleanValue?.ToString(),
                    NewValue = newResponse.BooleanValue?.ToString(),
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                });
            }

            // Track SelectedValues changes
            if (originalResponse.SelectedValues != newResponse.SelectedValues)
            {
                _logger.LogInformation("SelectedValues changed from '{OldValue}' to '{NewValue}'", originalResponse.SelectedValues, newResponse.SelectedValues);
                changes.Add(new ResponseChange
                {
                    ResponseId = newResponse.Id,
                    ChangedById = userId,
                    OldValue = originalResponse.SelectedValues,
                    NewValue = newResponse.SelectedValues,
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                });
            }

            if (changes.Any())
            {
                _context.ResponseChanges.AddRange(changes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Tracked {ChangeCount} response changes for Response ID {ResponseId}", changes.Count, newResponse.Id);
            }
            else
            {
                _logger.LogWarning("❌ No changes detected for Response ID {ResponseId} - tracking skipped", newResponse.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking response changes for Response ID {ResponseId}", newResponse.Id);
        }
    }

    public async Task TrackDelegationChangeAsync(string action, Delegation delegation, string userId, string? reason = null)
    {
        try
        {
            var auditLog = new ReviewAuditLog
            {
                CampaignAssignmentId = delegation.CampaignAssignmentId,
                QuestionId = delegation.QuestionId,
                UserId = userId,
                Action = action,
                Details = JsonSerializer.Serialize(new
                {
                    DelegationId = delegation.Id,
                    FromUserId = delegation.FromUserId,
                    ToUserId = delegation.ToUserId,
                    Instructions = delegation.Instructions,
                    Reason = reason
                }),
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Tracked delegation change: {Action} for Question ID {QuestionId}", action, delegation.QuestionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking delegation change for Question ID {QuestionId}", delegation.QuestionId);
        }
    }

    public async Task TrackReviewStatusChangeAsync(string action, int assignmentId, int? questionId, int? responseId, string userId, string fromStatus, string toStatus, string? details = null)
    {
        try
        {
            var auditLog = new ReviewAuditLog
            {
                CampaignAssignmentId = assignmentId,
                QuestionId = questionId,
                ResponseId = responseId,
                UserId = userId,
                Action = action,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Tracked review status change: {Action} from {FromStatus} to {ToStatus}", action, fromStatus, toStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking review status change for Assignment ID {AssignmentId}", assignmentId);
        }
    }
} 