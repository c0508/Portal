using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class OrganizationViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Organization Type")]
    public OrganizationType Type { get; set; } = OrganizationType.SupplierOrganization;

    [StringLength(500)]
    [Display(Name = "Logo URL")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? LogoUrl { get; set; }

    [StringLength(7)]
    [Display(Name = "Primary Color")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color (e.g., #007bff)")]
    public string? PrimaryColor { get; set; }

    [StringLength(7)]
    [Display(Name = "Secondary Color")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color (e.g., #6c757d)")]
    public string? SecondaryColor { get; set; }

    [StringLength(7)]
    [Display(Name = "Accent Color")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color (e.g., #e74c3c)")]
    public string? AccentColor { get; set; }

    [StringLength(50)]
    [Display(Name = "Theme")]
    public string? Theme { get; set; } = "Default";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Note: Organization attributes are now handled via OrganizationRelationshipAttribute
    // This allows different attributes per platform-supplier relationship

    // Computed properties for display
    public string TypeDisplayName => Type switch
    {
        OrganizationType.PlatformOrganization => "Platform Organization",
        OrganizationType.SupplierOrganization => "Supplier Organization",
        _ => "Unknown"
    };

    public string TypeDescription => Type switch
    {
        OrganizationType.PlatformOrganization => "Can create campaigns and questionnaires, send to suppliers",
        OrganizationType.SupplierOrganization => "Primarily responds to questionnaires from platform organizations",
        _ => "Unknown organization type"
    };

    // Note: Attribute display logic has moved to relationship-specific context
    // Each platform-supplier relationship can have different attribute values
}

public class UserInviteViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
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
    [Display(Name = "Organization")]
    public int OrganizationId { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    public string? OrganizationName { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Organizations { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Roles { get; set; } = new();
}

public class UserManagementViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
} 