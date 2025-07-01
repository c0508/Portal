using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum ReviewStatus
{
    Pending,
    InReview,
    Approved,
    ChangesRequested,
    Completed
}

public enum ReviewScope
{
    Question,    // Individual question review
    Section,     // Section-level review
    Assignment   // Entire assignment review
}

/// <summary>
/// Assigns reviewers to questions, sections, or entire assignments
/// </summary>
public class ReviewAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    [Required]
    [StringLength(450)]
    public string ReviewerId { get; set; } = string.Empty;

    [Required]
    public ReviewScope Scope { get; set; }

    // For question-level reviews
    public int? QuestionId { get; set; }

    // For section-level reviews
    [StringLength(255)]
    public string? SectionName { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    [StringLength(1000)]
    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string AssignedById { get; set; } = string.Empty; // Lead responder who assigned the reviewer

    // Navigation properties
    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(ReviewerId))]
    public virtual User Reviewer { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public virtual Question? Question { get; set; }

    [ForeignKey(nameof(AssignedById))]
    public virtual User AssignedBy { get; set; } = null!;

    public virtual ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();

    // Helper properties
    [NotMapped]
    public bool IsQuestionReview => Scope == ReviewScope.Question;

    [NotMapped]
    public bool IsSectionReview => Scope == ReviewScope.Section;

    [NotMapped]
    public bool IsAssignmentReview => Scope == ReviewScope.Assignment;
}

/// <summary>
/// Comments and feedback from reviewers on responses
/// </summary>
public class ReviewComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReviewAssignmentId { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    [StringLength(450)]
    public string ReviewerId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Comment { get; set; } = string.Empty;

    public ReviewStatus ActionTaken { get; set; } = ReviewStatus.InReview;

    // Tracks if this comment requires the responder to make changes
    public bool RequiresChange { get; set; } = false;

    // For tracking if the responder has addressed the comment
    public bool IsResolved { get; set; } = false;

    [StringLength(450)]
    public string? ResolvedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ReviewAssignmentId))]
    public virtual ReviewAssignment ReviewAssignment { get; set; } = null!;

    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(ReviewerId))]
    public virtual User Reviewer { get; set; } = null!;

    [ForeignKey(nameof(ResolvedById))]
    public virtual User? ResolvedBy { get; set; }
}

/// <summary>
/// Audit trail for all review actions and workflow changes
/// </summary>
public class ReviewAuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CampaignAssignmentId { get; set; }

    public int? QuestionId { get; set; }

    public int? ResponseId { get; set; }

    public int? ReviewAssignmentId { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty; // e.g., "ReviewAssigned", "CommentAdded", "StatusChanged", "ChangesRequested"

    [StringLength(100)]
    public string? FromStatus { get; set; }

    [StringLength(100)]
    public string? ToStatus { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Details { get; set; } // JSON or text details of the action

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CampaignAssignmentId))]
    public virtual CampaignAssignment CampaignAssignment { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public virtual Question? Question { get; set; }

    [ForeignKey(nameof(ResponseId))]
    public virtual Response? Response { get; set; }

    [ForeignKey(nameof(ReviewAssignmentId))]
    public virtual ReviewAssignment? ReviewAssignment { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Tracks workflow states for responses that require review
/// </summary>
public class ResponseWorkflow
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    public ReviewStatus CurrentStatus { get; set; } = ReviewStatus.Pending;

    public DateTime? SubmittedForReviewAt { get; set; }
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? ReviewCompletedAt { get; set; }

    [StringLength(450)]
    public string? CurrentReviewerId { get; set; }

    // Track how many times response has been sent back for changes
    public int RevisionCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ResponseId))]
    public virtual Response Response { get; set; } = null!;

    [ForeignKey(nameof(CurrentReviewerId))]
    public virtual User? CurrentReviewer { get; set; }
} 