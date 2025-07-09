using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class OrganizationRelationshipAttribute
{
    public int Id { get; set; }

    public int OrganizationRelationshipId { get; set; }

    public string AttributeType { get; set; } = null!;

    public string AttributeValue { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual OrganizationRelationship OrganizationRelationship { get; set; } = null!;
}
