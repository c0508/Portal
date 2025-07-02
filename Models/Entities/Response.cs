using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum ResponseStatus
{
    NotStarted = 0,
    PrePopulated = 1,
    Draft = 2,
    Answered = 3,
    SubmittedForReview = 4,
    UnderReview = 5,
    ChangesRequested = 6,
    ReviewApproved = 7,
    Final = 8
}

public class Response
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    [Required]
    [StringLength(450)]
    public string ResponderId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? TextValue { get; set; }

    public decimal? NumericValue { get; set; }

    public DateTime? DateValue { get; set; }

    public bool? BooleanValue { get; set; }

    // For multiple choice selections - JSON array
    [Column(TypeName = "nvarchar(max)")]
    public string? SelectedValues { get; set; }

    // Response Status Management
    public ResponseStatus Status { get; set; } = ResponseStatus.NotStarted;
    public DateTime? StatusUpdatedAt { get; set; }
    
    [StringLength(450)]
    public string? StatusUpdatedById { get; set; }

    // Pre-population tracking
    public bool IsPrePopulated { get; set; } = false;
    public int? SourceResponseId { get; set; } // Reference to the original response this was copied from
    public bool IsPrePopulatedAccepted { get; set; } = false; // Whether user has accepted the pre-populated value

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(ResponderId))]
    public virtual User Responder { get; set; } = null!;

    [ForeignKey(nameof(SourceResponseId))]
    public virtual Response? SourceResponse { get; set; }

    [ForeignKey(nameof(StatusUpdatedById))]
    public virtual User? StatusUpdatedBy { get; set; }

    public virtual ICollection<ResponseChange> Changes { get; set; } = new List<ResponseChange>();
    public virtual ICollection<FileUpload> FileUploads { get; set; } = new List<FileUpload>();
    public virtual ICollection<ResponseOverride> Overrides { get; set; } = new List<ResponseOverride>();
    public virtual ICollection<ResponseStatusHistory> StatusHistory { get; set; } = new List<ResponseStatusHistory>();
}

public class ResponseChange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    [StringLength(450)]
    public string ChangedById { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValue { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValue { get; set; }

    [StringLength(500)]
    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(ChangedById))]
    public virtual User ChangedBy { get; set; } = null!;
}

public class Delegation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    [Required]
    [StringLength(450)]
    public string FromUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string ToUserId { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Instructions { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(FromUserId))]
    public virtual User FromUser { get; set; } = null!;

    [ForeignKey(nameof(ToUserId))]
    public virtual User ToUser { get; set; } = null!;
}

public class FileUpload
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    [StringLength(450)]
    public string UploadedById { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(UploadedById))]
    public virtual User UploadedBy { get; set; } = null!;
}

public class ResponseStatusHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    public ResponseStatus FromStatus { get; set; }

    [Required]
    public ResponseStatus ToStatus { get; set; }

    [Required]
    [StringLength(450)]
    public string ChangedById { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(ChangedById))]
    public virtual User ChangedBy { get; set; } = null!;
} 