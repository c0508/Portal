using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class DelegationDashboardViewModel
{
    public List<DelegationSummaryViewModel> DelegationsReceived { get; set; } = new();
    public List<DelegationSummaryViewModel> DelegationsGiven { get; set; } = new();
    public DelegationStatisticsViewModel Statistics { get; set; } = new();
    public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
}

public class DelegationSummaryViewModel
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public int CampaignAssignmentId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    
    // Delegation info
    public string FromUserId { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? Deadline { get; set; }
    
    // Status info
    public DelegationStatus Status => GetStatus();
    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && !IsCompleted;
    public int DaysRemaining => Deadline.HasValue ? Math.Max(0, (Deadline.Value - DateTime.Now).Days) : 0;
    
    // Response info
    public bool HasResponse { get; set; }
    public string? ResponseSummary { get; set; }
    
    private DelegationStatus GetStatus()
    {
        if (!IsActive) return DelegationStatus.Cancelled;
        if (CompletedAt.HasValue) return DelegationStatus.Completed;
        if (Deadline.HasValue && Deadline.Value < DateTime.Now) return DelegationStatus.Overdue;
        return DelegationStatus.Pending;
    }
}

public class DelegationStatisticsViewModel
{
    public int TotalDelegationsReceived { get; set; }
    public int TotalDelegationsGiven { get; set; }
    public int PendingReceived { get; set; }
    public int PendingGiven { get; set; }
    public int CompletedReceived { get; set; }
    public int CompletedGiven { get; set; }
    public int OverdueReceived { get; set; }
    public int OverdueGiven { get; set; }
    
    public double CompletionRateReceived => TotalDelegationsReceived > 0 ? 
        (double)CompletedReceived / TotalDelegationsReceived * 100 : 0;
    public double CompletionRateGiven => TotalDelegationsGiven > 0 ? 
        (double)CompletedGiven / TotalDelegationsGiven * 100 : 0;
}

public class DelegationHistoryViewModel
{
    public List<DelegationHistoryItemViewModel> History { get; set; } = new();
    public string FilterStatus { get; set; } = "All";
    public string FilterType { get; set; } = "All"; // All, Received, Given
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

public class DelegationHistoryItemViewModel
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DelegationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string DelegationType { get; set; } = string.Empty; // "Received" or "Given"
    public bool HasResponse { get; set; }
    public string? ResponseSummary { get; set; }
}

public class BulkDelegationViewModel
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public List<BulkDelegationQuestionViewModel> Questions { get; set; } = new();
    public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
    public string? GlobalInstructions { get; set; }
}

public class BulkDelegationQuestionViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsSelected { get; set; }
    public string? ToUserId { get; set; }
    public string? Instructions { get; set; }
}

public class DelegationTemplateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<DelegationTemplateItemViewModel> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public class DelegationTemplateItemViewModel
{
    public int Id { get; set; }
    public string QuestionPattern { get; set; } = string.Empty; // Regex or text pattern
    public string? SectionPattern { get; set; }
    public QuestionType? QuestionType { get; set; }
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int Priority { get; set; } = 1;
}

public class CreateDelegationTemplateViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public List<CreateDelegationTemplateItemViewModel> Items { get; set; } = new();
}

public class CreateDelegationTemplateItemViewModel
{
    [Required]
    [StringLength(200)]
    public string QuestionPattern { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? SectionPattern { get; set; }
    
    public QuestionType? QuestionType { get; set; }
    
    [Required]
    public string ToUserId { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    public int Priority { get; set; } = 1;
}

public class DelegationTimelineViewModel
{
    public List<DelegationTimelineItemViewModel> TimelineItems { get; set; } = new();
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
    public string FilterUser { get; set; } = "All";
    public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
}

public class DelegationTimelineItemViewModel
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty; // Created, Completed, Cancelled, etc.
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public DelegationStatus Status { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public enum DelegationStatus
{
    Pending,
    Completed,
    Overdue,
    Cancelled
} 