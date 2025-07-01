using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class QuestionAssignmentViewModel
{
    public int Id { get; set; }
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    
    [Display(Name = "Assignment Type")]
    public QuestionAssignmentType AssignmentType { get; set; }
    
    [Display(Name = "Question")]
    public int? QuestionId { get; set; }
    public string? QuestionText { get; set; }
    
    [Display(Name = "Section")]
    [StringLength(255)]
    public string? SectionName { get; set; }
    
    [Required]
    [Display(Name = "Assigned User")]
    public string AssignedUserId { get; set; } = string.Empty;
    public string AssignedUserName { get; set; } = string.Empty;
    public string AssignedUserEmail { get; set; } = string.Empty;
    
    [Display(Name = "Instructions")]
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    
    // Available options for dropdowns
    public List<SelectListItem>? AvailableUsers { get; set; }
    public List<SelectListItem>? AvailableSections { get; set; }
    public List<SelectListItem>? AvailableQuestions { get; set; }
}

public class QuestionAssignmentManagementViewModel
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    public string QuestionnaireTitle { get; set; } = string.Empty;
    public string? LeadResponderName { get; set; }
    
    // Questionnaire structure
    public List<AssignmentSectionViewModel> Sections { get; set; } = new();
    
    // Current assignments
    public List<QuestionAssignmentViewModel> CurrentAssignments { get; set; } = new();
    
    // Available users for assignment
    public List<AssignmentUserViewModel> AvailableUsers { get; set; } = new();
    
    // Assignment statistics
    public int TotalQuestions { get; set; }
    public int AssignedQuestions { get; set; }
    public int UnassignedQuestions { get; set; }
    public Dictionary<string, int> AssignmentsByUser { get; set; } = new();
}

public class AssignmentSectionViewModel
{
    public string SectionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<QuestionAssignmentItemViewModel> Questions { get; set; } = new();
    public int TotalQuestions { get; set; }
    public int AssignedQuestions { get; set; }
    public bool IsFullyAssigned { get; set; }
    public string? AssignedToUserId { get; set; }  // If entire section is assigned to one user
    public string? AssignedToUserName { get; set; }
    public int? SectionAssignmentId { get; set; }  // Assignment ID for unassignment
}

public class QuestionAssignmentItemViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsAssigned { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public int? AssignmentId { get; set; }
    public string? Instructions { get; set; }
}

public class AssignmentUserViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int AssignedQuestions { get; set; }
    public int AssignedSections { get; set; }
    public bool IsLeadResponder { get; set; }
}

public class CreateQuestionAssignmentViewModel
{
    [Required]
    public int CampaignAssignmentId { get; set; }
    
    [Required]
    [Display(Name = "Assignment Type")]
    public QuestionAssignmentType AssignmentType { get; set; }
    
    [Display(Name = "Question")]
    public int? QuestionId { get; set; }
    
    [Display(Name = "Section")]
    [StringLength(255)]
    public string? SectionName { get; set; }
    
    [Required]
    [Display(Name = "Assigned User")]
    public string AssignedUserId { get; set; } = string.Empty;
    
    [Display(Name = "Instructions")]
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    // For bulk assignment
    public List<int>? QuestionIds { get; set; }
    public List<string>? SectionNames { get; set; }
    
    // Available options
    public List<SelectListItem>? AvailableUsers { get; set; }
    public List<SelectListItem>? AvailableSections { get; set; }
    public List<SelectListItem>? AvailableQuestions { get; set; }
}

public class BulkAssignmentViewModel
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Assigned User")]
    public string AssignedUserId { get; set; } = string.Empty;
    
    [Display(Name = "Assignment Type")]
    public string AssignmentType { get; set; } = "Section";  // "Section" or "Questions"
    
    // For section assignment
    public List<string>? SelectedSections { get; set; }
    
    // For individual question assignment
    public List<int>? SelectedQuestionIds { get; set; }
    
    [Display(Name = "Instructions")]
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    // Available options
    public List<AssignmentUserViewModel>? AvailableUsers { get; set; }
    public List<AssignmentSectionViewModel>? AvailableSections { get; set; }
    public List<QuestionAssignmentItemViewModel>? AvailableQuestions { get; set; }
}

public class AssignmentProgressViewModel
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string TargetOrganizationName { get; set; } = string.Empty;
    
    public int TotalQuestions { get; set; }
    public int AssignedQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public decimal AssignmentProgress { get; set; }  // Percentage of questions assigned
    public decimal CompletionProgress { get; set; }  // Percentage of assigned questions completed
    
    public List<UserProgressViewModel> UserProgress { get; set; } = new();
    public List<SectionProgressViewModel> SectionProgress { get; set; } = new();
}

public class UserProgressViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AssignedQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public decimal CompletionPercentage { get; set; }
    public DateTime? LastActivity { get; set; }
    public bool IsOverdue { get; set; }
}

public class SectionProgressViewModel
{
    public string SectionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int AssignedQuestions { get; set; }
    public int CompletedQuestions { get; set; }
    public decimal AssignmentPercentage { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<string> AssignedUsers { get; set; } = new();
} 