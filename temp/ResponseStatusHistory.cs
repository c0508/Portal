using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ResponseStatusHistory
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public int FromStatus { get; set; }

    public int ToStatus { get; set; }

    public string ChangedById { get; set; } = null!;

    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedBy { get; set; } = null!;

    public virtual Response Response { get; set; } = null!;
}
