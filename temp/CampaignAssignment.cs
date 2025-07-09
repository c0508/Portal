using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class CampaignAssignment
{
    public int Id { get; set; }

    public int CampaignId { get; set; }

    public int TargetOrganizationId { get; set; }

    public int QuestionnaireVersionId { get; set; }

    public string? LeadResponderId { get; set; }

    public int Status { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OrganizationRelationshipId { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual ICollection<Delegation> Delegations { get; set; } = new List<Delegation>();

    public virtual User? LeadResponder { get; set; }

    public virtual OrganizationRelationship? OrganizationRelationship { get; set; }

    public virtual ICollection<QuestionAssignmentChange> QuestionAssignmentChanges { get; set; } = new List<QuestionAssignmentChange>();

    public virtual ICollection<QuestionAssignment> QuestionAssignments { get; set; } = new List<QuestionAssignment>();

    public virtual QuestionnaireVersion QuestionnaireVersion { get; set; } = null!;

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();

    public virtual ICollection<ReviewAssignment> ReviewAssignments { get; set; } = new List<ReviewAssignment>();

    public virtual ICollection<ReviewAuditLog> ReviewAuditLogs { get; set; } = new List<ReviewAuditLog>();

    public virtual Organization TargetOrganization { get; set; } = null!;
}
