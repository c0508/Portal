using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class AssignmentDashboardViewModel
{
    public List<AssignmentSummaryViewModel> MyAssignments { get; set; } = new();
    public List<AssignmentSummaryViewModel> DelegatedToMe { get; set; } = new();
    public List<AssignmentSummaryViewModel> MyDelegations { get; set; } = new();
    public List<AssignmentSummaryViewModel> QuestionAssignmentsToMe { get; set; } = new();
}

public class AssignmentSummaryViewModel
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int DelegatedQuestions { get; set; }
    public int Progress { get; set; }
    
    // For delegated assignments
    public string? DelegatedBy { get; set; }
    public string? DelegationInstructions { get; set; }
    public int? QuestionId { get; set; } // For single question delegations
    
    // Computed properties
    public int ProgressPercentage => TotalQuestions > 0 ? (AnsweredQuestions * 100) / TotalQuestions : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && Status != AssignmentStatus.Submitted;
}

public class QuestionnaireResponseViewModel
{
    public int AssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTime? Deadline { get; set; }
    public AssignmentStatus Status { get; set; }
    public CampaignAssignment Assignment { get; set; } = null!;
    public int CompletionPercentage { get; set; }
    
    public List<QuestionResponseViewModel> Questions { get; set; } = new();
    public List<QuestionSectionViewModel> Sections { get; set; } = new(); // Add sections for grouped display
    public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
    public bool IsDelegatedUser { get; set; } = false;
    
    // Review assignment info
    public bool IsCurrentUserReviewer { get; set; }
    public bool HasReviewAssignments { get; set; }
    public int ReviewAssignmentsCount { get; set; }
    public int PendingReviewsCount { get; set; }
    public int CompletedReviewsCount { get; set; }
    
    // Progress tracking
    public int TotalQuestions => Questions.Count;
    public int AnsweredQuestions => Questions.Count(q => q.HasResponse);
    public int DelegatedQuestions => Questions.Count(q => q.IsDelegated);
    public int ReviewedQuestions => Questions.Count(q => q.ReviewStatus == Entities.ReviewStatus.Approved || q.ReviewStatus == Entities.ReviewStatus.Completed);
    public int QuestionsNeedingReview => Questions.Count(q => q.IsAssignedForReview && q.HasResponse && (q.ReviewStatus == Entities.ReviewStatus.Pending || q.ReviewStatus == null));
    public int ProgressPercentage => TotalQuestions > 0 ? (AnsweredQuestions * 100) / TotalQuestions : 0;
    
    // Only lead responders can submit, delegated users cannot
    public bool CanSubmit => !IsDelegatedUser && Status == AssignmentStatus.InProgress && AnsweredQuestions == TotalQuestions;
    public bool IsReadOnly => Status == AssignmentStatus.Submitted || Status == AssignmentStatus.UnderReview || Status == AssignmentStatus.Approved;
    
    // Review summary
    public bool HasPendingReviews => QuestionsNeedingReview > 0;
    public bool CanAccessReviews => IsCurrentUserReviewer || HasReviewAssignments;
}

public class QuestionResponseViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public string? Section { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public List<string>? Options { get; set; } // Parsed from JSON
    public List<string>? QuestionOptions { get; set; } // Alternative name for controller compatibility
    
    // Entity references for controller compatibility
    public Question Question { get; set; } = null!;
    public Response? Response { get; set; }
    public List<Delegation> Delegations { get; set; } = new();
    public List<FileUpload> Files { get; set; } = new();
    
    // Current response data
    public int? ResponseId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public List<string>? SelectedValues { get; set; } // For multi-select
    public List<FileUploadViewModel>? FileUploads { get; set; }
    
    // Delegation info
    public bool IsDelegated { get; set; }
    public string? DelegatedTo { get; set; }
    public string? DelegatedToName { get; set; }
    public string? DelegationInstructions { get; set; }
    public DateTime? DelegatedAt { get; set; }
    public bool IsDelegationCompleted { get; set; }
    
