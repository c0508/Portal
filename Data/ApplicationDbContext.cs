using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private int? _currentOrganizationId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Core entities
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationRelationship> OrganizationRelationships { get; set; }
    public DbSet<UserContext> UserContexts { get; set; }
    public DbSet<Questionnaire> Questionnaires { get; set; }
    public DbSet<QuestionnaireVersion> QuestionnaireVersions { get; set; }
    public DbSet<QuestionTypeMaster> QuestionTypes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionAttribute> QuestionAttributes { get; set; }
    public DbSet<QuestionQuestionAttribute> QuestionQuestionAttributes { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignAssignment> CampaignAssignments { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<ResponseChange> ResponseChanges { get; set; }
    public DbSet<QuestionChange> QuestionChanges { get; set; }
    public DbSet<Delegation> Delegations { get; set; }
    public DbSet<FileUpload> FileUploads { get; set; }
    public DbSet<QuestionAssignment> QuestionAssignments { get; set; }
    public DbSet<QuestionAssignmentChange> QuestionAssignmentChanges { get; set; }
    public DbSet<ResponseOverride> ResponseOverrides { get; set; }

    // Review system entities
    public DbSet<ReviewAssignment> ReviewAssignments { get; set; }
    public DbSet<ReviewComment> ReviewComments { get; set; }
    public DbSet<ReviewAuditLog> ReviewAuditLogs { get; set; }
    public DbSet<ResponseWorkflow> ResponseWorkflows { get; set; }

    // Organization Attribute Master Data
    public DbSet<OrganizationAttributeType> OrganizationAttributeTypes { get; set; }
    public DbSet<OrganizationAttributeValue> OrganizationAttributeValues { get; set; }
    public DbSet<OrganizationRelationshipAttribute> OrganizationRelationshipAttributes { get; set; }

    // Unit Master Data
    public DbSet<Unit> Units { get; set; }

    // Conditional Logic
    public DbSet<QuestionDependency> QuestionDependencies { get; set; }

    public void SetOrganizationContext(int organizationId)
    {
        _currentOrganizationId = organizationId;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // Configure relationships
        ConfigureRelationships(builder);

        // Configure organization-scoped query filters
        ConfigureQueryFilters(builder);

        // Configure indexes
        ConfigureIndexes(builder);
    }

    private void ConfigureRelationships(ModelBuilder builder)
    {
        // User - Organization (Many-to-One)
        builder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrganizationRelationship configurations
        builder.Entity<OrganizationRelationship>()
            .HasOne(or => or.PlatformOrganization)
            .WithMany(o => o.SupplierRelationships)
            .HasForeignKey(or => or.PlatformOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrganizationRelationship>()
            .HasOne(or => or.SupplierOrganization)
            .WithMany(o => o.PlatformRelationships)
            .HasForeignKey(or => or.SupplierOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrganizationRelationship>()
            .HasOne(or => or.CreatedByUser)
            .WithMany()
            .HasForeignKey(or => or.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrganizationRelationship>()
            .HasOne(or => or.CreatedByOrganization)
            .WithMany()
            .HasForeignKey(or => or.CreatedByOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Prevent self-referencing relationships
        builder.Entity<OrganizationRelationship>()
            .ToTable(t => t.HasCheckConstraint("CK_OrganizationRelationship_NoSelfReference", 
                "PlatformOrganizationId != SupplierOrganizationId"));

        // OrganizationRelationshipAttribute configurations
        builder.Entity<OrganizationRelationshipAttribute>()
            .HasOne(ora => ora.OrganizationRelationship)
            .WithMany(or => or.Attributes)
            .HasForeignKey(ora => ora.OrganizationRelationshipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrganizationRelationshipAttribute>()
            .HasOne(ora => ora.CreatedByUser)
            .WithMany()
            .HasForeignKey(ora => ora.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ensure unique attribute types per relationship
        builder.Entity<OrganizationRelationshipAttribute>()
            .HasIndex(ora => new { ora.OrganizationRelationshipId, ora.AttributeType })
            .IsUnique()
            .HasDatabaseName("IX_OrganizationRelationshipAttributes_Unique_Relationship_AttributeType");

        // UserContext configurations
        builder.Entity<UserContext>()
            .HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserContext>()
            .HasOne(uc => uc.Organization)
            .WithMany()
            .HasForeignKey(uc => uc.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserContext>()
            .HasOne(uc => uc.ActivePlatformOrganization)
            .WithMany()
            .HasForeignKey(uc => uc.ActivePlatformOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Questionnaire - Organization (Many-to-One)
        builder.Entity<Questionnaire>()
            .HasOne(q => q.Organization)
            .WithMany()
            .HasForeignKey(q => q.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Campaign - Organization (Many-to-One)
        builder.Entity<Campaign>()
            .HasOne(c => c.Organization)
            .WithMany(o => o.CampaignsCreated)
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // CampaignAssignment - Organizations (Many-to-One for both source and target)
        builder.Entity<CampaignAssignment>()
            .HasOne(ca => ca.TargetOrganization)
            .WithMany(o => o.CampaignAssignments)
            .HasForeignKey(ca => ca.TargetOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // CampaignAssignment - OrganizationRelationship (optional)
        builder.Entity<CampaignAssignment>()
            .HasOne(ca => ca.OrganizationRelationship)
            .WithMany(or => or.CampaignAssignments)
            .HasForeignKey(ca => ca.OrganizationRelationshipId)
            .OnDelete(DeleteBehavior.SetNull);

        // Response relationships (prevent cascade path conflicts)
        builder.Entity<Response>()
            .HasOne(r => r.Question)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Response>()
            .HasOne(r => r.CampaignAssignment)
            .WithMany(ca => ca.Responses)
            .HasForeignKey(r => r.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Response>()
            .HasOne(r => r.Responder)
            .WithMany(u => u.Responses)
            .HasForeignKey(r => r.ResponderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delegation relationships
        builder.Entity<Delegation>()
            .HasOne(d => d.FromUser)
            .WithMany(u => u.DelegationsFrom)
            .HasForeignKey(d => d.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Delegation>()
            .HasOne(d => d.ToUser)
            .WithMany(u => u.DelegationsTo)
            .HasForeignKey(d => d.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Question - Delegation relationship (to prevent cascade path conflicts)
        builder.Entity<Delegation>()
            .HasOne(d => d.Question)
            .WithMany(q => q.Delegations)
            .HasForeignKey(d => d.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // CampaignAssignment - Delegation relationship
        builder.Entity<Delegation>()
            .HasOne(d => d.CampaignAssignment)
            .WithMany()
            .HasForeignKey(d => d.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FileUpload relationships
        builder.Entity<FileUpload>()
            .HasOne(f => f.Response)
            .WithMany(r => r.FileUploads)
            .HasForeignKey(f => f.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FileUpload>()
            .HasOne(f => f.UploadedBy)
            .WithMany(u => u.FileUploads)
            .HasForeignKey(f => f.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ResponseChange relationships
        builder.Entity<ResponseChange>()
            .HasOne(rc => rc.Response)
            .WithMany(r => r.Changes)
            .HasForeignKey(rc => rc.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResponseChange>()
            .HasOne(rc => rc.ChangedBy)
            .WithMany()
            .HasForeignKey(rc => rc.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        // QuestionChange relationships
        builder.Entity<QuestionChange>()
            .HasOne(qc => qc.Question)
            .WithMany(q => q.Changes)
            .HasForeignKey(qc => qc.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // QuestionAssignmentChange relationships
        builder.Entity<QuestionAssignmentChange>()
            .HasOne(qac => qac.CampaignAssignment)
            .WithMany()
            .HasForeignKey(qac => qac.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignmentChange>()
            .HasOne(qac => qac.Question)
            .WithMany()
            .HasForeignKey(qac => qac.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignmentChange>()
            .HasOne(qac => qac.ChangedBy)
            .WithMany()
            .HasForeignKey(qac => qac.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignmentChange>()
            .HasOne(qac => qac.OldAssignedUser)
            .WithMany()
            .HasForeignKey(qac => qac.OldAssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignmentChange>()
            .HasOne(qac => qac.NewAssignedUser)
            .WithMany()
            .HasForeignKey(qac => qac.NewAssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionChange>()
            .HasOne(qc => qc.ChangedBy)
            .WithMany()
            .HasForeignKey(qc => qc.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        // QuestionnaireVersion relationships
        builder.Entity<QuestionnaireVersion>()
            .HasOne(qv => qv.Questionnaire)
            .WithMany(q => q.Versions)
            .HasForeignKey(qv => qv.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuestionnaireVersion>()
            .HasOne(qv => qv.CreatedByUser)
            .WithMany()
            .HasForeignKey(qv => qv.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Question relationships
        builder.Entity<Question>()
            .HasOne(q => q.Questionnaire)
            .WithMany(qu => qu.Questions)
            .HasForeignKey(q => q.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Question>()
            .HasOne(q => q.Organization)
            .WithMany()
            .HasForeignKey(q => q.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Question - QuestionTypeMaster relationship
        builder.Entity<Question>()
            .HasOne(q => q.QuestionTypeMaster)
            .WithMany(qt => qt.Questions)
            .HasForeignKey(q => q.QuestionTypeMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        // QuestionQuestionAttribute relationships (Many-to-Many)
        builder.Entity<QuestionQuestionAttribute>()
            .HasOne(qqa => qqa.Question)
            .WithMany(q => q.QuestionAttributes)
            .HasForeignKey(qqa => qqa.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuestionQuestionAttribute>()
            .HasOne(qqa => qqa.QuestionAttribute)
            .WithMany(qa => qa.QuestionAttributes)
            .HasForeignKey(qqa => qqa.QuestionAttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique question-attribute combinations
        builder.Entity<QuestionQuestionAttribute>()
            .HasIndex(qqa => new { qqa.QuestionId, qqa.QuestionAttributeId })
            .IsUnique()
            .HasDatabaseName("IX_QuestionQuestionAttribute_Question_Attribute_Unique");

        // Organization Attribute Master Data relationships
        builder.Entity<OrganizationAttributeValue>()
            .HasOne(oav => oav.AttributeType)
            .WithMany(oat => oat.Values)
            .HasForeignKey(oav => oav.AttributeTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique codes within attribute types
        builder.Entity<OrganizationAttributeValue>()
            .HasIndex(oav => new { oav.AttributeTypeId, oav.Code })
            .IsUnique()
            .HasDatabaseName("IX_OrganizationAttributeValue_Type_Code_Unique");

        builder.Entity<OrganizationAttributeType>()
            .HasIndex(oat => oat.Code)
            .IsUnique()
            .HasDatabaseName("IX_OrganizationAttributeType_Code_Unique");

        // QuestionAssignment relationships
        builder.Entity<QuestionAssignment>()
            .HasOne(qa => qa.CampaignAssignment)
            .WithMany(ca => ca.QuestionAssignments)
            .HasForeignKey(qa => qa.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuestionAssignment>()
            .HasOne(qa => qa.Question)
            .WithMany()
            .HasForeignKey(qa => qa.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignment>()
            .HasOne(qa => qa.AssignedUser)
            .WithMany()
            .HasForeignKey(qa => qa.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionAssignment>()
            .HasOne(qa => qa.CreatedBy)
            .WithMany()
            .HasForeignKey(qa => qa.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // QuestionDependency relationships
        builder.Entity<QuestionDependency>()
            .HasOne(qd => qd.Question)
            .WithMany(q => q.Dependencies)
            .HasForeignKey(qd => qd.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuestionDependency>()
            .HasOne(qd => qd.DependsOnQuestion)
            .WithMany(q => q.DependentQuestions)
            .HasForeignKey(qd => qd.DependsOnQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionDependency>()
            .HasOne(qd => qd.CreatedBy)
            .WithMany()
            .HasForeignKey(qd => qd.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Ensure either QuestionId OR SectionName is set, not both
        builder.Entity<QuestionAssignment>()
            .ToTable(t => t.HasCheckConstraint("CK_QuestionAssignment_QuestionOrSection", 
                "(QuestionId IS NOT NULL AND SectionName IS NULL) OR (QuestionId IS NULL AND SectionName IS NOT NULL)"));

        // ResponseOverride relationships
        builder.Entity<ResponseOverride>()
            .HasOne(ro => ro.Response)
            .WithMany(r => r.Overrides)
            .HasForeignKey(ro => ro.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResponseOverride>()
            .HasOne(ro => ro.OverriddenBy)
            .WithMany()
            .HasForeignKey(ro => ro.OverriddenById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResponseOverride>()
            .HasOne(ro => ro.OriginalResponder)
            .WithMany()
            .HasForeignKey(ro => ro.OriginalResponderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review system relationships
        builder.Entity<ReviewAssignment>()
            .HasOne(ra => ra.CampaignAssignment)
            .WithMany()
            .HasForeignKey(ra => ra.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReviewAssignment>()
            .HasOne(ra => ra.Reviewer)
            .WithMany()
            .HasForeignKey(ra => ra.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewAssignment>()
            .HasOne(ra => ra.Question)
            .WithMany()
            .HasForeignKey(ra => ra.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewAssignment>()
            .HasOne(ra => ra.AssignedBy)
            .WithMany()
            .HasForeignKey(ra => ra.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Ensure either QuestionId OR SectionName is set for question/section reviews
        builder.Entity<ReviewAssignment>()
            .ToTable(t => t.HasCheckConstraint("CK_ReviewAssignment_Scope", 
                "(Scope = 2 AND QuestionId IS NULL AND SectionName IS NULL) OR " + // Assignment review
                "(Scope = 0 AND QuestionId IS NOT NULL AND SectionName IS NULL) OR " + // Question review
                "(Scope = 1 AND QuestionId IS NULL AND SectionName IS NOT NULL)"));   // Section review

        // ReviewComment relationships
        builder.Entity<ReviewComment>()
            .HasOne(rc => rc.ReviewAssignment)
            .WithMany(ra => ra.Comments)
            .HasForeignKey(rc => rc.ReviewAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReviewComment>()
            .HasOne(rc => rc.Response)
            .WithMany()
            .HasForeignKey(rc => rc.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewComment>()
            .HasOne(rc => rc.Reviewer)
            .WithMany()
            .HasForeignKey(rc => rc.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewComment>()
            .HasOne(rc => rc.ResolvedBy)
            .WithMany()
            .HasForeignKey(rc => rc.ResolvedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ReviewAuditLog relationships
        builder.Entity<ReviewAuditLog>()
            .HasOne(ral => ral.CampaignAssignment)
            .WithMany()
            .HasForeignKey(ral => ral.CampaignAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReviewAuditLog>()
            .HasOne(ral => ral.Question)
            .WithMany()
            .HasForeignKey(ral => ral.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewAuditLog>()
            .HasOne(ral => ral.Response)
            .WithMany()
            .HasForeignKey(ral => ral.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewAuditLog>()
            .HasOne(ral => ral.ReviewAssignment)
            .WithMany()
            .HasForeignKey(ral => ral.ReviewAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewAuditLog>()
            .HasOne(ral => ral.User)
            .WithMany()
            .HasForeignKey(ral => ral.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ResponseWorkflow relationships
        builder.Entity<ResponseWorkflow>()
            .HasOne(rw => rw.Response)
            .WithMany()
            .HasForeignKey(rw => rw.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResponseWorkflow>()
            .HasOne(rw => rw.CurrentReviewer)
            .WithMany()
            .HasForeignKey(rw => rw.CurrentReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ensure unique workflow per response
        builder.Entity<ResponseWorkflow>()
            .HasIndex(rw => rw.ResponseId)
            .IsUnique()
            .HasDatabaseName("IX_ResponseWorkflow_Response_Unique");
    }

    private void ConfigureQueryFilters(ModelBuilder builder)
    {
        // Apply organization-scoped filters for multi-tenancy
        // Note: Platform admins can bypass these filters when needed

        builder.Entity<User>().HasQueryFilter(u => 
            _currentOrganizationId == null || u.OrganizationId == _currentOrganizationId);

        builder.Entity<Questionnaire>().HasQueryFilter(q => 
            _currentOrganizationId == null || q.OrganizationId == _currentOrganizationId);

        builder.Entity<Campaign>().HasQueryFilter(c => 
            _currentOrganizationId == null || c.OrganizationId == _currentOrganizationId);

        // For responses, filter by the responder's organization
        builder.Entity<Response>().HasQueryFilter(r => 
            _currentOrganizationId == null || r.Responder.OrganizationId == _currentOrganizationId);

        // For campaign assignments, filter by target organization
        builder.Entity<CampaignAssignment>().HasQueryFilter(ca => 
            _currentOrganizationId == null || ca.TargetOrganizationId == _currentOrganizationId);

        // OrganizationRelationships - show relationships where current org is involved
        builder.Entity<OrganizationRelationship>().HasQueryFilter(or => 
            _currentOrganizationId == null || 
            or.PlatformOrganizationId == _currentOrganizationId || 
            or.SupplierOrganizationId == _currentOrganizationId);

        // Add matching query filters for child entities to prevent warnings

        // Questions - filter by questionnaire's organization
        builder.Entity<Question>().HasQueryFilter(q => 
            _currentOrganizationId == null || q.Questionnaire.OrganizationId == _currentOrganizationId);

        // QuestionnaireVersions - filter by questionnaire's organization
        builder.Entity<QuestionnaireVersion>().HasQueryFilter(qv => 
            _currentOrganizationId == null || qv.Questionnaire.OrganizationId == _currentOrganizationId);

        // Delegations - filter by campaign assignment's target organization
        builder.Entity<Delegation>().HasQueryFilter(d => 
            _currentOrganizationId == null || d.CampaignAssignment.TargetOrganizationId == _currentOrganizationId);

        // FileUploads - filter by response's responder organization
        builder.Entity<FileUpload>().HasQueryFilter(f => 
            _currentOrganizationId == null || f.Response.Responder.OrganizationId == _currentOrganizationId);

        // ResponseChanges - filter by response's responder organization
        builder.Entity<ResponseChange>().HasQueryFilter(rc => 
            _currentOrganizationId == null || rc.Response.Responder.OrganizationId == _currentOrganizationId);

        // UserContexts - filter by user's organization
        builder.Entity<UserContext>().HasQueryFilter(uc => 
            _currentOrganizationId == null || uc.User.OrganizationId == _currentOrganizationId);

        // QuestionAssignments - filter by campaign assignment's target organization
        builder.Entity<QuestionAssignment>().HasQueryFilter(qa => 
            _currentOrganizationId == null || qa.CampaignAssignment.TargetOrganizationId == _currentOrganizationId);

        // ResponseOverrides - filter by response's responder organization
        builder.Entity<ResponseOverride>().HasQueryFilter(ro => 
            _currentOrganizationId == null || ro.Response.Responder.OrganizationId == _currentOrganizationId);

        // Review system query filters
        builder.Entity<ReviewAssignment>().HasQueryFilter(ra => 
            _currentOrganizationId == null || ra.CampaignAssignment.TargetOrganizationId == _currentOrganizationId);

        builder.Entity<ReviewComment>().HasQueryFilter(rc => 
            _currentOrganizationId == null || rc.Response.Responder.OrganizationId == _currentOrganizationId);

        builder.Entity<ReviewAuditLog>().HasQueryFilter(ral => 
            _currentOrganizationId == null || ral.CampaignAssignment.TargetOrganizationId == _currentOrganizationId);

        builder.Entity<ResponseWorkflow>().HasQueryFilter(rw => 
            _currentOrganizationId == null || rw.Response.Responder.OrganizationId == _currentOrganizationId);

        // Configure decimal precision for NumericValue to prevent truncation warnings
        builder.Entity<Response>()
            .Property(r => r.NumericValue)
            .HasPrecision(18, 6); // 18 total digits, 6 decimal places
    }

    private void ConfigureIndexes(ModelBuilder builder)
    {
        // Performance indexes for multi-tenant queries
        builder.Entity<User>()
            .HasIndex(u => u.OrganizationId)
            .HasDatabaseName("IX_Users_OrganizationId");

        builder.Entity<Questionnaire>()
            .HasIndex(q => q.OrganizationId)
            .HasDatabaseName("IX_Questionnaires_OrganizationId");

        builder.Entity<Campaign>()
            .HasIndex(c => c.OrganizationId)
            .HasDatabaseName("IX_Campaigns_OrganizationId");

        builder.Entity<CampaignAssignment>()
            .HasIndex(ca => ca.TargetOrganizationId)
            .HasDatabaseName("IX_CampaignAssignments_TargetOrganizationId");

        // Organization relationship indexes
        builder.Entity<OrganizationRelationship>()
            .HasIndex(or => or.PlatformOrganizationId)
            .HasDatabaseName("IX_OrganizationRelationships_PlatformOrganizationId");

        builder.Entity<OrganizationRelationship>()
            .HasIndex(or => or.SupplierOrganizationId)
            .HasDatabaseName("IX_OrganizationRelationships_SupplierOrganizationId");

        builder.Entity<OrganizationRelationship>()
            .HasIndex(or => new { or.PlatformOrganizationId, or.SupplierOrganizationId })
            .IsUnique()
            .HasDatabaseName("IX_OrganizationRelationships_Platform_Supplier_Unique");

        // UserContext indexes
        builder.Entity<UserContext>()
            .HasIndex(uc => new { uc.UserId, uc.OrganizationId })
            .IsUnique()
            .HasDatabaseName("IX_UserContexts_User_Organization_Unique");

        // Composite indexes for common queries
        builder.Entity<Response>()
            .HasIndex(r => new { r.QuestionId, r.CampaignAssignmentId })
            .HasDatabaseName("IX_Responses_Question_Assignment");

        builder.Entity<QuestionnaireVersion>()
            .HasIndex(qv => new { qv.QuestionnaireId, qv.VersionNumber })
            .HasDatabaseName("IX_QuestionnaireVersions_Questionnaire_Version");

        // QuestionAssignment indexes
        builder.Entity<QuestionAssignment>()
            .HasIndex(qa => qa.CampaignAssignmentId)
            .HasDatabaseName("IX_QuestionAssignments_CampaignAssignmentId");

        builder.Entity<QuestionAssignment>()
            .HasIndex(qa => qa.AssignedUserId)
            .HasDatabaseName("IX_QuestionAssignments_AssignedUserId");

        builder.Entity<QuestionAssignment>()
            .HasIndex(qa => new { qa.CampaignAssignmentId, qa.QuestionId })
            .HasDatabaseName("IX_QuestionAssignments_Assignment_Question");

        builder.Entity<QuestionAssignment>()
            .HasIndex(qa => new { qa.CampaignAssignmentId, qa.SectionName })
            .HasDatabaseName("IX_QuestionAssignments_Assignment_Section");

        // ResponseOverride indexes
        builder.Entity<ResponseOverride>()
            .HasIndex(ro => ro.ResponseId)
            .HasDatabaseName("IX_ResponseOverrides_ResponseId");

        builder.Entity<ResponseOverride>()
            .HasIndex(ro => ro.OverriddenById)
            .HasDatabaseName("IX_ResponseOverrides_OverriddenById");
    }
} 