using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ReviewAssignment
{
    public int Id { get; set; }

    public int CampaignAssignmentId { get; set; }

    public string ReviewerId { get; set; } = null!;

    public int Scope { get; set; }

    public int? QuestionId { get; set; }

    public string? SectionName { get; set; }

    public int Status { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string AssignedById { get; set; } = null!;

    public virtual User AssignedBy { get; set; } = null!;

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual Question? Question { get; set; }

    public virtual ICollection<ReviewAuditLog> ReviewAuditLogs { get; set; } = new List<ReviewAuditLog>();

    public virtual ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();

    public virtual User Reviewer { get; set; } = null!;
}
