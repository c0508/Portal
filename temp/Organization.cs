using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Organization
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? Theme { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int Type { get; set; }

    public string? AccentColor { get; set; }

    public string? NavigationTextColor { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<OrganizationRelationship> OrganizationRelationshipCreatedByOrganizations { get; set; } = new List<OrganizationRelationship>();

    public virtual ICollection<OrganizationRelationship> OrganizationRelationshipPlatformOrganizations { get; set; } = new List<OrganizationRelationship>();

    public virtual ICollection<OrganizationRelationship> OrganizationRelationshipSupplierOrganizations { get; set; } = new List<OrganizationRelationship>();

    public virtual ICollection<Questionnaire> Questionnaires { get; set; } = new List<Questionnaire>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<UserContext> UserContextActivePlatformOrganizations { get; set; } = new List<UserContext>();

    public virtual ICollection<UserContext> UserContextOrganizations { get; set; } = new List<UserContext>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
