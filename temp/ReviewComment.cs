using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ReviewComment
{
    public int Id { get; set; }

    public int ReviewAssignmentId { get; set; }

    public int ResponseId { get; set; }

    public string ReviewerId { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public int ActionTaken { get; set; }

    public bool RequiresChange { get; set; }

    public bool IsResolved { get; set; }

    public string? ResolvedById { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public virtual User? ResolvedBy { get; set; }

    public virtual Response Response { get; set; } = null!;

    public virtual ReviewAssignment ReviewAssignment { get; set; } = null!;

    public virtual User Reviewer { get; set; } = null!;
}
