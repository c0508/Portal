using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Campaign
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int OrganizationId { get; set; }

    public int Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? Deadline { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedById { get; set; }

    public DateTime? ReportingPeriodEnd { get; set; }

    public DateTime? ReportingPeriodStart { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    public virtual User? CreatedBy { get; set; }

    public virtual Organization Organization { get; set; } = null!;
}
