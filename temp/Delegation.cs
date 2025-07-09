using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Delegation
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int CampaignAssignmentId { get; set; }

    public string FromUserId { get; set; } = null!;

    public string ToUserId { get; set; } = null!;

    public string? Instructions { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual User FromUser { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual User ToUser { get; set; } = null!;
}
