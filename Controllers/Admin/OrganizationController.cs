using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
public class OrganizationController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(ApplicationDbContext context, ILogger<OrganizationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Organization
    public async Task<IActionResult> Index()
    {
        var organizations = await _context.Organizations
            .Include(o => o.Users)
            .OrderBy(o => o.Name)
            .ToListAsync();

        return View(organizations);
    }

    // GET: /Organization/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var organization = await _context.Organizations
            .Include(o => o.Users)
            .Include(o => o.CampaignsCreated)
            .Include(o => o.CampaignAssignments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        return View(organization);
    }

    // GET: /Organization/Create
    public async Task<IActionResult> Create()
    {
        var model = new OrganizationViewModel();
        // await PopulateMasterDataDropdowns(model); // Temporarily disabled - will be replaced with relationship-specific attribute system
        return View(model);
    }

    // POST: /Organization/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganizationViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organization = new Organization
            {
                Name = model.Name,
                Description = model.Description,
                Type = model.Type,
                LogoUrl = model.LogoUrl,
                PrimaryColor = model.PrimaryColor,
                SecondaryColor = model.SecondaryColor,
                AccentColor = model.AccentColor,
                Theme = model.Theme ?? "Default",
                IsActive = model.IsActive,
                // Note: Organization attributes are now handled via OrganizationRelationshipAttribute
                CreatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Auto-create relationship if a user creates a Supplier Organization and they belong to a Platform Organization
            if (organization.Type == OrganizationType.SupplierOrganization && CurrentOrganizationId.HasValue)
            {
                var currentOrg = await _context.Organizations.FindAsync(CurrentOrganizationId.Value);
                if (currentOrg?.Type == OrganizationType.PlatformOrganization)
                {
                    var relationship = new OrganizationRelationship
                    {
                        PlatformOrganizationId = CurrentOrganizationId.Value,
                        SupplierOrganizationId = organization.Id,
                        RelationshipType = "Supplier",
                        Description = $"Auto-created relationship: {currentOrg.Name} → {organization.Name}",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = CurrentUserId,
                        CreatedByOrganizationId = CurrentOrganizationId.Value,
                        IsPrimaryRelationship = true
                    };

                    _context.OrganizationRelationships.Add(relationship);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Auto-created organization relationship: Platform {PlatformOrgName} → Supplier {SupplierOrgName} by {UserId}", 
                        currentOrg.Name, organization.Name, CurrentUserId);

                    TempData["Success"] = $"Organization '{organization.Name}' created successfully with automatic relationship established.";
                }
                else
                {
                    TempData["Success"] = $"Organization '{organization.Name}' created successfully.";
                }
            }
            else
            {
                TempData["Success"] = $"Organization '{organization.Name}' created successfully.";
            }

            _logger.LogInformation("Organization created: {OrganizationName} by {UserId}", 
                organization.Name, CurrentUserId);

            return RedirectToAction(nameof(Index));
        }

        // await PopulateMasterDataDropdowns(model); // Temporarily disabled
        return View(model);
    }

    // GET: /Organization/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        var model = new OrganizationViewModel
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            Type = organization.Type,
            LogoUrl = organization.LogoUrl,
            PrimaryColor = organization.PrimaryColor,
            SecondaryColor = organization.SecondaryColor,
            AccentColor = organization.AccentColor,
            Theme = organization.Theme,
            IsActive = organization.IsActive
            // Note: Organization attributes are now relationship-specific
        };

        // await PopulateMasterDataDropdowns(model); // Temporarily disabled

        return View(model);
    }

    // POST: /Organization/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrganizationViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }

            organization.Name = model.Name;
            organization.Description = model.Description;
            organization.Type = model.Type;
            organization.LogoUrl = model.LogoUrl;
            organization.PrimaryColor = model.PrimaryColor;
            organization.SecondaryColor = model.SecondaryColor;
            organization.AccentColor = model.AccentColor;
            organization.Theme = model.Theme ?? "Default";
            organization.IsActive = model.IsActive;
            // Note: Organization attributes are now handled via OrganizationRelationshipAttribute
            // Dynamic attributes are managed separately per relationship
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization updated: {OrganizationName} by {UserId}", 
                organization.Name, CurrentUserId);

            TempData["Success"] = $"Organization '{organization.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // await PopulateMasterDataDropdowns(model); // Temporarily disabled

        return View(model);
    }

    // GET: /Organization/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var organization = await _context.Organizations
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        // Get relationship counts to inform user of potential blocking dependencies
        var assignmentCount = await _context.CampaignAssignments
            .CountAsync(ca => ca.TargetOrganizationId == id);
        var campaignCount = await _context.Campaigns
            .CountAsync(c => c.OrganizationId == id);
        var questionnaireCount = await _context.Questionnaires
            .CountAsync(q => q.OrganizationId == id);
        var platformRelationshipCount = await _context.OrganizationRelationships
            .CountAsync(or => or.PlatformOrganizationId == id);
        var supplierRelationshipCount = await _context.OrganizationRelationships
            .CountAsync(or => or.SupplierOrganizationId == id);

        // Add relationship counts to ViewBag for display in view
        ViewBag.UserCount = organization.Users.Count;
        ViewBag.AssignmentCount = assignmentCount;
        ViewBag.CampaignCount = campaignCount;
        ViewBag.QuestionnaireCount = questionnaireCount;
        ViewBag.PlatformRelationshipCount = platformRelationshipCount;
        ViewBag.SupplierRelationshipCount = supplierRelationshipCount;
        ViewBag.HasBlockingRelationships = organization.Users.Any() || assignmentCount > 0 || campaignCount > 0 || 
                                          questionnaireCount > 0 || platformRelationshipCount > 0 || supplierRelationshipCount > 0;

        return View(organization);
    }

    // POST: /Organization/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var organization = await _context.Organizations
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        // Comprehensive relationship checking before deletion
        var errors = new List<string>();

        // Check if organization has users
        var userCount = organization.Users.Count;
        if (userCount > 0)
        {
            errors.Add($"Organization has {userCount} user(s). Please reassign or remove users first.");
        }

        // Check if organization has campaign assignments as target
        var assignmentCount = await _context.CampaignAssignments
            .CountAsync(ca => ca.TargetOrganizationId == id);
        if (assignmentCount > 0)
        {
            errors.Add($"Organization has {assignmentCount} campaign assignment(s). Please complete or remove assignments first.");
        }

        // Check if organization has campaigns created by it
        var campaignCount = await _context.Campaigns
            .CountAsync(c => c.OrganizationId == id);
        if (campaignCount > 0)
        {
            errors.Add($"Organization has created {campaignCount} campaign(s). Please delete campaigns first.");
        }

        // Check if organization has questionnaires
        var questionnaireCount = await _context.Questionnaires
            .CountAsync(q => q.OrganizationId == id);
        if (questionnaireCount > 0)
        {
            errors.Add($"Organization has {questionnaireCount} questionnaire(s). Please delete questionnaires first.");
        }

        // Check if organization has relationships (as platform or supplier)
        var platformRelationshipCount = await _context.OrganizationRelationships
            .CountAsync(or => or.PlatformOrganizationId == id);
        var supplierRelationshipCount = await _context.OrganizationRelationships
            .CountAsync(or => or.SupplierOrganizationId == id);
        
        if (platformRelationshipCount > 0)
        {
            errors.Add($"Organization has {platformRelationshipCount} relationship(s) as platform. Please remove relationships first.");
        }
        if (supplierRelationshipCount > 0)
        {
            errors.Add($"Organization has {supplierRelationshipCount} relationship(s) as supplier. Please remove relationships first.");
        }

        // If there are any blocking relationships, show errors
        if (errors.Any())
        {
            var errorMessage = "Cannot delete organization due to the following dependencies:\n• " + string.Join("\n• ", errors);
            TempData["Error"] = errorMessage;
            return RedirectToAction(nameof(Delete), new { id });
        }

        // If no blocking relationships, proceed with deletion
        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization deleted: {OrganizationName} by {UserId}", 
            organization.Name, CurrentUserId);

        TempData["Success"] = $"Organization '{organization.Name}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Organization/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        organization.IsActive = !organization.IsActive;
        organization.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var status = organization.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Organization {Status}: {OrganizationName} by {UserId}", 
            status, organization.Name, CurrentUserId);

        TempData["Success"] = $"Organization '{organization.Name}' {status} successfully.";
        return RedirectToAction(nameof(Index));
    }



    /* TEMPORARILY DISABLED - Will be replaced with relationship-specific attribute system
    // Helper method to populate master data dropdown options
    private async Task PopulateMasterDataDropdowns(OrganizationViewModel model)
    {
        try
        {
            // Get all attribute types and their values
            var attributeTypes = await _context.OrganizationAttributeTypes
                .Include(t => t.Values.Where(v => v.IsActive))
                .Where(t => t.IsActive)
                .ToListAsync();

            _logger.LogDebug("Found {Count} attribute types", attributeTypes.Count);

            // Initialize dropdown lists as null (will only be populated if attribute types exist)
            model.AvailableCategories = null;
            model.AvailableIndustries = null;
            model.AvailableRegions = null;
            model.AvailableSizeCategories = null;
            model.AvailableABCSegmentations = null;
            model.AvailableSupplierClassifications = null;

            // Populate dropdowns based on attribute type codes
            foreach (var attributeType in attributeTypes)
            {
                _logger.LogDebug("Processing attribute type: {Code} - {Name} with {ValueCount} values", 
                    attributeType.Code, attributeType.Name, attributeType.Values.Count);

                if (attributeType.Values.Any())
                {
                    var selectItems = attributeType.Values
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = v.Name,
                            Text = v.Name
                        })
                        .ToList();

                    // Insert empty option at the beginning
                    selectItems.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = "",
                        Text = "-- Select --"
                    });

                    switch (attributeType.Code.ToUpper())
                    {
                        case "BUSINESS_CATEGORY":
                        case "CATEGORY":
                            model.AvailableCategories = selectItems;
                            _logger.LogDebug("Populated {Count} categories", selectItems.Count - 1);
                            break;
                        case "INDUSTRY":
                            model.AvailableIndustries = selectItems;
                            _logger.LogDebug("Populated {Count} industries", selectItems.Count - 1);
                            break;
                        case "REGION":
                            model.AvailableRegions = selectItems;
                            _logger.LogDebug("Populated {Count} regions", selectItems.Count - 1);
                            break;
                        case "SIZE_CATEGORY":
                            model.AvailableSizeCategories = selectItems;
                            _logger.LogDebug("Populated {Count} size categories", selectItems.Count - 1);
                            break;
                        case "ABC_SEGMENTATION":
                            model.AvailableABCSegmentations = selectItems;
                            _logger.LogDebug("Populated {Count} ABC segmentations", selectItems.Count - 1);
                            break;
                        case "SUPPLIER_CLASSIFICATION":
                            model.AvailableSupplierClassifications = selectItems;
                            _logger.LogDebug("Populated {Count} supplier classifications", selectItems.Count - 1);
                            break;
                        default:
                            _logger.LogDebug("Unknown attribute type code: {Code}", attributeType.Code);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating master data dropdowns");
            // Initialize with null dropdowns on error (no dropdowns will be shown)
            model.AvailableCategories = null;
            model.AvailableIndustries = null;
            model.AvailableRegions = null;
            model.AvailableSizeCategories = null;
            model.AvailableABCSegmentations = null;
            model.AvailableSupplierClassifications = null;
        }
    }
    */
}