    // Review assignment info
    public bool IsAssignedForReview { get; set; }
    public ReviewStatus? ReviewStatus { get; set; }
    public int? ReviewAssignmentId { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewInstructions { get; set; }
    public bool IsCurrentUserReviewer { get; set; }
    public DateTime? ReviewAssignedAt { get; set; }
    public int ReviewCommentsCount { get; set; }
    public bool HasUnresolvedComments { get; set; }
    
    // State flags
    public bool HasResponse => !string.IsNullOrEmpty(TextValue) || NumericValue.HasValue || 
                               DateValue.HasValue || BooleanValue.HasValue || 
                               (SelectedValues?.Any() == true) || (FileUploads?.Any() == true);
    public bool CanDelegate { get; set; } = true;
    public bool CanAnswer => !IsDelegated || IsDelegationCompleted;
    public bool IsConditionallyVisible { get; set; } = true; // For conditional question logic
    
    // Review status display
    public string ReviewStatusDisplay => ReviewStatus switch
    {
        Entities.ReviewStatus.Pending => "Pending Review",
        Entities.ReviewStatus.InReview => "Under Review", 
        Entities.ReviewStatus.Approved => "Approved",
        Entities.ReviewStatus.ChangesRequested => "Changes Requested",
        Entities.ReviewStatus.Completed => "Review Complete",
        _ => "Not Reviewed"
    };
    
    public string ReviewStatusBadgeClass => ReviewStatus switch
    {
        Entities.ReviewStatus.Pending => "bg-warning",
        Entities.ReviewStatus.InReview => "bg-info",
        Entities.ReviewStatus.Approved => "bg-success",
        Entities.ReviewStatus.ChangesRequested => "bg-danger",
        Entities.ReviewStatus.Completed => "bg-success",
        _ => "bg-secondary"
    };
}

public class SaveResponseRequest
{
    public int AssignmentId { get; set; }
    public int QuestionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public List<string>? SelectedValues { get; set; }
}

public class DelegateQuestionViewModel
{
    public int AssignmentId { get; set; }
    public int QuestionId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    
    [Required]
    [Display(Name = "Delegate To")]
    public string ToUserId { get; set; } = string.Empty;
    
    [Display(Name = "Instructions")]
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    [Display(Name = "Notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
}

public class TeamMemberViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FileUploadViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024 * 1024):F1} MB";
            return $"{FileSize / (1024 * 1024 * 1024):F1} GB";
        }
    }
}

public class SubmissionReviewViewModel
{
    public int AssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public CampaignAssignment Assignment { get; set; } = null!;
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int CompletionPercentage { get; set; }
    
    public List<QuestionResponseSummaryViewModel> Questions { get; set; } = new();
    public List<QuestionSummaryViewModel> QuestionSummaries { get; set; } = new();
    
    public int RequiredQuestions => Questions.Count(q => q.IsRequired);
    public int AnsweredRequiredQuestions => Questions.Count(q => q.IsRequired && q.HasResponse);
    
    public bool CanSubmit => AnsweredRequiredQuestions == RequiredQuestions;
    public List<string> MissingRequiredQuestions => Questions
        .Where(q => q.IsRequired && !q.HasResponse)
        .Select(q => q.QuestionText)
        .ToList();
}

public class QuestionResponseSummaryViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public bool HasResponse { get; set; }
    public string? ResponseSummary { get; set; }
    public bool IsDelegated { get; set; }
    public string? DelegatedTo { get; set; }
}

public class QuestionSummaryViewModel
{
    public Question Question { get; set; } = null!;
    public Response? Response { get; set; }
    public string ResponseText { get; set; } = string.Empty;
    public int FileCount { get; set; }
}

public class QuestionSectionViewModel
{
    public string SectionName { get; set; } = string.Empty;
    public List<QuestionResponseViewModel> Questions { get; set; } = new();
    
    // Computed properties for section status
    public int TotalQuestions => Questions.Count;
    public int AnsweredQuestions => Questions.Count(q => q.HasResponse);
    public int DelegatedQuestions => Questions.Count(q => q.IsDelegated);
    public int RequiredQuestions => Questions.Count(q => q.IsRequired);
    public int AnsweredRequiredQuestions => Questions.Count(q => q.IsRequired && q.HasResponse);
    
    // Section completion status
    public bool IsCompleted => RequiredQuestions == 0 ? 
        AnsweredQuestions == TotalQuestions : 
        AnsweredRequiredQuestions == RequiredQuestions;
    
    public bool HasAnswers => AnsweredQuestions > 0;
    public bool HasDelegations => DelegatedQuestions > 0;
    
    // Progress percentage for the section
    public int ProgressPercentage => TotalQuestions > 0 ? 
        (AnsweredQuestions * 100) / TotalQuestions : 0;
    
    // Status icon class for display
    public string StatusIconClass => IsCompleted ? "bi-check-circle text-success" : 
                                    HasAnswers ? "bi-circle-half text-warning" : 
                                    "bi-circle text-muted";
    
    // Status text for display
    public string StatusText => IsCompleted ? "Complete" : 
                               HasAnswers ? "In Progress" : 
                               "Pending";
}

#region Review System ViewModels

public class AssignReviewerViewModel
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    
    public List<User> AvailableReviewers { get; set; } = new();
    public List<Question> Questions { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();
    public List<ReviewAssignment> ExistingAssignments { get; set; } = new();
    
    // Add assignment property for compatibility with existing views
    public CampaignAssignment Assignment { get; set; } = null!;
    
    // Organize questions by sections for the view
    public Dictionary<string, List<Question>> QuestionsBySections { get; set; } = new();
    
    // Computed properties for statistics
    public int TotalQuestions => Questions.Count;
    public int TotalAssignments => ExistingAssignments.Count;
    public int AvailableReviewersCount => AvailableReviewers.Count;
    public int UnassignedQuestions => TotalQuestions - ExistingAssignments.Count(ea => ea.QuestionId.HasValue);
    
    // Section-based statistics
    public int AssignedSections => ExistingAssignments.Count(ea => !string.IsNullOrEmpty(ea.SectionName));
    public int TotalSections => AvailableSections.Count;
    
    // Assignment progress
    public decimal AssignmentProgressPercentage => TotalQuestions > 0 ? 
        ((decimal)TotalAssignments / TotalQuestions) * 100 : 0;
    
    // Get assignments by reviewer for display
    public Dictionary<string, List<ReviewAssignment>> AssignmentsByReviewer => 
        ExistingAssignments.GroupBy(ea => ea.ReviewerId)
                          .ToDictionary(g => g.Key, g => g.ToList());
}

