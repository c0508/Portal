using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionAssignment
{
    public int Id { get; set; }

    public int CampaignAssignmentId { get; set; }

    public int? QuestionId { get; set; }

    public string? SectionName { get; set; }

    public string AssignedUserId { get; set; } = null!;

    public int AssignmentType { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CreatedById { get; set; } = null!;

    public virtual User AssignedUser { get; set; } = null!;

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual User CreatedBy { get; set; } = null!;

    public virtual Question? Question { get; set; }
}
