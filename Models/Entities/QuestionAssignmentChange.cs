using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public class QuestionAssignmentChange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    // Either QuestionId OR SectionName will be set
    public int? QuestionId { get; set; }
    
    [StringLength(255)]
    public string? SectionName { get; set; }

    [Required]
    [StringLength(450)]
    public string ChangedById { get; set; } = string.Empty;

    [StringLength(450)]
    public string? OldAssignedUserId { get; set; }  // Who was previously assigned (null for new assignments)

    [StringLength(450)]  
    public string? NewAssignedUserId { get; set; }  // Who is now assigned (null for removals)

    [StringLength(1000)]
    public string? OldInstructions { get; set; }

    [StringLength(1000)]
    public string? NewInstructions { get; set; }

    [Required]
    [StringLength(100)]
    public string ChangeType { get; set; } = string.Empty; // "Created", "Modified", "Removed"

    [StringLength(500)]
    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public virtual Question? Question { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public virtual User ChangedBy { get; set; } = null!;

    [ForeignKey(nameof(OldAssignedUserId))]
    public virtual User? OldAssignedUser { get; set; }

    [ForeignKey(nameof(NewAssignedUserId))]
    public virtual User? NewAssignedUser { get; set; }
} 