using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ResponseOverride
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public string OverriddenById { get; set; } = null!;

    public string OriginalResponderId { get; set; } = null!;

    public string? OriginalValue { get; set; }

    public string? OverrideValue { get; set; }

    public string? OverrideReason { get; set; }

    public DateTime OverriddenAt { get; set; }

    public virtual User OriginalResponder { get; set; } = null!;

    public virtual User OverriddenBy { get; set; } = null!;

    public virtual Response Response { get; set; } = null!;
}
