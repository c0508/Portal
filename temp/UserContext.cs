using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class UserContext
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int OrganizationId { get; set; }

    public int? ActivePlatformOrganizationId { get; set; }

    public DateTime LastSwitched { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Organization? ActivePlatformOrganization { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
