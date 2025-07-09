using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class OrganizationAttributeType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrganizationAttributeValue> OrganizationAttributeValues { get; set; } = new List<OrganizationAttributeValue>();
}
