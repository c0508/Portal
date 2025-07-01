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