using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum QuestionAssignmentType
{
    Question,  // Individual question assignment
    Section    // Entire section assignment
}

public class QuestionAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    // Either QuestionId OR SectionName will be set, not both
    public int? QuestionId { get; set; }  // For individual question assignment

    [StringLength(255)]
    public string? SectionName { get; set; }  // For section assignment

    [Required]
    [StringLength(450)]
    public string AssignedUserId { get; set; } = string.Empty;

    [Required]
    public QuestionAssignmentType AssignmentType { get; set; }

    [StringLength(1000)]
    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string CreatedById { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public virtual Question? Question { get; set; }

    [ForeignKey(nameof(AssignedUserId))]
    public virtual User AssignedUser { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public bool IsQuestionAssignment => AssignmentType == QuestionAssignmentType.Question;

    [NotMapped]
    public bool IsSectionAssignment => AssignmentType == QuestionAssignmentType.Section;
}

public class ResponseOverride
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    [StringLength(450)]
    public string OverriddenById { get; set; } = string.Empty;  // Lead responder who made the override

    [Required]
    [StringLength(450)]
    public string OriginalResponderId { get; set; } = string.Empty;  // User whose response was overridden

    [Column(TypeName = "nvarchar(max)")]
    public string? OriginalValue { get; set; }  // The original response value

    [Column(TypeName = "nvarchar(max)")]
    public string? OverrideValue { get; set; }  // The new response value

    [StringLength(1000)]
    public string? OverrideReason { get; set; }

    public DateTime OverriddenAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(OverriddenById))]
    public virtual User OverriddenBy { get; set; } = null!;

    [ForeignKey(nameof(OriginalResponderId))]
    public virtual User OriginalResponder { get; set; } = null!;
} 