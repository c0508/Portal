using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Middleware;
using ESGPlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotNetEnv.Env.Load();

// Add Kestrel configuration for both HTTP and HTTPS (localhost only)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // HTTP
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD") ?? 
                  builder.Configuration["ConnectionStrings:DefaultConnection:Password"];
if (!string.IsNullOrEmpty(sqlPassword))
{
    connectionString = connectionString.Replace("${SQL_PASSWORD}", sqlPassword);
}

// Connection string logging removed for security

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity - using custom controllers instead of default UI
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings - strengthened for production
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = !builder.Environment.IsDevelopment(); // Require special chars in production
    options.Password.RequiredLength = builder.Environment.IsDevelopment() ? 8 : 12; // Longer passwords in production
    options.Password.RequiredUniqueChars = builder.Environment.IsDevelopment() ? 1 : 4; // More unique chars in production

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = !builder.Environment.IsDevelopment(); // Only require in production
    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment(); // Only require in production
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookies to redirect to custom login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

// Add authentication providers (temporarily disabled for initial development)
// builder.Services.AddAuthentication()
//     .AddMicrosoftAccount(options =>
//     {
//         // Configure these in appsettings.json or user secrets
//         options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
//         options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
//     });

// Add MVC services
builder.Services.AddControllersWithViews();

// Configure form options for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.KeyLengthLimit = int.MaxValue;
    options.BufferBody = true;
    options.BufferBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});

// Add HttpContextAccessor for multi-tenant context
builder.Services.AddHttpContextAccessor();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add memory cache for PDF text caching
builder.Services.AddMemoryCache();

// Add custom services
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<IQuestionTypeService, QuestionTypeService>();
builder.Services.AddScoped<QuestionAssignmentService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<IQuestionChangeTrackingService, QuestionChangeTrackingService>();
builder.Services.AddScoped<IResponseChangeTrackingService, ResponseChangeTrackingService>();
builder.Services.AddScoped<IQuestionAssignmentTrackingService, QuestionAssignmentTrackingService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IConditionalQuestionService, ConditionalQuestionService>();
builder.Services.AddScoped<IAnswerPrePopulationService, AnswerPrePopulationService>();
builder.Services.AddScoped<IResponseWorkflowService, ResponseWorkflowService>();
builder.Services.AddScoped<IESGAnalyticsService, ESGAnalyticsService>();
builder.Services.AddScoped<IFlexibleAnalyticsService, FlexibleAnalyticsService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IPdfAnalysisService, PdfAnalysisService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddScoped<IPdfTextCacheService, PdfTextCacheService>();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin"));
    options.AddPolicy("OrgAdmin", policy => policy.RequireRole("OrgAdmin"));
    options.AddPolicy("OrgAdminOrHigher", policy => policy.RequireRole("PlatformAdmin", "OrgAdmin"));
    options.AddPolicy("CampaignManager", policy => policy.RequireRole("CampaignManager"));
    options.AddPolicy("CampaignManagerOrHigher", policy => policy.RequireRole("PlatformAdmin", "OrgAdmin", "CampaignManager"));
    options.AddPolicy("LeadResponder", policy => policy.RequireRole("PlatformAdmin", "LeadResponder"));
    options.AddPolicy("Responder", policy => policy.RequireRole("PlatformAdmin", "LeadResponder", "Responder"));
    options.AddPolicy("Reviewer", policy => policy.RequireRole("PlatformAdmin", "Reviewer"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Add detailed error logging for development
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception in request pipeline for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            throw;
        }
        
        // Log 400 errors specifically
        if (context.Response.StatusCode == 400)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("400 Bad Request for {Method} {Path}. Content-Type: {ContentType}, Content-Length: {ContentLength}", 
                context.Request.Method, context.Request.Path, 
                context.Request.ContentType, context.Request.ContentLength);
        }
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add organization context middleware after authentication
app.UseOrganizationContext();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Initialize database and seed data
await InitializeDatabaseAsync(app);

app.Run();

async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed roles
        await SeedRolesAsync(roleManager);
        
        // Seed initial data
        await SeedInitialDataAsync(context, userManager);
        
        Console.WriteLine("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw;
    }
}

async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    var roles = new[] { "PlatformAdmin", "OrgAdmin", "CampaignManager", "LeadResponder", "Responder", "Reviewer" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

async Task SeedInitialDataAsync(ApplicationDbContext context, UserManager<User> userManager)
{
    // Only seed if database is empty
    if (await context.Organizations.AnyAsync())
    {
        return;
    }
    
    // Create a sample organization with full branding
    var sampleOrg = new Organization
    {
        Name = "Sample ESG Organization",
        Type = ESGPlatform.Models.Entities.OrganizationType.PlatformOrganization,
        Description = "A sample organization for testing the ESG platform",
        PrimaryColor = "#007bff",
        SecondaryColor = "#6c757d",
        AccentColor = "#28a745",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    context.Organizations.Add(sampleOrg);
    await context.SaveChangesAsync();
    
    // Create a platform admin user
    var adminUser = new User
    {
        UserName = "admin@esgplatform.com",
        Email = "admin@esgplatform.com",
        FirstName = "Platform",
        LastName = "Administrator",
        OrganizationId = sampleOrg.Id,
        EmailConfirmed = true,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    var result = await userManager.CreateAsync(adminUser, "Admin123!");
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "PlatformAdmin");
        Console.WriteLine("Platform admin user created: admin@esgplatform.com / Admin123!");
    }
}
