using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IBrandingService
{
    Task<BrandingContext> GetBrandingContextAsync(string userId, int? campaignId = null, int? organizationId = null);
    Task<BrandingContext> GetDefaultBrandingAsync(int organizationId);
    Task<BrandingContext> GetCampaignBrandingAsync(int campaignId);
    Task<BrandingContext?> GetPrimaryRelationshipBrandingAsync(int supplierOrganizationId);
}

public class BrandingService : IBrandingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BrandingService> _logger;

    public BrandingService(ApplicationDbContext context, ILogger<BrandingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BrandingContext> GetBrandingContextAsync(string userId, int? campaignId = null, int? organizationId = null)
    {
        try
        {
            _logger.LogInformation("Getting branding context for user {UserIdHash}, campaignId: {CampaignId}, organizationId: {OrganizationId}", 
                HashUserId(userId), campaignId, organizationId);
            
            var user = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found, returning fallback branding", userId);
                return GetFallbackBranding();
            }

            _logger.LogInformation("Found user {UserIdHash} with organization {OrganizationId} ({OrganizationName})", 
                HashUserId(userId), user.OrganizationId, user.Organization?.Name);

            // If specific campaign context is provided
            if (campaignId.HasValue)
            {
                return await GetCampaignBrandingAsync(campaignId.Value);
            }

            // If specific organization context is provided
            if (organizationId.HasValue)
            {
                return await GetDefaultBrandingAsync(organizationId.Value);
            }

            // For supplier organizations, check if they have a primary relationship
            if (user.Organization?.IsSupplierOrganization == true)
            {
                var primaryBranding = await GetPrimaryRelationshipBrandingAsync(user.OrganizationId);
                if (primaryBranding != null)
                {
                    return primaryBranding;
                }
            }

            // Default to user's organization branding
            var brandingContext = await GetDefaultBrandingAsync(user.OrganizationId);
            //_logger.LogInformation("Returning branding context: {OrganizationName}, {PrimaryColor}, {LogoUrl}", brandingContext.OrganizationName, brandingContext.PrimaryColor, brandingContext.LogoUrl);
            return brandingContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branding context for user {UserId}", userId);
            return GetFallbackBranding();
        }
    }

    public async Task<BrandingContext> GetDefaultBrandingAsync(int organizationId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
        {
            return GetFallbackBranding();
        }

        return new BrandingContext
        {
            OrganizationId = organization.Id,
            OrganizationName = organization.Name,
            LogoUrl = organization.LogoUrl,
            PrimaryColor = organization.PrimaryColor ?? "#1a365d",
            SecondaryColor = organization.SecondaryColor ?? "#2d3748",
            AccentColor = organization.AccentColor,
            NavigationTextColor = organization.NavigationTextColor ?? "#ffffff",
            Theme = organization.Theme ?? "Default",
            BrandingSource = BrandingSource.Organization,
            SourceId = organization.Id
        };
    }

    public async Task<BrandingContext> GetCampaignBrandingAsync(int campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign?.Organization == null)
        {
            return GetFallbackBranding();
        }

        return new BrandingContext
        {
            OrganizationId = campaign.OrganizationId,
            OrganizationName = campaign.Organization.Name,
            LogoUrl = campaign.Organization.LogoUrl,
            PrimaryColor = campaign.Organization.PrimaryColor ?? "#1a365d",
            SecondaryColor = campaign.Organization.SecondaryColor ?? "#2d3748",
            AccentColor = campaign.Organization.AccentColor,
            NavigationTextColor = campaign.Organization.NavigationTextColor ?? "#ffffff",
            Theme = campaign.Organization.Theme ?? "Default",
            BrandingSource = BrandingSource.Campaign,
            SourceId = campaignId,
            CampaignName = campaign.Name
        };
    }

    public async Task<BrandingContext?> GetPrimaryRelationshipBrandingAsync(int supplierOrganizationId)
    {
        var primaryRelationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.CreatedByOrganization)
            .Where(or => or.SupplierOrganizationId == supplierOrganizationId && or.IsActive)
            .OrderByDescending(or => or.IsPrimaryRelationship)
            .ThenBy(or => or.CreatedAt)
            .FirstOrDefaultAsync();

        if (primaryRelationship?.PlatformOrganization == null)
        {
            return null;
        }

        var brandingOrg = primaryRelationship.CreatedByOrganization ?? primaryRelationship.PlatformOrganization;

        return new BrandingContext
        {
            OrganizationId = brandingOrg.Id,
            OrganizationName = brandingOrg.Name,
            LogoUrl = brandingOrg.LogoUrl,
            PrimaryColor = brandingOrg.PrimaryColor ?? "#1a365d",
            SecondaryColor = brandingOrg.SecondaryColor ?? "#2d3748",
            AccentColor = brandingOrg.AccentColor,
            NavigationTextColor = brandingOrg.NavigationTextColor ?? "#ffffff",
            Theme = brandingOrg.Theme ?? "Default",
            BrandingSource = BrandingSource.PrimaryRelationship,
            SourceId = primaryRelationship.Id,
            RelationshipDescription = primaryRelationship.RelationshipDisplayName
        };
    }

    private static BrandingContext GetFallbackBranding()
    {
        return new BrandingContext
        {
            OrganizationId = 0,
            OrganizationName = "ESG Platform",
            LogoUrl = null,
            PrimaryColor = "#1a365d",
            SecondaryColor = "#2d3748",
            NavigationTextColor = "#ffffff",
            Theme = "Default",
            BrandingSource = BrandingSource.System,
            SourceId = 0
        };
    }

    /// <summary>
    /// Creates a hash of the user ID for logging purposes to avoid exposing sensitive data
    /// </summary>
    private static string HashUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return "null";
            
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userId));
        return Convert.ToBase64String(hashBytes).Substring(0, 8); // First 8 characters of hash
    }
}

