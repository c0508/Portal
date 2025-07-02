using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class CampaignCreateViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Campaign Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Status")]
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Deadline")]
    [DataType(DataType.Date)]
    public DateTime? Deadline { get; set; }

    // Reporting period fields
    [Display(Name = "Reporting Period Start")]
    [DataType(DataType.Date)]
    public DateTime? ReportingPeriodStart { get; set; }

    [Display(Name = "Reporting Period End")]
    [DataType(DataType.Date)]
    public DateTime? ReportingPeriodEnd { get; set; }

    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }

    // For dropdown lists
    public List<QuestionnaireSelectionViewModel> AvailableQuestionnaires { get; set; } = new();
    public List<OrganizationSelectionViewModel> AvailableOrganizations { get; set; } = new();
}

public class CampaignEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Campaign Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Status")]
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Deadline")]
    [DataType(DataType.Date)]
    public DateTime? Deadline { get; set; }

    // Reporting period fields
    [Display(Name = "Reporting Period Start")]
    [DataType(DataType.Date)]
    public DateTime? ReportingPeriodStart { get; set; }

    [Display(Name = "Reporting Period End")]
    [DataType(DataType.Date)]
    public DateTime? ReportingPeriodEnd { get; set; }

    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }
}

public class CampaignAssignmentCreateViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Target Organization")]
    public int TargetOrganizationId { get; set; }

    [Required]
    [Display(Name = "Questionnaire Version")]
    public int QuestionnaireVersionId { get; set; }

    [Display(Name = "Lead Responder")]
    public string? LeadResponderId { get; set; }

    // For dropdown lists
    public List<OrganizationSelectionViewModel> AvailableOrganizations { get; set; } = new();
    public List<QuestionnaireVersionSelectionViewModel> AvailableQuestionnaireVersions { get; set; } = new();
    public List<UserSelectionViewModel> AvailableLeadResponders { get; set; } = new();
}

public class QuestionnaireSelectionViewModel
{
    public int QuestionnaireId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<QuestionnaireVersionViewModel> Versions { get; set; } = new();
}

public class QuestionnaireVersionViewModel
{
    public int Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class QuestionnaireVersionSelectionViewModel
{
    public int Id { get; set; }
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}

public class OrganizationSelectionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CampaignAssignmentEditViewModel
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Target Organization")]
    public int TargetOrganizationId { get; set; }

    [Required]
    [Display(Name = "Questionnaire Version")]
    public int QuestionnaireVersionId { get; set; }

    [Display(Name = "Lead Responder")]
    public string? LeadResponderId { get; set; }

    [Required]
    [Display(Name = "Status")]
    public AssignmentStatus Status { get; set; }

    [Display(Name = "Review Notes")]
    [StringLength(1000)]
    public string? ReviewNotes { get; set; }

    // For dropdown lists
    public List<OrganizationSelectionViewModel> AvailableOrganizations { get; set; } = new();
    public List<QuestionnaireVersionSelectionViewModel> AvailableQuestionnaireVersions { get; set; } = new();
    public List<UserSelectionViewModel> AvailableLeadResponders { get; set; } = new();
}

public class UserSelectionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Enhanced bulk assignment view model
public class CampaignBulkAssignmentViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Questionnaire Version")]
    public int QuestionnaireVersionId { get; set; }

    // Selected organization IDs
    public List<int> SelectedOrganizationIds { get; set; } = new();

    // For dropdown lists
    public List<QuestionnaireVersionSelectionViewModel> AvailableQuestionnaireVersions { get; set; } = new();
    public List<OrganizationSelectionWithDetailsViewModel> AvailableOrganizations { get; set; } = new();
    
    // Filtering properties
    public string? NameFilter { get; set; }
    public string? AttributeFilter { get; set; }
    public string? RelationshipTypeFilter { get; set; }
}

// Enhanced organization selection with additional details
public class OrganizationSelectionWithDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public List<OrganizationAttributeViewModel> Attributes { get; set; } = new();
    public string? RelationshipType { get; set; }
    public bool HasActiveRelationship { get; set; }
    public int UsersCount { get; set; }
    public bool AlreadyAssigned { get; set; }
}

public class OrganizationAttributeViewModel
{
    public string AttributeType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

#region Campaign Dashboard ViewModels (Phase 5.1)

public class CampaignDashboardViewModel
{
    public CampaignDashboardSummaryViewModel Summary { get; set; } = new();
    public List<CampaignDashboardItemViewModel> ActiveCampaigns { get; set; } = new();
    public List<CampaignDashboardItemViewModel> RecentCampaigns { get; set; } = new();
    public CampaignProgressMetricsViewModel ProgressMetrics { get; set; } = new();
    public List<CampaignPerformanceViewModel> CampaignPerformance { get; set; } = new();
    
