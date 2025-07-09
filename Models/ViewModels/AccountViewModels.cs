using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginWith2faViewModel
{
    [Required]
    [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Text)]
    [Display(Name = "Authenticator code")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Remember this machine")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }
}

public class ExternalLoginConfirmationViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
}

#region Home Dashboard ViewModels

public class HomeViewModel
{
    public UserDashboardSummaryViewModel Summary { get; set; } = new();
    public List<MyAssignmentViewModel> MyAssignments { get; set; } = new();
    public List<MyReviewAssignmentViewModel> MyReviews { get; set; } = new();
    public List<MyCampaignViewModel> MyCampaigns { get; set; } = new();
    public bool ShowCampaigns { get; set; } // Based on user role
    public bool ShowReviews { get; set; } // Based on whether user has reviews
}

public class UserDashboardSummaryViewModel
{
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int PendingReviews { get; set; }
    public int CompletedReviews { get; set; }
    public int MyCampaignsCount { get; set; }
    public int ActiveCampaignsCount { get; set; }
    
    // Computed properties
    public decimal CompletionRate => TotalAssignments > 0 ? 
        (decimal)CompletedAssignments / TotalAssignments * 100 : 0;
    public bool HasOverdueWork => OverdueAssignments > 0;
    public bool HasPendingWork => InProgressAssignments > 0 || PendingReviews > 0;
}

public class MyAssignmentViewModel
{
    public int AssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsLeadResponder { get; set; }
    public bool IsDelegated { get; set; }
    
    // Computed properties
    public decimal ProgressPercentage => TotalQuestions > 0 ? 
        (decimal)QuestionsAnswered / TotalQuestions * 100 : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && 
        Status != AssignmentStatus.Submitted && Status != AssignmentStatus.Approved;
    public bool IsNearDeadline => Deadline.HasValue && !IsOverdue && 
        Deadline.Value.Subtract(DateTime.Now).TotalDays <= 3;
    public int DaysUntilDeadline => Deadline.HasValue ? 
        Math.Max(0, (int)(Deadline.Value - DateTime.Now).TotalDays) : 0;
    public string StatusDisplayName => Status switch
    {
        AssignmentStatus.NotStarted => "Not Started",
        AssignmentStatus.InProgress => "In Progress", 
        AssignmentStatus.Submitted => "Submitted",
        AssignmentStatus.UnderReview => "Under Review",
        AssignmentStatus.Approved => "Approved",
        AssignmentStatus.ChangesRequested => "Changes Requested",
        _ => Status.ToString()
    };
}

public class MyReviewAssignmentViewModel
{
    public int ReviewAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public ReviewScope Scope { get; set; }
    public string? SectionName { get; set; }
    public string? QuestionText { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PendingResponsesCount { get; set; }
    public int TotalResponsesCount { get; set; }
    
    // Computed properties
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && 
        Status != ReviewStatus.Completed;
    public bool IsNearDeadline => Deadline.HasValue && !IsOverdue && 
        Deadline.Value.Subtract(DateTime.Now).TotalDays <= 2;
    public string ScopeDisplayName => Scope switch
    {
        ReviewScope.Assignment => "Full Assignment",
        ReviewScope.Section => $"Section: {SectionName}",
        ReviewScope.Question => $"Question: {QuestionText?.Substring(0, Math.Min(50, QuestionText.Length))}...",
        _ => Scope.ToString()
    };
    public string StatusDisplayName => Status switch
    {
        ReviewStatus.Pending => "Assigned",
        ReviewStatus.InReview => "In Progress",
        ReviewStatus.Completed => "Completed",
        ReviewStatus.Approved => "Approved",
        ReviewStatus.ChangesRequested => "Changes Requested",
        _ => Status.ToString()
    };
}

public class MyCampaignViewModel
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    
    // Computed properties
    public decimal CompletionRate => TotalAssignments > 0 ? 
        (decimal)CompletedAssignments / TotalAssignments * 100 : 0;
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now && Status == CampaignStatus.Active;
    public bool IsNearDeadline => Deadline.HasValue && !IsOverdue && 
        Deadline.Value.Subtract(DateTime.Now).TotalDays <= 7;
    public string StatusDisplayName => Status switch
    {
        CampaignStatus.Draft => "Draft",
        CampaignStatus.Active => "Open",
        CampaignStatus.Paused => "Paused",
        CampaignStatus.Completed => "Closed",
        CampaignStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };
    public bool NeedsAttention => OverdueAssignments > 0 || IsOverdue || 
        (Status == CampaignStatus.Active && CompletionRate < 25 && IsNearDeadline);
}

#endregion 