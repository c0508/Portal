using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class OrganizationRelationship
{
    public int Id { get; set; }

    public int PlatformOrganizationId { get; set; }

    public int SupplierOrganizationId { get; set; }

    public string RelationshipType { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public int? CreatedByOrganizationId { get; set; }

    public bool IsPrimaryRelationship { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    public virtual Organization? CreatedByOrganization { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<OrganizationRelationshipAttribute> OrganizationRelationshipAttributes { get; set; } = new List<OrganizationRelationshipAttribute>();

    public virtual Organization PlatformOrganization { get; set; } = null!;

    public virtual Organization SupplierOrganization { get; set; } = null!;
}
