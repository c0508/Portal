using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ReviewAuditLog
{
    public int Id { get; set; }

    public int CampaignAssignmentId { get; set; }

    public int? QuestionId { get; set; }

    public int? ResponseId { get; set; }

    public int? ReviewAssignmentId { get; set; }

    public string UserId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual Question? Question { get; set; }

    public virtual Response? Response { get; set; }

    public virtual ReviewAssignment? ReviewAssignment { get; set; }

    public virtual User User { get; set; } = null!;
}
