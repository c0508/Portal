using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public class OrganizationRelationship
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PlatformOrganizationId { get; set; }

    [Required]
    public int SupplierOrganizationId { get; set; }

    [Required]
    [StringLength(50)]
    public string RelationshipType { get; set; } = "Supplier";

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [StringLength(450)] // IdentityUser Id length
    public string? CreatedByUserId { get; set; }

    // Track which organization created this relationship (for styling inheritance)
    public int? CreatedByOrganizationId { get; set; }

    // Mark this as the primary relationship for styling purposes
    public bool IsPrimaryRelationship { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(PlatformOrganizationId))]
    public virtual Organization PlatformOrganization { get; set; } = null!;

    [ForeignKey(nameof(SupplierOrganizationId))]
    public virtual Organization SupplierOrganization { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey(nameof(CreatedByOrganizationId))]
    public virtual Organization? CreatedByOrganization { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();
    public virtual ICollection<OrganizationRelationshipAttribute> Attributes { get; set; } = new List<OrganizationRelationshipAttribute>();

    // Computed properties
    [NotMapped]
    public string RelationshipDisplayName => $"{PlatformOrganization?.Name} → {SupplierOrganization?.Name} ({RelationshipType})";
}

// Relationship-specific attributes (replacing global organization attributes)
public class OrganizationRelationshipAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationRelationshipId { get; set; }

    [Required]
    [StringLength(50)]
    public string AttributeType { get; set; } = string.Empty; // 'ABC_SEGMENTATION', 'SUPPLIER_CLASSIFICATION', etc.

    [Required]
    [StringLength(100)]
    public string AttributeValue { get; set; } = string.Empty; // 'A', 'Strategic', etc.

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationRelationshipId))]
    public virtual OrganizationRelationship OrganizationRelationship { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User? CreatedByUser { get; set; }

    // Helper properties
    [NotMapped]
    public string DisplayText => $"{AttributeType}: {AttributeValue}";
} 