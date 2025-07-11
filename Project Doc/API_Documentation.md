# ESG Platform API Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Authentication & Authorization](#authentication--authorization)
4. [Controllers](#controllers)
5. [Services](#services)
6. [Models](#models)
7. [Middleware](#middleware)
8. [Extensions](#extensions)
9. [Database Context](#database-context)
10. [Usage Examples](#usage-examples)

## Overview

The ESG Platform is a comprehensive Environmental, Social, and Governance (ESG) data collection and management system built with ASP.NET Core. It provides a multi-tenant platform for organizations to create campaigns, distribute questionnaires, collect responses, and manage ESG compliance data.

### Key Features

- **Multi-tenant Architecture**: Supports platform organizations and supplier organizations
- **Role-based Access Control**: PlatformAdmin, OrgAdmin, CampaignManager, LeadResponder, Responder, Reviewer
- **Campaign Management**: Create, manage, and track ESG data collection campaigns
- **Questionnaire System**: Dynamic questionnaire creation with versioning
- **Response Management**: Collect, review, and approve responses
- **Branding System**: Customizable branding per organization
- **Analytics**: Comprehensive reporting and analytics capabilities

## Architecture

### Technology Stack

- **Framework**: ASP.NET Core 7.0+
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **UI**: MVC with Razor Views
- **Logging**: Built-in .NET logging

### Project Structure

```
ESGPlatform/
├── Controllers/          # API endpoints and MVC controllers
├── Services/            # Business logic layer
├── Models/              # Data models and view models
│   ├── Entities/        # Database entities
│   └── ViewModels/      # Data transfer objects
├── Data/                # Database context and configuration
├── Middleware/          # Custom middleware components
├── Extensions/          # Utility extensions
├── Views/               # Razor views
└── wwwroot/            # Static assets
```

## Authentication & Authorization

### User Roles

The platform implements a hierarchical role-based access control system:

1. **PlatformAdmin**: Full system access, can manage all organizations
2. **OrgAdmin**: Organization-level administration
3. **CampaignManager**: Campaign creation and management
4. **LeadResponder**: Lead response coordination
5. **Responder**: Individual question response
6. **Reviewer**: Response review and approval

### Authorization Policies

```csharp
// Configured in Program.cs
options.AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin"));
options.AddPolicy("OrgAdmin", policy => policy.RequireRole("OrgAdmin"));
options.AddPolicy("OrgAdminOrHigher", policy => policy.RequireRole("PlatformAdmin", "OrgAdmin"));
options.AddPolicy("CampaignManager", policy => policy.RequireRole("CampaignManager"));
options.AddPolicy("CampaignManagerOrHigher", policy => policy.RequireRole("PlatformAdmin", "OrgAdmin", "CampaignManager"));
options.AddPolicy("LeadResponder", policy => policy.RequireRole("PlatformAdmin", "LeadResponder"));
options.AddPolicy("Responder", policy => policy.RequireRole("PlatformAdmin", "LeadResponder", "Responder"));
options.AddPolicy("Reviewer", policy => policy.RequireRole("PlatformAdmin", "Reviewer"));
```

## Controllers

### BaseController

**Location**: `Controllers/BaseController.cs`

Base controller providing common functionality for all controllers.

#### Properties

```csharp
protected int? CurrentOrganizationId { get; }
protected string? CurrentOrganizationName { get; }
protected OrganizationType? CurrentOrganizationType { get; }
protected bool IsCurrentOrgPlatformType { get; }
protected bool IsCurrentOrgSupplierType { get; }
protected string? CurrentUserId { get; }
protected bool IsPlatformAdmin { get; }
protected bool IsOrgAdmin { get; }
protected bool IsCampaignManager { get; }
protected bool IsLeadResponder { get; }
protected bool IsResponder { get; }
protected bool IsReviewer { get; }
```

#### Methods

```csharp
// Get branding context for current user
protected async Task<BrandingContext> GetBrandingContextAsync(int? campaignId = null, int? organizationId = null);

// Get campaign-specific branding
protected async Task<BrandingContext> GetCampaignBrandingContextAsync(int campaignId);

// Get fallback branding context
protected BrandingContext GetFallbackBrandingContext();

// Set branding context for views
protected async Task SetBrandingContextAsync(int? campaignId = null, int? organizationId = null);
```

### HomeController

**Location**: `Controllers/HomeController.cs`

Main dashboard controller providing user-specific views and data.

#### Endpoints

```csharp
// GET: /
[Authorize]
public async Task<IActionResult> Index()

// GET: /Home/Privacy
public IActionResult Privacy()

// GET: /Home/Error
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult Error()
```

#### Dashboard Features

- **User-specific assignments**: Shows campaigns and questionnaires assigned to the current user
- **Review assignments**: Displays review tasks for reviewers
- **Campaign overview**: Platform admins and campaign managers see campaign summaries
- **Progress tracking**: Real-time progress indicators and deadlines

### CampaignController

**Location**: `Controllers/CampaignController.cs`

Manages ESG data collection campaigns and assignments.

#### Endpoints

```csharp
// Campaign Management
GET: /Campaign/Dashboard          // Campaign dashboard with analytics
GET: /Campaign                    // List all campaigns
GET: /Campaign/Details/{id}       // Campaign details
GET: /Campaign/Create             // Create new campaign form
POST: /Campaign/Create            // Create new campaign
GET: /Campaign/Edit/{id}          // Edit campaign form
POST: /Campaign/Edit/{id}         // Update campaign
GET: /Campaign/Delete/{id}        // Delete confirmation
POST: /Campaign/Delete/{id}       // Delete campaign
POST: /Campaign/CloseCampaign/{id} // Close campaign

// Assignment Management
GET: /Campaign/ManageAssignments/{id}     // Manage campaign assignments
GET: /Campaign/CreateAssignment/{campaignId} // Create assignment form
POST: /Campaign/CreateAssignment          // Create assignment
GET: /Campaign/BulkAssignment/{campaignId} // Bulk assignment form
POST: /Campaign/BulkAssignment            // Bulk create assignments
GET: /Campaign/ViewAssignment/{id}        // View assignment details
GET: /Campaign/EditAssignment/{id}        // Edit assignment form
POST: /Campaign/EditAssignment/{id}       // Update assignment
POST: /Campaign/DeleteAssignment/{id}     // Delete assignment

// AJAX Endpoints
GET: /Campaign/GetUsersByOrganization/{organizationId} // Get users for organization
```

#### Key Features

- **Multi-organization support**: Platform admins can manage all organizations
- **Bulk operations**: Create multiple assignments simultaneously
- **Progress tracking**: Real-time assignment status and completion rates
- **Analytics**: Comprehensive dashboard with performance metrics

### ResponseController

**Location**: `Controllers/ResponseController.cs`

Handles questionnaire responses and data collection.

#### Endpoints

```csharp
// Response Management
GET: /Response/Index              // List user's responses
GET: /Response/Details/{id}       // Response details
GET: /Response/Create/{assignmentId} // Create response form
POST: /Response/Create            // Submit response
GET: /Response/Edit/{id}          // Edit response form
POST: /Response/Edit/{id}         // Update response
GET: /Response/Delete/{id}        // Delete confirmation
POST: /Response/Delete/{id}       // Delete response

// Response Workflow
POST: /Response/Submit/{id}       // Submit response for review
POST: /Response/Approve/{id}      // Approve response
POST: /Response/RequestChanges/{id} // Request changes
POST: /Response/Override/{id}     // Override response

// Delegation
GET: /Response/Delegate/{id}      // Delegate response form
POST: /Response/Delegate/{id}     // Delegate response
POST: /Response/RemoveDelegation/{id} // Remove delegation

// File Upload
POST: /Response/UploadFile        // Upload file attachment
GET: /Response/DownloadFile/{id}  // Download file
```

### QuestionnaireController

**Location**: `Controllers/QuestionnaireController.cs`

Manages questionnaire creation, editing, and versioning.

#### Endpoints

```csharp
// Questionnaire Management
GET: /Questionnaire               // List questionnaires
GET: /Questionnaire/Details/{id}  // Questionnaire details
GET: /Questionnaire/Create        // Create questionnaire form
POST: /Questionnaire/Create       // Create questionnaire
GET: /Questionnaire/Edit/{id}     // Edit questionnaire form
POST: /Questionnaire/Edit/{id}    // Update questionnaire
GET: /Questionnaire/Delete/{id}   // Delete confirmation
POST: /Questionnaire/Delete/{id}  // Delete questionnaire

// Question Management
GET: /Questionnaire/AddQuestion/{questionnaireId} // Add question form
POST: /Questionnaire/AddQuestion  // Add question
GET: /Questionnaire/EditQuestion/{id} // Edit question form
POST: /Questionnaire/EditQuestion/{id} // Update question
POST: /Questionnaire/DeleteQuestion/{id} // Delete question

// Version Management
GET: /Questionnaire/CreateVersion/{id} // Create new version
POST: /Questionnaire/CreateVersion // Create version
GET: /Questionnaire/ManageVersions/{id} // Manage versions

// Import/Export
GET: /Questionnaire/Export/{id}   // Export questionnaire
POST: /Questionnaire/Import       // Import questionnaire
```

### ReviewController

**Location**: `Controllers/ReviewController.cs`

Handles response review and approval workflow.

#### Endpoints

```csharp
// Review Management
GET: /Review                      // List review assignments
GET: /Review/Details/{id}         // Review details
GET: /Review/Start/{id}           // Start review
POST: /Review/Start/{id}          // Start review
GET: /Review/Complete/{id}        // Complete review form
POST: /Review/Complete/{id}       // Complete review

// Review Comments
POST: /Review/AddComment/{id}     // Add review comment
POST: /Review/EditComment/{id}    // Edit comment
POST: /Review/DeleteComment/{id}  // Delete comment

// Review Assignment
GET: /Review/Assign/{id}          // Assign review form
POST: /Review/Assign/{id}         // Assign review
POST: /Review/Unassign/{id}       // Unassign review
```

### ESGAnalyticsController

**Location**: `Controllers/ESGAnalyticsController.cs`

Provides analytics and reporting capabilities.

#### Endpoints

```csharp
// Analytics Dashboard
GET: /ESGAnalytics/Dashboard      // Analytics dashboard
GET: /ESGAnalytics/Campaign/{id}  // Campaign-specific analytics

// Data Export
GET: /ESGAnalytics/Export/{id}    // Export analytics data
GET: /ESGAnalytics/Report/{id}    // Generate report

// Charts and Visualizations
GET: /ESGAnalytics/Chart/{type}   // Get chart data
GET: /ESGAnalytics/Metrics/{id}   // Get performance metrics
```

## Services

### IBrandingService

**Location**: `Services/BrandingService.cs`

Manages organization-specific branding and theming.

#### Interface

```csharp
public interface IBrandingService
{
    Task<BrandingContext> GetBrandingContextAsync(string userId, int? campaignId = null, int? organizationId = null);
    Task<BrandingContext> GetDefaultBrandingAsync(int organizationId);
    Task<BrandingContext> GetCampaignBrandingAsync(int campaignId);
    Task<BrandingContext> GetPrimaryRelationshipBrandingAsync(int supplierOrganizationId);
}
```

#### Methods

```csharp
// Get branding context for user
public async Task<BrandingContext> GetBrandingContextAsync(string userId, int? campaignId = null, int? organizationId = null)

// Get default organization branding
public async Task<BrandingContext> GetDefaultBrandingAsync(int organizationId)

// Get campaign-specific branding
public async Task<BrandingContext> GetCampaignBrandingAsync(int campaignId)

// Get primary relationship branding for suppliers
public async Task<BrandingContext> GetPrimaryRelationshipBrandingAsync(int supplierOrganizationId)
```

#### BrandingContext Properties

```csharp
public class BrandingContext
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string NavigationTextColor { get; set; }
    public string Theme { get; set; }
    public BrandingSource BrandingSource { get; set; }
    public int SourceId { get; set; }
    public string? CampaignName { get; set; }
    public string? RelationshipDescription { get; set; }
}
```

### IExcelImportService

**Location**: `Services/ExcelImportService.cs`

Handles Excel file import for questionnaire creation.

#### Interface

```csharp
public interface IExcelImportService
{
    Task<List<ExcelQuestionPreviewViewModel>> ParseExcelFileAsync(IFormFile file);
    byte[] GenerateQuestionnaireTemplate();
}
```

#### Methods

```csharp
// Parse Excel file and extract questions
public async Task<List<ExcelQuestionPreviewViewModel>> ParseExcelFileAsync(IFormFile file)

// Generate Excel template for questionnaire creation
public byte[] GenerateQuestionnaireTemplate()
```

### QuestionAssignmentService

**Location**: `Services/QuestionAssignmentService.cs`

Manages question assignments and delegation.

#### Key Methods

```csharp
// Assign questions to users
public async Task<bool> AssignQuestionsAsync(int assignmentId, List<QuestionAssignmentViewModel> assignments)

// Get user's assigned questions
public async Task<List<QuestionAssignmentViewModel>> GetUserAssignmentsAsync(string userId)

// Delegate questions to other users
public async Task<bool> DelegateQuestionsAsync(int assignmentId, string fromUserId, string toUserId, List<int> questionIds)

// Track assignment changes
public async Task LogAssignmentChangeAsync(QuestionAssignmentChange change)
```

### ReviewService

**Location**: `Services/ReviewService.cs`

Handles response review workflow.

#### Key Methods

```csharp
// Create review assignment
public async Task<ReviewAssignment> CreateReviewAssignmentAsync(ReviewAssignmentCreateViewModel model)

// Start review process
public async Task<bool> StartReviewAsync(int reviewAssignmentId, string reviewerId)

// Complete review
public async Task<bool> CompleteReviewAsync(int reviewAssignmentId, ReviewCompletionViewModel model)

// Get review statistics
public async Task<ReviewStatisticsViewModel> GetReviewStatisticsAsync(string userId)
```

### ESGAnalyticsService

**Location**: `Services/ESGAnalyticsService.cs`

Provides analytics and reporting capabilities.

#### Key Methods

```csharp
// Get campaign analytics
public async Task<CampaignAnalyticsViewModel> GetCampaignAnalyticsAsync(int campaignId)

// Get response rate analytics
public async Task<ResponseRateAnalyticsViewModel> GetResponseRateAnalyticsAsync(int campaignId)

// Generate compliance report
public async Task<ComplianceReportViewModel> GenerateComplianceReportAsync(int campaignId)

// Get ESG metrics
public async Task<ESGMetricsViewModel> GetESGMetricsAsync(int organizationId)
```

## Models

### Entities

#### Organization

**Location**: `Models/Entities/Organization.cs`

Core organization entity supporting multi-tenant architecture.

```csharp
public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public OrganizationType Type { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? NavigationTextColor { get; set; }
    public string? Theme { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### Campaign

**Location**: `Models/Entities/Campaign.cs`

Represents ESG data collection campaigns.

```csharp
public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int OrganizationId { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? ReportingPeriodStart { get; set; }
    public DateTime? ReportingPeriodEnd { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
}
```

#### CampaignAssignment

**Location**: `Models/Entities/Campaign.cs`

Represents assignments of campaigns to target organizations.

```csharp
public class CampaignAssignment
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public int TargetOrganizationId { get; set; }
    public int QuestionnaireVersionId { get; set; }
    public int? OrganizationRelationshipId { get; set; }
    public string? LeadResponderId { get; set; }
    public AssignmentStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### Question

**Location**: `Models/Entities/Question.cs`

Represents individual questions in questionnaires.

```csharp
public class Question
{
    public int Id { get; set; }
    public int QuestionnaireId { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public string? Section { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ValidationRules { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### Response

**Location**: `Models/Entities/Response.cs`

Represents user responses to questions.

```csharp
public class Response
{
    public int Id { get; set; }
    public int CampaignAssignmentId { get; set; }
    public int QuestionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? SelectedValues { get; set; }
    public string? FilePath { get; set; }
    public ResponseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
}
```

### ViewModels

#### CampaignViewModels

**Location**: `Models/ViewModels/CampaignViewModels.cs`

Data transfer objects for campaign management.

```csharp
// Campaign creation
public class CampaignCreateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? ReportingPeriodStart { get; set; }
    public DateTime? ReportingPeriodEnd { get; set; }
    public string? Instructions { get; set; }
    public List<QuestionnaireSelectionViewModel> AvailableQuestionnaires { get; set; }
    public List<OrganizationSelectionViewModel> AvailableOrganizations { get; set; }
}

// Campaign dashboard
public class CampaignDashboardViewModel
{
    public CampaignDashboardSummaryViewModel Summary { get; set; }
    public List<CampaignDashboardItemViewModel> ActiveCampaigns { get; set; }
    public List<CampaignDashboardItemViewModel> RecentCampaigns { get; set; }
    public CampaignProgressMetricsViewModel ProgressMetrics { get; set; }
    public List<CampaignPerformanceViewModel> CampaignPerformance { get; set; }
    public List<CompanyAssignmentStatusViewModel> CompanyBreakdown { get; set; }
    public List<ResponderWorkloadViewModel> ResponderBreakdown { get; set; }
    public AssignmentStatusDistributionViewModel StatusDistribution { get; set; }
}
```

#### ResponseViewModels

**Location**: `Models/ViewModels/ResponseViewModels.cs`

Data transfer objects for response management.

```csharp
// Response creation
public class ResponseCreateViewModel
{
    public int CampaignAssignmentId { get; set; }
    public int QuestionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public List<string> SelectedValues { get; set; }
    public IFormFile? FileUpload { get; set; }
}

// Response review
public class ResponseReviewViewModel
{
    public int ResponseId { get; set; }
    public string QuestionText { get; set; }
    public string ResponseValue { get; set; }
    public ResponseStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
    public List<ReviewCommentViewModel> Comments { get; set; }
}
```

## Middleware

### OrganizationContextMiddleware

**Location**: `Middleware/OrganizationContextMiddleware.cs`

Sets organization context for multi-tenant data filtering.

#### Features

- **Automatic context setting**: Sets current organization context for authenticated users
- **Platform admin bypass**: Platform admins can see all data without organization filtering
- **HttpContext integration**: Stores organization context in HttpContext.Items for controller access

#### Usage

```csharp
// Configure in Program.cs
app.UseOrganizationContext();
```

#### Methods

```csharp
// Main middleware method
public async Task InvokeAsync(HttpContext context)

// Set organization context for user
private async Task SetOrganizationContextAsync(HttpContext context)
```

## Extensions

### HttpContextExtensions

**Location**: `Extensions/HttpContextExtensions.cs`

Utility extensions for HttpContext access.

#### Methods

```csharp
// Get current user's organization ID
public static int GetCurrentUserOrganizationId(this HttpContext httpContext)

// Try to get organization ID (returns null if not found)
public static int? TryGetCurrentUserOrganizationId(this HttpContext httpContext)

// Get current user ID
public static string GetCurrentUserId(this HttpContext httpContext)

// Check if user is in specific role
public static bool IsUserInRole(this HttpContext httpContext, string role)

// Check if user is platform admin
public static bool IsCurrentUserPlatformAdmin(this HttpContext httpContext)

// Get current user's email
public static string? GetCurrentUserEmail(this HttpContext httpContext)

// Get current user's name
public static string? GetCurrentUserName(this HttpContext httpContext)

// Get current user's organization name
public static string? GetCurrentUserOrganizationName(this HttpContext httpContext)

// Get current user's organization type
public static OrganizationType? GetCurrentUserOrganizationType(this HttpContext httpContext)
```

## Database Context

### ApplicationDbContext

**Location**: `Data/ApplicationDbContext.cs`

Main database context with Entity Framework Core configuration.

#### DbSets

```csharp
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
public DbSet<ResponseStatusHistory> ResponseStatusHistories { get; set; }
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
```

#### Key Methods

```csharp
// Set organization context for data filtering
public void SetOrganizationContext(int organizationId)

// Configure entity relationships
private void ConfigureRelationships(ModelBuilder builder)

// Configure query filters for multi-tenancy
private void ConfigureQueryFilters(ModelBuilder builder)

// Configure database indexes
private void ConfigureIndexes(ModelBuilder builder)
```

## Usage Examples

### Creating a Campaign

```csharp
// 1. Create campaign
var campaign = new Campaign
{
    Name = "ESG Compliance 2024",
    Description = "Annual ESG compliance data collection",
    OrganizationId = currentOrgId,
    Status = CampaignStatus.Draft,
    StartDate = DateTime.Now,
    Deadline = DateTime.Now.AddMonths(3),
    ReportingPeriodStart = new DateTime(2024, 1, 1),
    ReportingPeriodEnd = new DateTime(2024, 12, 31),
    Instructions = "Please complete all required fields"
};

// 2. Create assignment
var assignment = new CampaignAssignment
{
    CampaignId = campaign.Id,
    TargetOrganizationId = supplierOrgId,
    QuestionnaireVersionId = questionnaireVersionId,
    LeadResponderId = leadResponderId,
    Status = AssignmentStatus.NotStarted
};

// 3. Save to database
_context.Campaigns.Add(campaign);
_context.CampaignAssignments.Add(assignment);
await _context.SaveChangesAsync();
```

### Managing Responses

```csharp
// 1. Create response
var response = new Response
{
    CampaignAssignmentId = assignmentId,
    QuestionId = questionId,
    TextValue = "Our company has implemented comprehensive ESG policies",
    Status = ResponseStatus.Draft,
    CreatedById = currentUserId
};

// 2. Submit for review
response.Status = ResponseStatus.Submitted;
response.UpdatedAt = DateTime.UtcNow;

// 3. Review and approve
var review = new ReviewAssignment
{
    CampaignAssignmentId = assignmentId,
    QuestionId = questionId,
    ReviewerId = reviewerId,
    Scope = ReviewScope.Question,
    Status = ReviewStatus.InReview
};

// 4. Complete review
review.Status = ReviewStatus.Completed;
review.Comments.Add(new ReviewComment
{
    Comment = "Response approved",
    CreatedById = reviewerId
});
```

### Branding Integration

```csharp
// 1. Get branding context
var brandingService = HttpContext.RequestServices.GetService<IBrandingService>();
var brandingContext = await brandingService.GetBrandingContextAsync(userId, campaignId);

// 2. Apply branding to view
ViewBag.BrandingContext = brandingContext;
ViewBag.BrandingCssVariables = brandingContext.ToCssVariables();

// 3. Use in view
<div style="@brandingContext.GetBrandedCardHeaderStyle()">
    <h2>@brandingContext.OrganizationName</h2>
</div>
```

### Analytics Usage

```csharp
// 1. Get campaign analytics
var analyticsService = HttpContext.RequestServices.GetService<IESGAnalyticsService>();
var analytics = await analyticsService.GetCampaignAnalyticsAsync(campaignId);

// 2. Display metrics
ViewBag.CompletionRate = analytics.CompletionRate;
ViewBag.ResponseRate = analytics.ResponseRate;
ViewBag.AverageResponseTime = analytics.AverageResponseTimeHours;
```

### Multi-tenant Data Access

```csharp
// 1. Set organization context (done by middleware)
_context.SetOrganizationContext(currentOrganizationId);

// 2. Query data (automatically filtered by organization)
var campaigns = await _context.Campaigns
    .Include(c => c.Assignments)
    .ToListAsync(); // Only returns campaigns for current organization

// 3. Platform admin can see all data
if (IsPlatformAdmin)
{
    campaigns = await _context.Campaigns
        .IgnoreQueryFilters() // Bypass organization filtering
        .Include(c => c.Assignments)
        .ToListAsync();
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ESGPlatform;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Program.cs Configuration

```csharp
// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Service registration
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<IQuestionTypeService, QuestionTypeService>();
builder.Services.AddScoped<QuestionAssignmentService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IESGAnalyticsService, ESGAnalyticsService>();
```

## Security Considerations

1. **Authentication**: ASP.NET Core Identity with secure password policies
2. **Authorization**: Role-based access control with hierarchical permissions
3. **Data Isolation**: Multi-tenant architecture with organization-level data filtering
4. **Input Validation**: Model validation and anti-forgery tokens
5. **SQL Injection Protection**: Entity Framework Core with parameterized queries
6. **XSS Protection**: Razor views with automatic HTML encoding
7. **CSRF Protection**: Anti-forgery tokens on all POST requests

## Performance Considerations

1. **Database Indexing**: Optimized indexes for common queries
2. **Lazy Loading**: Entity Framework navigation properties loaded on demand
3. **Caching**: Response caching for static content
4. **Async Operations**: All database operations are async
5. **Query Optimization**: Efficient LINQ queries with proper includes
6. **Pagination**: Large result sets are paginated

## Deployment

### Prerequisites

- .NET 7.0 or later
- SQL Server 2019 or later
- IIS or Kestrel web server

### Deployment Steps

1. **Database Setup**
   ```bash
   dotnet ef database update
   ```

2. **Application Deployment**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **Configuration**
   - Update connection strings
   - Configure logging
   - Set up SSL certificates

4. **Monitoring**
   - Application Insights integration
   - Health checks
   - Performance monitoring

This comprehensive documentation covers all public APIs, functions, and components of the ESG Platform. The system provides a robust, scalable solution for ESG data collection and management with strong security, multi-tenant support, and comprehensive analytics capabilities.