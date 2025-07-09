using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.temp;

public partial class EsgplatformContext : DbContext
{
    public EsgplatformContext()
    {
    }

    public EsgplatformContext(DbContextOptions<EsgplatformContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CampaignAssignment> CampaignAssignments { get; set; }

    public virtual DbSet<Delegation> Delegations { get; set; }

    public virtual DbSet<FileUpload> FileUploads { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OrganizationAttributeType> OrganizationAttributeTypes { get; set; }

    public virtual DbSet<OrganizationAttributeValue> OrganizationAttributeValues { get; set; }

    public virtual DbSet<OrganizationRelationship> OrganizationRelationships { get; set; }

    public virtual DbSet<OrganizationRelationshipAttribute> OrganizationRelationshipAttributes { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAssignment> QuestionAssignments { get; set; }

    public virtual DbSet<QuestionAssignmentChange> QuestionAssignmentChanges { get; set; }

    public virtual DbSet<QuestionAttribute> QuestionAttributes { get; set; }

    public virtual DbSet<QuestionChange> QuestionChanges { get; set; }

    public virtual DbSet<QuestionDependency> QuestionDependencies { get; set; }

    public virtual DbSet<QuestionQuestionAttribute> QuestionQuestionAttributes { get; set; }

    public virtual DbSet<QuestionType> QuestionTypes { get; set; }

    public virtual DbSet<Questionnaire> Questionnaires { get; set; }

    public virtual DbSet<QuestionnaireVersion> QuestionnaireVersions { get; set; }

    public virtual DbSet<Response> Responses { get; set; }

    public virtual DbSet<ResponseChange> ResponseChanges { get; set; }

    public virtual DbSet<ResponseOverride> ResponseOverrides { get; set; }

    public virtual DbSet<ResponseStatusHistory> ResponseStatusHistories { get; set; }

    public virtual DbSet<ResponseWorkflow> ResponseWorkflows { get; set; }

    public virtual DbSet<ReviewAssignment> ReviewAssignments { get; set; }

    public virtual DbSet<ReviewAuditLog> ReviewAuditLogs { get; set; }

    public virtual DbSet<ReviewComment> ReviewComments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleClaim> RoleClaims { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserClaim> UserClaims { get; set; }

    public virtual DbSet<UserContext> UserContexts { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasIndex(e => e.CreatedById, "IX_Campaigns_CreatedById");

            entity.HasIndex(e => e.OrganizationId, "IX_Campaigns_OrganizationId");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.Campaigns).HasForeignKey(d => d.CreatedById);

            entity.HasOne(d => d.Organization).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CampaignAssignment>(entity =>
        {
            entity.HasIndex(e => e.CampaignId, "IX_CampaignAssignments_CampaignId");

            entity.HasIndex(e => e.LeadResponderId, "IX_CampaignAssignments_LeadResponderId");

            entity.HasIndex(e => e.OrganizationRelationshipId, "IX_CampaignAssignments_OrganizationRelationshipId");

            entity.HasIndex(e => e.QuestionnaireVersionId, "IX_CampaignAssignments_QuestionnaireVersionId");

            entity.HasIndex(e => e.TargetOrganizationId, "IX_CampaignAssignments_TargetOrganizationId");

            entity.Property(e => e.ReviewNotes).HasMaxLength(1000);

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignAssignments).HasForeignKey(d => d.CampaignId);

            entity.HasOne(d => d.LeadResponder).WithMany(p => p.CampaignAssignments).HasForeignKey(d => d.LeadResponderId);

            entity.HasOne(d => d.OrganizationRelationship).WithMany(p => p.CampaignAssignments)
                .HasForeignKey(d => d.OrganizationRelationshipId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.QuestionnaireVersion).WithMany(p => p.CampaignAssignments).HasForeignKey(d => d.QuestionnaireVersionId);

            entity.HasOne(d => d.TargetOrganization).WithMany(p => p.CampaignAssignments)
                .HasForeignKey(d => d.TargetOrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Delegation>(entity =>
        {
            entity.HasIndex(e => e.CampaignAssignmentId, "IX_Delegations_CampaignAssignmentId");

            entity.HasIndex(e => e.FromUserId, "IX_Delegations_FromUserId");

            entity.HasIndex(e => e.QuestionId, "IX_Delegations_QuestionId");

            entity.HasIndex(e => e.ToUserId, "IX_Delegations_ToUserId");

            entity.Property(e => e.Instructions).HasMaxLength(1000);

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.Delegations)
                .HasForeignKey(d => d.CampaignAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.FromUser).WithMany(p => p.DelegationFromUsers)
                .HasForeignKey(d => d.FromUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Question).WithMany(p => p.Delegations)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ToUser).WithMany(p => p.DelegationToUsers)
                .HasForeignKey(d => d.ToUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.HasIndex(e => e.ResponseId, "IX_FileUploads_ResponseId");

            entity.HasIndex(e => e.UploadedById, "IX_FileUploads_UploadedById");

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);

            entity.HasOne(d => d.Response).WithMany(p => p.FileUploads)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UploadedBy).WithMany(p => p.FileUploads)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(e => e.AccentColor).HasMaxLength(7);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.NavigationTextColor).HasMaxLength(7);
            entity.Property(e => e.PrimaryColor).HasMaxLength(7);
            entity.Property(e => e.SecondaryColor).HasMaxLength(7);
            entity.Property(e => e.Theme).HasMaxLength(50);
        });

        modelBuilder.Entity<OrganizationAttributeType>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_OrganizationAttributeType_Code_Unique").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<OrganizationAttributeValue>(entity =>
        {
            entity.HasIndex(e => new { e.AttributeTypeId, e.Code }, "IX_OrganizationAttributeValue_Type_Code_Unique").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.AttributeType).WithMany(p => p.OrganizationAttributeValues).HasForeignKey(d => d.AttributeTypeId);
        });

