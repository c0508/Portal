using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum OrganizationType
{
    PlatformOrganization = 1,    // Can create campaigns, questionnaires, send to suppliers
    SupplierOrganization = 2     // Primarily responds to questionnaires from platform orgs
}

// Removed hardcoded enums - using dynamic attribute system instead

public class Organization
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public OrganizationType Type { get; set; } = OrganizationType.SupplierOrganization;

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(7)] // #FFFFFF format
    public string? PrimaryColor { get; set; }

    [StringLength(7)] // #FFFFFF format
    public string? SecondaryColor { get; set; }

    [StringLength(7)] // #FFFFFF format
    public string? AccentColor { get; set; }

    [StringLength(7)] // #FFFFFF format  
    public string? NavigationTextColor { get; set; }

    [StringLength(50)]
    public string? Theme { get; set; } = "Default";

    public bool IsActive { get; set; } = true;

    // Note: Organization attributes are now relationship-specific via OrganizationRelationshipAttribute
    // This allows suppliers to have different attributes per platform organization relationship

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Campaign> CampaignsCreated { get; set; } = new List<Campaign>();
    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    // Organization Relationship navigation properties
    // When this organization is the Platform Organization (has suppliers)
    public virtual ICollection<OrganizationRelationship> SupplierRelationships { get; set; } = new List<OrganizationRelationship>();
    
    // When this organization is the Supplier Organization (works for platform orgs)
    public virtual ICollection<OrganizationRelationship> PlatformRelationships { get; set; } = new List<OrganizationRelationship>();

    // Computed properties
    [NotMapped]
    public bool IsPlatformOrganization => Type == OrganizationType.PlatformOrganization;
    
    [NotMapped]
    public bool IsSupplierOrganization => Type == OrganizationType.SupplierOrganization;
    
    [NotMapped]
    public string TypeDisplayName => Type switch
    {
        OrganizationType.PlatformOrganization => "Platform Organization",
        OrganizationType.SupplierOrganization => "Supplier Organization",
        _ => "Unknown"
    };

    // Note: Display properties are now handled via OrganizationRelationshipAttribute.DisplayText
    // Each relationship can have different attribute values for the same organization

    // Helper methods for relationships (no NotMapped needed for methods)
    public IEnumerable<Organization> GetSupplierOrganizations() => 
        SupplierRelationships.Where(r => r.IsActive).Select(r => r.SupplierOrganization);

    public IEnumerable<Organization> GetPlatformOrganizations() => 
        PlatformRelationships.Where(r => r.IsActive).Select(r => r.PlatformOrganization);

    public bool HasRelationshipWith(int organizationId) =>
        SupplierRelationships.Any(r => r.IsActive && r.SupplierOrganizationId == organizationId) ||
        PlatformRelationships.Any(r => r.IsActive && r.PlatformOrganizationId == organizationId);
}

// Master Data Tables for Organization Attributes
public class OrganizationAttributeType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty; // e.g., "INDUSTRY", "REGION", "SIZE_CATEGORY"

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Industry Types", "Geographic Regions"

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<OrganizationAttributeValue> Values { get; set; } = new List<OrganizationAttributeValue>();
}

public class OrganizationAttributeValue
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AttributeTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty; // e.g., "TECH", "MANUFACTURING"

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty; // e.g., "Technology", "Manufacturing"

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(7)] // For color coding if needed
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AttributeTypeId))]
    public virtual OrganizationAttributeType AttributeType { get; set; } = null!;
} 