public class AssignQuestionReviewerRequest
{
    [Required]
    public int CampaignAssignmentId { get; set; }
    
    [Required]
    public int QuestionId { get; set; }
    
    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Instructions { get; set; }
}

public class AssignSectionReviewerRequest
{
    [Required]
    public int CampaignAssignmentId { get; set; }
    
    [Required]
    public string SectionName { get; set; } = string.Empty;
    
    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Instructions { get; set; }
}

public class MyReviewsViewModel
{
    public List<ReviewAssignmentSummaryViewModel> ReviewAssignments { get; set; } = new();
}

public class ReviewAssignmentSummaryViewModel
{
    public int Id { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public ReviewScope Scope { get; set; }
    public string? QuestionText { get; set; }
    public string? SectionName { get; set; }
    public ReviewStatus Status { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PendingCommentsCount { get; set; }
    
    public string ScopeDisplayText => Scope switch
    {
        ReviewScope.Question => $"Question: {QuestionText?.Substring(0, Math.Min(QuestionText.Length, 50))}...",
        ReviewScope.Section => $"Section: {SectionName}",
        ReviewScope.Assignment => "Entire Assignment",
        _ => "Unknown"
    };
    
    public string StatusDisplayText => Status switch
    {
        ReviewStatus.Pending => "Pending Review",
        ReviewStatus.InReview => "In Review",
        ReviewStatus.Approved => "Approved",
        ReviewStatus.ChangesRequested => "Changes Requested",
        ReviewStatus.Completed => "Completed",
        _ => Status.ToString()
    };
    
    public string StatusBadgeClass => Status switch
    {
        ReviewStatus.Pending => "badge bg-warning",
        ReviewStatus.InReview => "badge bg-primary",
        ReviewStatus.Approved => "badge bg-success",
        ReviewStatus.ChangesRequested => "badge bg-danger",
        ReviewStatus.Completed => "badge bg-secondary",
        _ => "badge bg-light"
    };
}

public class ReviewQuestionsViewModel
{
    public ReviewAssignment ReviewAssignment { get; set; } = null!;
    public List<QuestionReviewViewModel> QuestionReviews { get; set; } = new();
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    
    public int TotalQuestions => QuestionReviews.Count;
    public int QuestionsWithResponses => QuestionReviews.Count(qr => qr.HasResponse);
    public int QuestionsWithComments => QuestionReviews.Count(qr => qr.Comments.Any());
    
    public bool CanComplete => QuestionsWithResponses > 0; // Can complete if there are responses to review
}

public class QuestionReviewViewModel
{
    public Question Question { get; set; } = null!;
    public Response? Response { get; set; }
    public List<ReviewComment> Comments { get; set; } = new();
    public bool HasResponse { get; set; }
    public string ResponseText { get; set; } = string.Empty;
    
    public bool HasUnresolvedComments => Comments.Any(c => !c.IsResolved);
    public int CommentCount => Comments.Count;
}

public class AddReviewCommentRequest
{
    [Required]
    public int ReviewAssignmentId { get; set; }
    
    [Required]
    public int ResponseId { get; set; }
    
    [Required]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
    
    [Required]
    public ReviewStatus ActionTaken { get; set; }
    
    public bool RequiresChange { get; set; } = false;
}

public class ReviewAuditViewModel
{
    public CampaignAssignment CampaignAssignment { get; set; } = null!;
    public List<ReviewAuditLog> AuditLogs { get; set; } = new();
    public ReviewSummaryStats ReviewSummary { get; set; } = null!;
}

/// <summary>
/// Summary statistics for review assignments
/// </summary>
public class ReviewSummaryStats
{
    public int TotalReviews { get; set; }
    public int PendingReviews { get; set; }
    public int InProgressReviews { get; set; }
    public int ApprovedReviews { get; set; }
    public int ChangesRequestedReviews { get; set; }
    public int CompletedReviews { get; set; }
}

public class ReviewAssignmentViewModel
{
    public int Id { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    
    // Question-specific properties (for individual question reviews)
    public string? QuestionText { get; set; }
    public int? QuestionDisplayOrder { get; set; }
    
    // Section-specific properties (for section reviews)
    public string? SectionName { get; set; }
    
    // Review details
    public string? ReviewerName { get; set; }
    public string? Instructions { get; set; }
}

#endregion 