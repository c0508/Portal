using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum CampaignStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Cancelled
}

public class Campaign
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int OrganizationId { get; set; } // The organization launching the campaign

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? Deadline { get; set; }

    // Reporting period - the time period that the data collection covers
    [Display(Name = "Reporting Period Start")]
    public DateTime? ReportingPeriodStart { get; set; }
    
    [Display(Name = "Reporting Period End")]
    public DateTime? ReportingPeriodEnd { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [StringLength(450)]
    public string? CreatedById { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<CampaignAssignment> Assignments { get; set; } = new List<CampaignAssignment>();
}

public enum AssignmentStatus
{
    NotStarted,
    InProgress,
    Submitted,
    UnderReview,
    Approved,
    ChangesRequested
}

public class CampaignAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CampaignId { get; set; }

    [Required]
    public int TargetOrganizationId { get; set; } // The organization receiving the questionnaire

    [Required]
    public int QuestionnaireVersionId { get; set; }

    // Optional: Link to the specific relationship context (null for internal campaigns)
    public int? OrganizationRelationshipId { get; set; }

    [StringLength(450)]
    public string? LeadResponderId { get; set; } // User assigned as lead responder

    public AssignmentStatus Status { get; set; } = AssignmentStatus.NotStarted;

    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    [StringLength(1000)]
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign Campaign { get; set; } = null!;

    [ForeignKey(nameof(TargetOrganizationId))]
    public virtual Organization TargetOrganization { get; set; } = null!;

    [ForeignKey(nameof(QuestionnaireVersionId))]
    public virtual QuestionnaireVersion QuestionnaireVersion { get; set; } = null!;

    [ForeignKey(nameof(OrganizationRelationshipId))]
    public virtual OrganizationRelationship? OrganizationRelationship { get; set; }

    [ForeignKey(nameof(LeadResponderId))]
    public virtual User? LeadResponder { get; set; }

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();
    public virtual ICollection<QuestionAssignment> QuestionAssignments { get; set; } = new List<QuestionAssignment>();

    // Computed properties
    [NotMapped]
    public bool IsInternalAssignment => OrganizationRelationshipId == null;

    [NotMapped]
    public bool IsExternalAssignment => OrganizationRelationshipId != null;
} 