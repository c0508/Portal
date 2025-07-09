using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class OrganizationAttributeValue
{
    public int Id { get; set; }

    public int AttributeTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Color { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual OrganizationAttributeType AttributeType { get; set; } = null!;
}
