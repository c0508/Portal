using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public class Questionnaire
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public bool IsTemplate { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<QuestionnaireVersion> Versions { get; set; } = new List<QuestionnaireVersion>();
}

public class QuestionnaireVersion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionnaireId { get; set; }

    [Required]
    [StringLength(50)]
    public string VersionNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ChangeDescription { get; set; }

    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false; // Locked versions cannot be modified

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(QuestionnaireId))]
    public virtual Questionnaire Questionnaire { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();
} 