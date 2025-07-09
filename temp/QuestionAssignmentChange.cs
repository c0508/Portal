using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionAssignmentChange
{
    public int Id { get; set; }

    public int CampaignAssignmentId { get; set; }

    public int? QuestionId { get; set; }

    public string? SectionName { get; set; }

    public string ChangedById { get; set; } = null!;

    public string? OldAssignedUserId { get; set; }

    public string? NewAssignedUserId { get; set; }

    public string? OldInstructions { get; set; }

    public string? NewInstructions { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual User ChangedBy { get; set; } = null!;

    public virtual User? NewAssignedUser { get; set; }

    public virtual User? OldAssignedUser { get; set; }

    public virtual Question? Question { get; set; }
}
