using ESGPlatform.Models.Entities;

namespace ESGPlatform.Services;

public interface IQuestionAssignmentTrackingService
{
    Task TrackAssignmentCreatedAsync(QuestionAssignment assignment, string createdById, string? reason = null);
    Task TrackAssignmentModifiedAsync(QuestionAssignment oldAssignment, QuestionAssignment newAssignment, string changedById, string? reason = null);
    Task TrackAssignmentRemovedAsync(QuestionAssignment assignment, string removedById, string? reason = null);
} 