    // Enhanced assignment-focused sections
    public List<CompanyAssignmentStatusViewModel> CompanyBreakdown { get; set; } = new();
    public List<ResponderWorkloadViewModel> ResponderBreakdown { get; set; } = new();
    public AssignmentStatusDistributionViewModel StatusDistribution { get; set; } = new();
}

public class CampaignDashboardSummaryViewModel
{
    public int TotalActiveCampaigns { get; set; }
    public int TotalCompletedCampaigns { get; set; }
    public int TotalDraftCampaigns { get; set; }
    public int TotalPausedCampaigns { get; set; }
    public int TotalAssignments { get; set; }
    public int TotalActiveAssignments { get; set; }
    public int TotalCompletedAssignments { get; set; }
    public int TotalOverdueAssignments { get; set; }
    
    // Computed properties
    public decimal OverallCompletionRate => TotalAssignments > 0 ? 
        (decimal)TotalCompletedAssignments / TotalAssignments * 100 : 0;
    public decimal OverdueRate => TotalAssignments > 0 ? 
        (decimal)TotalOverdueAssignments / TotalAssignments * 100 : 0;
}

public class CampaignDashboardItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    
    // Assignment statistics
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int NotStartedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    
    // Progress metrics
    public decimal CompletionPercentage => TotalAssignments > 0 ? 
        (decimal)CompletedAssignments / TotalAssignments * 100 : 0;
    public decimal InProgressPercentage => TotalAssignments > 0 ? 
        (decimal)InProgressAssignments / TotalAssignments * 100 : 0;
    
    // Time metrics
    public int DaysRemaining => Deadline.HasValue ? 
        Math.Max(0, (int)(Deadline.Value - DateTime.Now).TotalDays) : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && Status == CampaignStatus.Active;
    public bool IsNearDeadline => Deadline.HasValue && !IsOverdue && DaysRemaining <= 7;
    
    // Response time analytics
    public double? AverageResponseTimeHours { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? LastResponseAt { get; set; }
}

public class CampaignProgressMetricsViewModel
{
    public decimal OverallCompletionRate { get; set; }
    public decimal OverallResponseRate { get; set; }
    public double AverageResponseTimeHours { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalQuestionsAssigned { get; set; }
    public int ActiveRespondents { get; set; }
    public int TotalRespondents { get; set; }
    
    // Time-based metrics
    public List<DailyProgressViewModel> DailyProgress { get; set; } = new();
    public List<WeeklyProgressViewModel> WeeklyProgress { get; set; } = new();
}

public class DailyProgressViewModel
{
    public DateTime Date { get; set; }
    public int ResponsesReceived { get; set; }
    public int AssignmentsCompleted { get; set; }
    public int NewAssignments { get; set; }
}

public class WeeklyProgressViewModel
{
    public DateTime WeekStartDate { get; set; }
    public string WeekLabel => WeekStartDate.ToString("MMM dd");
    public int ResponsesReceived { get; set; }
    public int AssignmentsCompleted { get; set; }
    public int NewAssignments { get; set; }
    public decimal CompletionRate { get; set; }
}

public class CampaignPerformanceViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public decimal CompletionRate { get; set; }
    public double AverageResponseTimeHours { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsOnTrack { get; set; }
    public string PerformanceIndicator { get; set; } = string.Empty; // "Excellent", "Good", "At Risk", "Behind"
    public string PerformanceColor { get; set; } = string.Empty; // CSS class for color coding
}

// Assignment-focused view models for enhanced dashboard

public class CompanyAssignmentStatusViewModel
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public List<OrganizationAttributeViewModel> Attributes { get; set; } = new();
    public string RelationshipType { get; set; } = string.Empty;
    
    // Assignment statistics
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int NotStartedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int UnderReviewAssignments { get; set; }
    
    // Company-specific metrics
    public decimal CompletionRate => TotalAssignments > 0 ? 
        (decimal)CompletedAssignments / TotalAssignments * 100 : 0;
    public decimal OverdueRate => TotalAssignments > 0 ? 
        (decimal)OverdueAssignments / TotalAssignments * 100 : 0;
    public int ActiveResponders { get; set; }
    public int TotalResponders { get; set; }
    