public class BrandingContext
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#007bff";
    public string SecondaryColor { get; set; } = "#6c757d";
    public string? AccentColor { get; set; }
    public string NavigationTextColor { get; set; } = "#ffffff";
    public string Theme { get; set; } = "Default";
    public BrandingSource BrandingSource { get; set; }
    public int SourceId { get; set; }
    public string? CampaignName { get; set; }
    public string? RelationshipDescription { get; set; }

    // CSS Variables for dynamic styling
    public string ToCssVariables()
    {
        var primaryRgb = HexToRgb(PrimaryColor);
        var secondaryRgb = HexToRgb(SecondaryColor);
        var accentRgb = HexToRgb(AccentColor ?? SecondaryColor);
        
        return $@"
            --brand-primary: {PrimaryColor};
            --brand-secondary: {SecondaryColor};
            --brand-accent: {AccentColor ?? SecondaryColor};
            --brand-nav-text: {NavigationTextColor};
            --brand-theme: {Theme};
            --brand-primary-rgb: {primaryRgb};
            --brand-secondary-rgb: {secondaryRgb};
            --brand-accent-rgb: {accentRgb};
        ";
    }
    
    private static string HexToRgb(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
            return "44, 62, 80"; // Default fallback
            
        try
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return $"{r}, {g}, {b}";
        }
        catch
        {
            return "44, 62, 80"; // Default fallback
        }
    }

    // Bootstrap CSS classes
    public string PrimaryButtonClass => "btn-primary";
    public string SecondaryButtonClass => "btn-secondary";
    public string PrimaryBadgeClass => "badge bg-primary";
    public string SecondaryBadgeClass => "badge bg-secondary";

    // Custom CSS for branded elements
    public string GetBrandedCardHeaderStyle()
    {
        return $"background: linear-gradient(135deg, {PrimaryColor}, {SecondaryColor}); color: white;";
    }

    public string GetBrandedNavbarStyle()
    {
        return $"background-color: {PrimaryColor}; color: {NavigationTextColor};";
    }
}

public enum BrandingSource
{
    System,
    Organization,
    Campaign,
    PrimaryRelationship,
    UserContext
} 