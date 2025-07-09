using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionnaireVersion
{
    public int Id { get; set; }

    public int QuestionnaireId { get; set; }

    public string VersionNumber { get; set; } = null!;

    public string? ChangeDescription { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Questionnaire Questionnaire { get; set; } = null!;
}