    // Time metrics
    public DateTime? LastResponseDate { get; set; }
    public double? AverageResponseTimeHours { get; set; }
    public DateTime? NextDeadline { get; set; }
    public bool HasOverdueAssignments => OverdueAssignments > 0;
    public bool IsAtRisk => CompletionRate < 50 && NextDeadline.HasValue && 
        NextDeadline.Value.Subtract(DateTime.Now).TotalDays <= 7;
    
    // Campaign breakdown
    public List<CampaignAssignmentSummaryViewModel> CampaignAssignments { get; set; } = new();
}

public class CampaignAssignmentSummaryViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public CampaignStatus CampaignStatus { get; set; }
    public AssignmentStatus AssignmentStatus { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public string? LeadResponderName { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
    public decimal ProgressPercentage => TotalQuestions > 0 ? 
        (decimal)QuestionsAnswered / TotalQuestions * 100 : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && 
        AssignmentStatus != AssignmentStatus.Submitted && AssignmentStatus != AssignmentStatus.Approved;
}

public class ResponderWorkloadViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    
    // Workload statistics
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int NotStartedAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int AssignmentsAsLeadResponder { get; set; }
    public int DelegatedAssignments { get; set; }
    
    // Performance metrics
    public decimal CompletionRate => TotalAssignments > 0 ? 
        (decimal)CompletedAssignments / TotalAssignments * 100 : 0;
    public double? AverageResponseTimeHours { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestionsAssigned { get; set; }
    
    // Workload indicators
    public bool IsOverloaded => InProgressAssignments > 5 || OverdueAssignments > 2;
    public bool IsInactive => LastActivityDate.HasValue && 
        LastActivityDate.Value < DateTime.Now.AddDays(-7);
    public string WorkloadIndicator => IsOverloaded ? "High" : 
        InProgressAssignments > 2 ? "Medium" : "Low";
    public string WorkloadColor => IsOverloaded ? "danger" : 
        InProgressAssignments > 2 ? "warning" : "success";
    
    // Assignment breakdown
    public List<ResponderAssignmentSummaryViewModel> AssignmentDetails { get; set; } = new();
}

public class ResponderAssignmentSummaryViewModel
{
    public int AssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
    public decimal ProgressPercentage => TotalQuestions > 0 ? 
        (decimal)QuestionsAnswered / TotalQuestions * 100 : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && 
        Status != AssignmentStatus.Submitted && Status != AssignmentStatus.Approved;
    public bool IsLeadResponder { get; set; }
    public bool IsDelegated { get; set; }
}

public class AssignmentStatusDistributionViewModel
{
    public int TotalAssignments { get; set; }
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int ChangesRequestedCount { get; set; }
    public int OverdueCount { get; set; }
    
    // Percentage calculations
    public decimal NotStartedPercentage => CalculatePercentage(NotStartedCount);
    public decimal InProgressPercentage => CalculatePercentage(InProgressCount);
    public decimal SubmittedPercentage => CalculatePercentage(SubmittedCount);
    public decimal UnderReviewPercentage => CalculatePercentage(UnderReviewCount);
    public decimal ApprovedPercentage => CalculatePercentage(ApprovedCount);
    public decimal ChangesRequestedPercentage => CalculatePercentage(ChangesRequestedCount);
    public decimal OverduePercentage => CalculatePercentage(OverdueCount);
    
    // Completion metrics
    public int CompletedCount => SubmittedCount + ApprovedCount;
    public decimal CompletionRate => CalculatePercentage(CompletedCount);
    public decimal ActiveCompletionRate => TotalAssignments - NotStartedCount > 0 ? 
        (decimal)CompletedCount / (TotalAssignments - NotStartedCount) * 100 : 0;
    
    private decimal CalculatePercentage(int count) => 
        TotalAssignments > 0 ? (decimal)count / TotalAssignments * 100 : 0;
    
    // Status distribution for charts
    public List<StatusDistributionItemViewModel> StatusItems => new()
    {
        new() { Status = "Not Started", Count = NotStartedCount, Percentage = NotStartedPercentage, Color = "secondary" },
        new() { Status = "In Progress", Count = InProgressCount, Percentage = InProgressPercentage, Color = "warning" },
        new() { Status = "Submitted", Count = SubmittedCount, Percentage = SubmittedPercentage, Color = "info" },
        new() { Status = "Under Review", Count = UnderReviewCount, Percentage = UnderReviewPercentage, Color = "primary" },
        new() { Status = "Approved", Count = ApprovedCount, Percentage = ApprovedPercentage, Color = "success" },
        new() { Status = "Changes Requested", Count = ChangesRequestedCount, Percentage = ChangesRequestedPercentage, Color = "danger" }
    };
}

public class StatusDistributionItemViewModel
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

#endregion 