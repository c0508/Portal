using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class OrganizationRelationshipViewModel
{
    public int Id { get; set; }
    
    [Required]
    [Display(Name = "Platform Organization")]
    public int PlatformOrganizationId { get; set; }
    
    [Required]
    [Display(Name = "Supplier Organization")]
    public int SupplierOrganizationId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Relationship Type")]
    public string RelationshipType { get; set; } = "Supplier";
    
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Primary Relationship")]
    public bool IsPrimaryRelationship { get; set; } = false;
    
    // Display properties
    public string PlatformOrganizationName { get; set; } = string.Empty;
    public string SupplierOrganizationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
    
    // Dropdown lists for form
    public List<SelectListItem>? AvailablePlatformOrganizations { get; set; }
    public List<SelectListItem>? AvailableSupplierOrganizations { get; set; }
    public List<SelectListItem>? AvailableRelationshipTypes { get; set; }
    
    // Relationship attributes
    public List<OrganizationRelationshipAttributeViewModel> Attributes { get; set; } = new List<OrganizationRelationshipAttributeViewModel>();
}

public class OrganizationRelationshipListViewModel
{
    public List<OrganizationRelationshipSummaryViewModel> Relationships { get; set; } = new List<OrganizationRelationshipSummaryViewModel>();
    public string? SearchFilter { get; set; }
    public bool? ActiveFilter { get; set; }
    public int? PlatformOrganizationFilter { get; set; }
    public int? SupplierOrganizationFilter { get; set; }
    
    // Dropdown filters
    public List<SelectListItem>? AvailablePlatformOrganizations { get; set; }
    public List<SelectListItem>? AvailableSupplierOrganizations { get; set; }
    
    // Statistics
    public int TotalRelationships { get; set; }
    public int ActiveRelationships { get; set; }
    public int InactiveRelationships { get; set; }
}

public class OrganizationRelationshipSummaryViewModel
{
    public int Id { get; set; }
    public string PlatformOrganizationName { get; set; } = string.Empty;
    public string SupplierOrganizationName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPrimaryRelationship { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
    public int AttributeCount { get; set; }
    public int CampaignAssignmentCount { get; set; }
    public string RelationshipDisplayName { get; set; } = string.Empty;
}

public class OrganizationRelationshipAttributeViewModel
{
    public int Id { get; set; }
    public int OrganizationRelationshipId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Attribute Type")]
    public string AttributeType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Attribute Value")]
    public string AttributeValue { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Display properties
    public string DisplayText { get; set; } = string.Empty;
}

public class OrganizationRelationshipCreateViewModel
{
    [Required]
    [Display(Name = "Platform Organization")]
    public int PlatformOrganizationId { get; set; }
    
    [Required]
    [Display(Name = "Supplier Organization")]
    public int SupplierOrganizationId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Relationship Type")]
    public string RelationshipType { get; set; } = "Supplier";
    
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Primary Relationship")]
    public bool IsPrimaryRelationship { get; set; } = false;
    
    // Dropdown lists
    public List<SelectListItem>? AvailablePlatformOrganizations { get; set; }
    public List<SelectListItem>? AvailableSupplierOrganizations { get; set; }
    public List<SelectListItem>? AvailableRelationshipTypes { get; set; }
}

public class ManageRelationshipAttributesViewModel
{
    public int OrganizationRelationshipId { get; set; }
    public string RelationshipDisplayName { get; set; } = string.Empty;
    public List<OrganizationRelationshipAttributeViewModel> Attributes { get; set; } = new List<OrganizationRelationshipAttributeViewModel>();
    
    // Available attribute types from master data
    public List<SelectListItem>? AvailableAttributeTypes { get; set; }
    public Dictionary<string, List<SelectListItem>> AvailableAttributeValues { get; set; } = new Dictionary<string, List<SelectListItem>>();
    
    // New attribute form
    public OrganizationRelationshipAttributeViewModel NewAttribute { get; set; } = new OrganizationRelationshipAttributeViewModel();
} 