using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class User
{
    public string Id { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int OrganizationId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public virtual ICollection<CampaignAssignment> CampaignAssignments { get; set; } = new List<CampaignAssignment>();

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<Delegation> DelegationFromUsers { get; set; } = new List<Delegation>();

    public virtual ICollection<Delegation> DelegationToUsers { get; set; } = new List<Delegation>();

    public virtual ICollection<FileUpload> FileUploads { get; set; } = new List<FileUpload>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<OrganizationRelationshipAttribute> OrganizationRelationshipAttributes { get; set; } = new List<OrganizationRelationshipAttribute>();

    public virtual ICollection<OrganizationRelationship> OrganizationRelationships { get; set; } = new List<OrganizationRelationship>();

    public virtual ICollection<QuestionAssignment> QuestionAssignmentAssignedUsers { get; set; } = new List<QuestionAssignment>();

    public virtual ICollection<QuestionAssignmentChange> QuestionAssignmentChangeChangedBies { get; set; } = new List<QuestionAssignmentChange>();

    public virtual ICollection<QuestionAssignmentChange> QuestionAssignmentChangeNewAssignedUsers { get; set; } = new List<QuestionAssignmentChange>();

    public virtual ICollection<QuestionAssignmentChange> QuestionAssignmentChangeOldAssignedUsers { get; set; } = new List<QuestionAssignmentChange>();

    public virtual ICollection<QuestionAssignment> QuestionAssignmentCreatedBies { get; set; } = new List<QuestionAssignment>();

    public virtual ICollection<QuestionChange> QuestionChanges { get; set; } = new List<QuestionChange>();

    public virtual ICollection<QuestionDependency> QuestionDependencies { get; set; } = new List<QuestionDependency>();

    public virtual ICollection<QuestionnaireVersion> QuestionnaireVersions { get; set; } = new List<QuestionnaireVersion>();

    public virtual ICollection<Questionnaire> Questionnaires { get; set; } = new List<Questionnaire>();

    public virtual ICollection<ResponseChange> ResponseChanges { get; set; } = new List<ResponseChange>();

    public virtual ICollection<ResponseOverride> ResponseOverrideOriginalResponders { get; set; } = new List<ResponseOverride>();

    public virtual ICollection<ResponseOverride> ResponseOverrideOverriddenBies { get; set; } = new List<ResponseOverride>();

    public virtual ICollection<Response> ResponseResponders { get; set; } = new List<Response>();

    public virtual ICollection<ResponseStatusHistory> ResponseStatusHistories { get; set; } = new List<ResponseStatusHistory>();

    public virtual ICollection<Response> ResponseStatusUpdatedBies { get; set; } = new List<Response>();

    public virtual ICollection<ResponseWorkflow> ResponseWorkflows { get; set; } = new List<ResponseWorkflow>();

    public virtual ICollection<ReviewAssignment> ReviewAssignmentAssignedBies { get; set; } = new List<ReviewAssignment>();

    public virtual ICollection<ReviewAssignment> ReviewAssignmentReviewers { get; set; } = new List<ReviewAssignment>();

    public virtual ICollection<ReviewAuditLog> ReviewAuditLogs { get; set; } = new List<ReviewAuditLog>();

    public virtual ICollection<ReviewComment> ReviewCommentResolvedBies { get; set; } = new List<ReviewComment>();

    public virtual ICollection<ReviewComment> ReviewCommentReviewers { get; set; } = new List<ReviewComment>();

    public virtual ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();

    public virtual ICollection<UserContext> UserContexts { get; set; } = new List<UserContext>();

    public virtual ICollection<UserLogin> UserLogins { get; set; } = new List<UserLogin>();

    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