        modelBuilder.Entity<OrganizationRelationship>(entity =>
        {
            entity.HasIndex(e => e.CreatedByOrganizationId, "IX_OrganizationRelationships_CreatedByOrganizationId");

            entity.HasIndex(e => e.CreatedByUserId, "IX_OrganizationRelationships_CreatedByUserId");

            entity.HasIndex(e => e.PlatformOrganizationId, "IX_OrganizationRelationships_PlatformOrganizationId");

            entity.HasIndex(e => new { e.PlatformOrganizationId, e.SupplierOrganizationId }, "IX_OrganizationRelationships_Platform_Supplier_Unique").IsUnique();

            entity.HasIndex(e => e.SupplierOrganizationId, "IX_OrganizationRelationships_SupplierOrganizationId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RelationshipType).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByOrganization).WithMany(p => p.OrganizationRelationshipCreatedByOrganizations)
                .HasForeignKey(d => d.CreatedByOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OrganizationRelationships)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.PlatformOrganization).WithMany(p => p.OrganizationRelationshipPlatformOrganizations)
                .HasForeignKey(d => d.PlatformOrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SupplierOrganization).WithMany(p => p.OrganizationRelationshipSupplierOrganizations)
                .HasForeignKey(d => d.SupplierOrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OrganizationRelationshipAttribute>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_OrganizationRelationshipAttributes_CreatedByUserId");

            entity.HasIndex(e => new { e.OrganizationRelationshipId, e.AttributeType }, "IX_OrganizationRelationshipAttributes_Unique_Relationship_AttributeType").IsUnique();

            entity.Property(e => e.AttributeType).HasMaxLength(50);
            entity.Property(e => e.AttributeValue).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OrganizationRelationshipAttributes)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.OrganizationRelationship).WithMany(p => p.OrganizationRelationshipAttributes).HasForeignKey(d => d.OrganizationRelationshipId);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId, "IX_Questions_OrganizationId");

            entity.HasIndex(e => e.QuestionTypeMasterId, "IX_Questions_QuestionTypeMasterId");

            entity.HasIndex(e => e.QuestionnaireId, "IX_Questions_QuestionnaireId");

