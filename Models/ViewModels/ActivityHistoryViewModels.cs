using ESGPlatform.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace ESGPlatform.Models.ViewModels;

public class ActivityHistoryViewModel
{
    public List<ActivityHistoryItemViewModel> Activities { get; set; } = new();
    public ActivityHistoryFilterViewModel Filter { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;
}

public class ActivityHistoryItemViewModel
{
    public int Id { get; set; }
    public string ActivityType { get; set; } = string.Empty; // "ResponseChange", "Delegation", "Review"
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    // Question/Campaign context
    public int? QuestionId { get; set; }
    public string? QuestionText { get; set; }
    public string? CampaignName { get; set; }
    public string? QuestionnaireName { get; set; }
    public string? Section { get; set; }
    
    // Change details
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? Details { get; set; }
    
    // Visual properties
    public string IconClass { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    
    // Computed properties
    public string TimeAgo 
    { 
        get 
        {
            var diff = DateTime.UtcNow - Timestamp;
            if (diff.Days > 0) return $"{diff.Days} day{(diff.Days > 1 ? "s" : "")} ago";
            if (diff.Hours > 0) return $"{diff.Hours} hour{(diff.Hours > 1 ? "s" : "")} ago";
            if (diff.Minutes > 0) return $"{diff.Minutes} minute{(diff.Minutes > 1 ? "s" : "")} ago";
            return "Just now";
        }
    }
    
    public bool HasChanges => !string.IsNullOrEmpty(OldValue) || !string.IsNullOrEmpty(NewValue);
}

public class ActivityHistoryFilterViewModel
{
    public string? SearchText { get; set; }
    public string ActivityType { get; set; } = "All"; // All, ResponseChange, Delegation, Review, QuestionAssignment
    public string Action { get; set; } = "All";
    public int? CampaignId { get; set; }
    public int? QuestionnaireId { get; set; }
    public int? QuestionId { get; set; }
    public string? Section { get; set; }
    public string? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Filter options (populated by controller)
    public List<Campaign> AvailableCampaigns { get; set; } = new();
    public List<Questionnaire> AvailableQuestionnaires { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();
    public List<UserSummaryViewModel> AvailableUsers { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();
}

public class UserSummaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ActivitySummaryViewModel
{
    public DateTime Date { get; set; }
    public int TotalActivities { get; set; }
    public int ResponseChanges { get; set; }
    public int DelegationChanges { get; set; }
    public int ReviewChanges { get; set; }
    public List<ActivityHistoryItemViewModel> RecentActivities { get; set; } = new();
}

public class QuestionActivityHistoryViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string? Section { get; set; }
    public List<ActivityHistoryItemViewModel> Activities { get; set; } = new();
    
    // Summary stats
    public int TotalResponseChanges { get; set; }
    public int TotalDelegations { get; set; }
    public int TotalReviewActions { get; set; }
    public int TotalAssignmentChanges { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class UserActivitySummaryViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int TotalActivities { get; set; }
    public int ResponseChanges { get; set; }
    public int DelegationActions { get; set; }
    public int ReviewActions { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<ActivityHistoryItemViewModel> RecentActivities { get; set; } = new();
}

public class CampaignActivitySummaryViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public int TotalActivities { get; set; }
    public int ResponseChanges { get; set; }
    public int DelegationActions { get; set; }
    public int ReviewActions { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<ActivityHistoryItemViewModel> RecentActivities { get; set; } = new();
}

public enum ActivityType
{
    ResponseChange,
    Delegation,
    Review
}

public enum ActivityAction
{
    // Response actions
    Created,
    Updated,
    Deleted,
    
    // Delegation actions
    Delegated,
    DelegationCompleted,
    DelegationCancelled,
    
    // Review actions
    ReviewAssigned,
    ReviewStarted,
    ReviewCompleted,
    ChangesRequested,
    Approved,
    CommentAdded
} 