using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IQuestionChangeTrackingService
{
    Task TrackQuestionChangesAsync(Question originalQuestion, Question updatedQuestion, string userId, string? changeReason = null);
}

public class QuestionChangeTrackingService : IQuestionChangeTrackingService
{
    private readonly ApplicationDbContext _context;

    public QuestionChangeTrackingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task TrackQuestionChangesAsync(Question originalQuestion, Question updatedQuestion, string userId, string? changeReason = null)
    {
        var changes = new List<QuestionChange>();

        // Track QuestionText changes
        if (originalQuestion.QuestionText != updatedQuestion.QuestionText)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "QuestionText",
                OldValue = originalQuestion.QuestionText,
                NewValue = updatedQuestion.QuestionText,
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track HelpText changes
        if (originalQuestion.HelpText != updatedQuestion.HelpText)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "HelpText",
                OldValue = originalQuestion.HelpText,
                NewValue = updatedQuestion.HelpText,
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track Section changes
        if (originalQuestion.Section != updatedQuestion.Section)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "Section",
                OldValue = originalQuestion.Section,
                NewValue = updatedQuestion.Section,
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track QuestionType changes
        if (originalQuestion.QuestionType != updatedQuestion.QuestionType)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "QuestionType",
                OldValue = originalQuestion.QuestionType.ToString(),
                NewValue = updatedQuestion.QuestionType.ToString(),
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track IsRequired changes
        if (originalQuestion.IsRequired != updatedQuestion.IsRequired)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "IsRequired",
                OldValue = originalQuestion.IsRequired.ToString(),
                NewValue = updatedQuestion.IsRequired.ToString(),
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track Options changes
        if (originalQuestion.Options != updatedQuestion.Options)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "Options",
                OldValue = originalQuestion.Options,
                NewValue = updatedQuestion.Options,
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track ValidationRules changes
        if (originalQuestion.ValidationRules != updatedQuestion.ValidationRules)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "ValidationRules",
                OldValue = originalQuestion.ValidationRules,
                NewValue = updatedQuestion.ValidationRules,
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Track DisplayOrder changes
        if (originalQuestion.DisplayOrder != updatedQuestion.DisplayOrder)
        {
            changes.Add(new QuestionChange
            {
                QuestionId = originalQuestion.Id,
                ChangedById = userId,
                FieldName = "DisplayOrder",
                OldValue = originalQuestion.DisplayOrder.ToString(),
                NewValue = updatedQuestion.DisplayOrder.ToString(),
                ChangeReason = changeReason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Save all changes
        if (changes.Any())
        {
            _context.QuestionChanges.AddRange(changes);
            await _context.SaveChangesAsync();
        }
    }
} 