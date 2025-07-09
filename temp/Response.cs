using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Response
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int CampaignAssignmentId { get; set; }

    public string ResponderId { get; set; } = null!;

    public string? TextValue { get; set; }

    public decimal? NumericValue { get; set; }

    public DateTime? DateValue { get; set; }

    public bool? BooleanValue { get; set; }

    public string? SelectedValues { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsPrePopulated { get; set; }

    public bool IsPrePopulatedAccepted { get; set; }

    public int? SourceResponseId { get; set; }

    public int Status { get; set; }

    public DateTime? StatusUpdatedAt { get; set; }

    public string? StatusUpdatedById { get; set; }

    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    public virtual ICollection<FileUpload> FileUploads { get; set; } = new List<FileUpload>();

    public virtual ICollection<Response> InverseSourceResponse { get; set; } = new List<Response>();

    public virtual Question Question { get; set; } = null!;

    public virtual User Responder { get; set; } = null!;

    public virtual ICollection<ResponseChange> ResponseChanges { get; set; } = new List<ResponseChange>();

    public virtual ICollection<ResponseOverride> ResponseOverrides { get; set; } = new List<ResponseOverride>();

    public virtual ICollection<ResponseStatusHistory> ResponseStatusHistories { get; set; } = new List<ResponseStatusHistory>();

    public virtual ResponseWorkflow? ResponseWorkflow { get; set; }

    public virtual ICollection<ReviewAuditLog> ReviewAuditLogs { get; set; } = new List<ReviewAuditLog>();

    public virtual ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();

    public virtual Response? SourceResponse { get; set; }

    public virtual User? StatusUpdatedBy { get; set; }
}
