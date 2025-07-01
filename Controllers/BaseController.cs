using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ESGPlatform.Models.Entities;
using ESGPlatform.Services;

namespace ESGPlatform.Controllers;

public abstract class BaseController : Controller
{
    protected int? CurrentOrganizationId
    {
        get
        {
            if (HttpContext.Items.TryGetValue("CurrentOrganizationId", out var orgId))
            {
                return orgId as int?;
            }
            return null;
        }
    }

    protected string? CurrentOrganizationName
    {
        get
        {
            if (HttpContext.Items.TryGetValue("CurrentOrganizationName", out var orgName))
            {
                return orgName as string;
            }
            return null;
        }
    }

    protected OrganizationType? CurrentOrganizationType
    {
        get
        {
            if (HttpContext.Items.TryGetValue("CurrentOrganizationType", out var orgType))
            {
                return orgType as OrganizationType?;
            }
            return null;
        }
    }

    protected bool IsCurrentOrgPlatformType => CurrentOrganizationType == OrganizationType.PlatformOrganization;
    
    protected bool IsCurrentOrgSupplierType => CurrentOrganizationType == OrganizationType.SupplierOrganization;

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected bool IsPlatformAdmin => User.IsInRole("PlatformAdmin");

    protected bool IsOrgAdmin => User.IsInRole("OrgAdmin");

    protected bool IsCampaignManager => User.IsInRole("CampaignManager");

    protected bool IsLeadResponder => User.IsInRole("LeadResponder");

    protected bool IsResponder => User.IsInRole("Responder");

    protected bool IsReviewer => User.IsInRole("Reviewer");

    // Branding context methods
    protected async Task<BrandingContext> GetBrandingContextAsync(int? campaignId = null, int? organizationId = null)
    {
        var brandingService = HttpContext.RequestServices.GetService<IBrandingService>();
        if (brandingService == null || string.IsNullOrEmpty(CurrentUserId))
        {
            return GetFallbackBrandingContext();
        }

        return await brandingService.GetBrandingContextAsync(CurrentUserId, campaignId, organizationId);
    }

    protected async Task<BrandingContext> GetCampaignBrandingContextAsync(int campaignId)
    {
        var brandingService = HttpContext.RequestServices.GetService<IBrandingService>();
        if (brandingService == null)
        {
            return GetFallbackBrandingContext();
        }

        return await brandingService.GetCampaignBrandingAsync(campaignId);
    }

    protected BrandingContext GetFallbackBrandingContext()
    {
        return new BrandingContext
        {
            OrganizationId = CurrentOrganizationId ?? 0,
            OrganizationName = CurrentOrganizationName ?? "ESG Platform",
            LogoUrl = null,
            PrimaryColor = "#007bff",
            SecondaryColor = "#6c757d",
            Theme = "Default",
            BrandingSource = BrandingSource.System,
            SourceId = 0
        };
    }

    protected async Task SetBrandingContextAsync(int? campaignId = null, int? organizationId = null)
    {
        var brandingContext = await GetBrandingContextAsync(campaignId, organizationId);
        ViewBag.BrandingContext = brandingContext;
        ViewBag.BrandingCssVariables = brandingContext.ToCssVariables();
        ViewBag.BrandingSource = brandingContext.BrandingSource.ToString();
    }

    public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        // Pass organization context to all views
        ViewBag.CurrentOrganizationId = CurrentOrganizationId;
        ViewBag.CurrentOrganizationName = CurrentOrganizationName;
        ViewBag.CurrentOrganizationType = CurrentOrganizationType;
        ViewBag.IsCurrentOrgPlatformType = IsCurrentOrgPlatformType;
        ViewBag.IsCurrentOrgSupplierType = IsCurrentOrgSupplierType;
        ViewBag.IsPlatformAdmin = IsPlatformAdmin;
        ViewBag.IsOrgAdmin = IsOrgAdmin;
        ViewBag.IsOrgAdminOrHigher = IsPlatformAdmin || IsOrgAdmin;
        ViewBag.IsCampaignManager = IsCampaignManager;
        ViewBag.IsLeadResponder = IsLeadResponder;
        ViewBag.IsResponder = IsResponder;
        ViewBag.IsReviewer = IsReviewer;

        // Set default branding context
        try
        {
            var brandingContext = await GetBrandingContextAsync();
            ViewBag.BrandingContext = brandingContext;
            ViewBag.BrandingCssVariables = brandingContext.ToCssVariables();
            ViewBag.BrandingSource = brandingContext.BrandingSource.ToString();
        }
        catch (Exception)
        {
            // Fallback to default branding if there's an error
            var fallbackContext = GetFallbackBrandingContext();
            ViewBag.BrandingContext = fallbackContext;
            ViewBag.BrandingCssVariables = fallbackContext.ToCssVariables();
            ViewBag.BrandingSource = fallbackContext.BrandingSource.ToString();
        }

        await next();
    }
} 