            entity.Property(e => e.HelpText).HasMaxLength(500);
            entity.Property(e => e.Options).HasMaxLength(2000);
            entity.Property(e => e.QuestionText).HasMaxLength(500);
            entity.Property(e => e.Section).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ValidationRules).HasMaxLength(1000);

            entity.HasOne(d => d.Organization).WithMany(p => p.Questions)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.QuestionTypeMaster).WithMany(p => p.Questions).HasForeignKey(d => d.QuestionTypeMasterId);

            entity.HasOne(d => d.Questionnaire).WithMany(p => p.Questions).HasForeignKey(d => d.QuestionnaireId);
        });

        modelBuilder.Entity<QuestionAssignment>(entity =>
        {
            entity.HasIndex(e => e.AssignedUserId, "IX_QuestionAssignments_AssignedUserId");

            entity.HasIndex(e => new { e.CampaignAssignmentId, e.QuestionId }, "IX_QuestionAssignments_Assignment_Question");

            entity.HasIndex(e => new { e.CampaignAssignmentId, e.SectionName }, "IX_QuestionAssignments_Assignment_Section");

            entity.HasIndex(e => e.CampaignAssignmentId, "IX_QuestionAssignments_CampaignAssignmentId");

            entity.HasIndex(e => e.CreatedById, "IX_QuestionAssignments_CreatedById");

            entity.HasIndex(e => e.QuestionId, "IX_QuestionAssignments_QuestionId");

            entity.Property(e => e.Instructions).HasMaxLength(1000);
            entity.Property(e => e.SectionName).HasMaxLength(255);

            entity.HasOne(d => d.AssignedUser).WithMany(p => p.QuestionAssignmentAssignedUsers)
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.QuestionAssignments).HasForeignKey(d => d.CampaignAssignmentId);

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.QuestionAssignmentCreatedBies)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionAssignments).HasForeignKey(d => d.QuestionId);
        });

        modelBuilder.Entity<QuestionAssignmentChange>(entity =>
        {
            entity.HasIndex(e => e.CampaignAssignmentId, "IX_QuestionAssignmentChanges_CampaignAssignmentId");

            entity.HasIndex(e => e.ChangedById, "IX_QuestionAssignmentChanges_ChangedById");

            entity.HasIndex(e => e.NewAssignedUserId, "IX_QuestionAssignmentChanges_NewAssignedUserId");

            entity.HasIndex(e => e.OldAssignedUserId, "IX_QuestionAssignmentChanges_OldAssignedUserId");

            entity.HasIndex(e => e.QuestionId, "IX_QuestionAssignmentChanges_QuestionId");

            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.ChangeType).HasMaxLength(100);
            entity.Property(e => e.NewInstructions).HasMaxLength(1000);
            entity.Property(e => e.OldInstructions).HasMaxLength(1000);
            entity.Property(e => e.SectionName).HasMaxLength(255);

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.QuestionAssignmentChanges)
                .HasForeignKey(d => d.CampaignAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.QuestionAssignmentChangeChangedBies)
                .HasForeignKey(d => d.ChangedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.NewAssignedUser).WithMany(p => p.QuestionAssignmentChangeNewAssignedUsers).HasForeignKey(d => d.NewAssignedUserId);

            entity.HasOne(d => d.OldAssignedUser).WithMany(p => p.QuestionAssignmentChangeOldAssignedUsers).HasForeignKey(d => d.OldAssignedUserId);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionAssignmentChanges).HasForeignKey(d => d.QuestionId);
        });

        modelBuilder.Entity<QuestionAttribute>(entity =>
        {
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Subcategory).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);
        });

        modelBuilder.Entity<QuestionChange>(entity =>
        {
            entity.HasIndex(e => e.ChangedById, "IX_QuestionChanges_ChangedById");

            entity.HasIndex(e => e.QuestionId, "IX_QuestionChanges_QuestionId");

            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.FieldName).HasMaxLength(100);

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.QuestionChanges)
                .HasForeignKey(d => d.ChangedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionChanges)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<QuestionDependency>(entity =>
        {
            entity.HasIndex(e => e.CreatedById, "IX_QuestionDependencies_CreatedById");

            entity.HasIndex(e => e.DependsOnQuestionId, "IX_QuestionDependencies_DependsOnQuestionId");

            entity.HasIndex(e => e.QuestionId, "IX_QuestionDependencies_QuestionId");

            entity.Property(e => e.ConditionValue).HasMaxLength(500);

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.QuestionDependencies)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DependsOnQuestion).WithMany(p => p.QuestionDependencyDependsOnQuestions)
                .HasForeignKey(d => d.DependsOnQuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionDependencyQuestions).HasForeignKey(d => d.QuestionId);
        });

        modelBuilder.Entity<QuestionQuestionAttribute>(entity =>
        {
            entity.HasIndex(e => new { e.QuestionId, e.QuestionAttributeId }, "IX_QuestionQuestionAttribute_Question_Attribute_Unique").IsUnique();

            entity.HasIndex(e => e.QuestionAttributeId, "IX_QuestionQuestionAttributes_QuestionAttributeId");

            entity.HasOne(d => d.QuestionAttribute).WithMany(p => p.QuestionQuestionAttributes).HasForeignKey(d => d.QuestionAttributeId);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionQuestionAttributes).HasForeignKey(d => d.QuestionId);
        });

        modelBuilder.Entity<QuestionType>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.InputType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Questionnaire>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_Questionnaires_CreatedByUserId");

            entity.HasIndex(e => e.OrganizationId, "IX_Questionnaires_OrganizationId");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Questionnaires).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.Organization).WithMany(p => p.Questionnaires)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<QuestionnaireVersion>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_QuestionnaireVersions_CreatedByUserId");

            entity.HasIndex(e => new { e.QuestionnaireId, e.VersionNumber }, "IX_QuestionnaireVersions_Questionnaire_Version");

            entity.Property(e => e.ChangeDescription).HasMaxLength(500);
            entity.Property(e => e.VersionNumber).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.QuestionnaireVersions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Questionnaire).WithMany(p => p.QuestionnaireVersions).HasForeignKey(d => d.QuestionnaireId);
        });

        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasIndex(e => e.CampaignAssignmentId, "IX_Responses_CampaignAssignmentId");

            entity.HasIndex(e => new { e.QuestionId, e.CampaignAssignmentId }, "IX_Responses_Question_Assignment");

            entity.HasIndex(e => e.ResponderId, "IX_Responses_ResponderId");

            entity.HasIndex(e => e.SourceResponseId, "IX_Responses_SourceResponseId");

            entity.HasIndex(e => e.StatusUpdatedById, "IX_Responses_StatusUpdatedById");

            entity.Property(e => e.NumericValue).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.Responses)
                .HasForeignKey(d => d.CampaignAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Question).WithMany(p => p.Responses)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Responder).WithMany(p => p.ResponseResponders)
                .HasForeignKey(d => d.ResponderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SourceResponse).WithMany(p => p.InverseSourceResponse).HasForeignKey(d => d.SourceResponseId);

            entity.HasOne(d => d.StatusUpdatedBy).WithMany(p => p.ResponseStatusUpdatedBies).HasForeignKey(d => d.StatusUpdatedById);
        });

        modelBuilder.Entity<ResponseChange>(entity =>
        {
            entity.HasIndex(e => e.ChangedById, "IX_ResponseChanges_ChangedById");

            entity.HasIndex(e => e.ResponseId, "IX_ResponseChanges_ResponseId");

            entity.Property(e => e.ChangeReason).HasMaxLength(500);

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.ResponseChanges)
                .HasForeignKey(d => d.ChangedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseChanges)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ResponseOverride>(entity =>
        {
            entity.HasIndex(e => e.OriginalResponderId, "IX_ResponseOverrides_OriginalResponderId");

            entity.HasIndex(e => e.OverriddenById, "IX_ResponseOverrides_OverriddenById");

            entity.HasIndex(e => e.ResponseId, "IX_ResponseOverrides_ResponseId");

            entity.Property(e => e.OverrideReason).HasMaxLength(1000);

            entity.HasOne(d => d.OriginalResponder).WithMany(p => p.ResponseOverrideOriginalResponders)
                .HasForeignKey(d => d.OriginalResponderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OverriddenBy).WithMany(p => p.ResponseOverrideOverriddenBies)
                .HasForeignKey(d => d.OverriddenById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseOverrides).HasForeignKey(d => d.ResponseId);
        });

        modelBuilder.Entity<ResponseStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.ChangedById, "IX_ResponseStatusHistories_ChangedById");

            entity.HasIndex(e => e.ResponseId, "IX_ResponseStatusHistories_ResponseId");

            entity.Property(e => e.ChangeReason).HasMaxLength(1000);

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.ResponseStatusHistories).HasForeignKey(d => d.ChangedById);

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseStatusHistories).HasForeignKey(d => d.ResponseId);
        });

        modelBuilder.Entity<ResponseWorkflow>(entity =>
        {
            entity.HasIndex(e => e.ResponseId, "IX_ResponseWorkflow_Response_Unique").IsUnique();

            entity.HasIndex(e => e.CurrentReviewerId, "IX_ResponseWorkflows_CurrentReviewerId");

            entity.HasOne(d => d.CurrentReviewer).WithMany(p => p.ResponseWorkflows).HasForeignKey(d => d.CurrentReviewerId);

            entity.HasOne(d => d.Response).WithOne(p => p.ResponseWorkflow).HasForeignKey<ResponseWorkflow>(d => d.ResponseId);
        });

        modelBuilder.Entity<ReviewAssignment>(entity =>
        {
            entity.HasIndex(e => e.AssignedById, "IX_ReviewAssignments_AssignedById");

            entity.HasIndex(e => e.CampaignAssignmentId, "IX_ReviewAssignments_CampaignAssignmentId");

            entity.HasIndex(e => e.QuestionId, "IX_ReviewAssignments_QuestionId");

            entity.HasIndex(e => e.ReviewerId, "IX_ReviewAssignments_ReviewerId");

            entity.Property(e => e.Instructions).HasMaxLength(1000);
            entity.Property(e => e.SectionName).HasMaxLength(255);

            entity.HasOne(d => d.AssignedBy).WithMany(p => p.ReviewAssignmentAssignedBies)
                .HasForeignKey(d => d.AssignedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.ReviewAssignments).HasForeignKey(d => d.CampaignAssignmentId);

            entity.HasOne(d => d.Question).WithMany(p => p.ReviewAssignments).HasForeignKey(d => d.QuestionId);

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewAssignmentReviewers)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ReviewAuditLog>(entity =>
        {
            entity.HasIndex(e => e.CampaignAssignmentId, "IX_ReviewAuditLogs_CampaignAssignmentId");

            entity.HasIndex(e => e.QuestionId, "IX_ReviewAuditLogs_QuestionId");

            entity.HasIndex(e => e.ResponseId, "IX_ReviewAuditLogs_ResponseId");

            entity.HasIndex(e => e.ReviewAssignmentId, "IX_ReviewAuditLogs_ReviewAssignmentId");

            entity.HasIndex(e => e.UserId, "IX_ReviewAuditLogs_UserId");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.FromStatus).HasMaxLength(100);
            entity.Property(e => e.ToStatus).HasMaxLength(100);

            entity.HasOne(d => d.CampaignAssignment).WithMany(p => p.ReviewAuditLogs).HasForeignKey(d => d.CampaignAssignmentId);

            entity.HasOne(d => d.Question).WithMany(p => p.ReviewAuditLogs).HasForeignKey(d => d.QuestionId);

            entity.HasOne(d => d.Response).WithMany(p => p.ReviewAuditLogs).HasForeignKey(d => d.ResponseId);

            entity.HasOne(d => d.ReviewAssignment).WithMany(p => p.ReviewAuditLogs).HasForeignKey(d => d.ReviewAssignmentId);

            entity.HasOne(d => d.User).WithMany(p => p.ReviewAuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ReviewComment>(entity =>
        {
            entity.HasIndex(e => e.ResolvedById, "IX_ReviewComments_ResolvedById");

            entity.HasIndex(e => e.ResponseId, "IX_ReviewComments_ResponseId");

            entity.HasIndex(e => e.ReviewAssignmentId, "IX_ReviewComments_ReviewAssignmentId");

            entity.HasIndex(e => e.ReviewerId, "IX_ReviewComments_ReviewerId");

            entity.HasOne(d => d.ResolvedBy).WithMany(p => p.ReviewCommentResolvedBies).HasForeignKey(d => d.ResolvedById);

            entity.HasOne(d => d.Response).WithMany(p => p.ReviewComments)
                .HasForeignKey(d => d.ResponseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ReviewAssignment).WithMany(p => p.ReviewComments).HasForeignKey(d => d.ReviewAssignmentId);

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewCommentReviewers)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_RoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Symbol).HasMaxLength(10);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.OrganizationId, "IX_Users_OrganizationId");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserContext>(entity =>
        {
            entity.HasIndex(e => e.ActivePlatformOrganizationId, "IX_UserContexts_ActivePlatformOrganizationId");

            entity.HasIndex(e => e.OrganizationId, "IX_UserContexts_OrganizationId");

            entity.HasIndex(e => new { e.UserId, e.OrganizationId }, "IX_UserContexts_User_Organization_Unique").IsUnique();

            entity.HasOne(d => d.ActivePlatformOrganization).WithMany(p => p.UserContextActivePlatformOrganizations)
                .HasForeignKey(d => d.ActivePlatformOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Organization).WithMany(p => p.UserContextOrganizations)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UserContexts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_UserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
