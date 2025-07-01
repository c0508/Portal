using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Controllers.Admin;

[Authorize(Policy = "OrgAdminOrHigher")]
public class OrganizationRelationshipController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationRelationshipController> _logger;

    public OrganizationRelationshipController(ApplicationDbContext context, ILogger<OrganizationRelationshipController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /OrganizationRelationship
    public async Task<IActionResult> Index(string? searchFilter, bool? activeFilter, int? platformOrgFilter, int? supplierOrgFilter)
    {
        var query = _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .Include(or => or.CreatedByUser)
            .Include(or => or.Attributes)
            .Include(or => or.CampaignAssignments)
            .AsQueryable();

        // Apply access control - non-platform admins only see relationships involving their organization
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue)
        {
            query = query.Where(or => or.PlatformOrganizationId == CurrentOrganizationId.Value || 
                                     or.SupplierOrganizationId == CurrentOrganizationId.Value);
        }

        // Apply filters
        if (!string.IsNullOrEmpty(searchFilter))
        {
            query = query.Where(or => or.PlatformOrganization.Name.Contains(searchFilter) ||
                                     or.SupplierOrganization.Name.Contains(searchFilter) ||
                                     (or.Description != null && or.Description.Contains(searchFilter)));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(or => or.IsActive == activeFilter.Value);
        }

        if (platformOrgFilter.HasValue)
        {
            query = query.Where(or => or.PlatformOrganizationId == platformOrgFilter.Value);
        }

        if (supplierOrgFilter.HasValue)
        {
            query = query.Where(or => or.SupplierOrganizationId == supplierOrgFilter.Value);
        }

        var relationships = await query
            .OrderBy(or => or.PlatformOrganization.Name)
            .ThenBy(or => or.SupplierOrganization.Name)
            .Select(or => new OrganizationRelationshipSummaryViewModel
            {
                Id = or.Id,
                PlatformOrganizationName = or.PlatformOrganization.Name,
                SupplierOrganizationName = or.SupplierOrganization.Name,
                RelationshipType = or.RelationshipType,
                IsActive = or.IsActive,
                IsPrimaryRelationship = or.IsPrimaryRelationship,
                CreatedAt = or.CreatedAt,
                CreatedByUserName = or.CreatedByUser != null ? or.CreatedByUser.UserName : null,
                AttributeCount = or.Attributes.Count(a => a.IsActive),
                CampaignAssignmentCount = or.CampaignAssignments.Count(),
                RelationshipDisplayName = or.RelationshipDisplayName
            })
            .ToListAsync();

        // Get dropdown data for filters
        var availablePlatformOrgs = await _context.Organizations
            .Where(o => o.Type == OrganizationType.PlatformOrganization && o.IsActive)
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        var availableSupplierOrgs = await _context.Organizations
            .Where(o => o.Type == OrganizationType.SupplierOrganization && o.IsActive)
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        // Calculate statistics
        var allRelationships = await _context.OrganizationRelationships.CountAsync();
        var activeRelationships = await _context.OrganizationRelationships.CountAsync(or => or.IsActive);

        var model = new OrganizationRelationshipListViewModel
        {
            Relationships = relationships,
            SearchFilter = searchFilter,
            ActiveFilter = activeFilter,
            PlatformOrganizationFilter = platformOrgFilter,
            SupplierOrganizationFilter = supplierOrgFilter,
            AvailablePlatformOrganizations = availablePlatformOrgs,
            AvailableSupplierOrganizations = availableSupplierOrgs,
            TotalRelationships = allRelationships,
            ActiveRelationships = activeRelationships,
            InactiveRelationships = allRelationships - activeRelationships
        };

        return View(model);
    }

    // GET: /OrganizationRelationship/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var relationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .Include(or => or.CreatedByUser)
            .Include(or => or.CreatedByOrganization)
            .Include(or => or.Attributes.Where(a => a.IsActive))
            .Include(or => or.CampaignAssignments)
                .ThenInclude(ca => ca.Campaign)
            .FirstOrDefaultAsync(or => or.Id == id);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        var model = new OrganizationRelationshipViewModel
        {
            Id = relationship.Id,
            PlatformOrganizationId = relationship.PlatformOrganizationId,
            SupplierOrganizationId = relationship.SupplierOrganizationId,
            RelationshipType = relationship.RelationshipType,
            Description = relationship.Description,
            IsActive = relationship.IsActive,
            IsPrimaryRelationship = relationship.IsPrimaryRelationship,
            PlatformOrganizationName = relationship.PlatformOrganization.Name,
            SupplierOrganizationName = relationship.SupplierOrganization.Name,
            CreatedAt = relationship.CreatedAt,
            UpdatedAt = relationship.UpdatedAt,
            CreatedByUserName = relationship.CreatedByUser?.UserName,
            Attributes = relationship.Attributes.Select(a => new OrganizationRelationshipAttributeViewModel
            {
                Id = a.Id,
                OrganizationRelationshipId = a.OrganizationRelationshipId,
                AttributeType = a.AttributeType,
                AttributeValue = a.AttributeValue,
                Description = a.Description,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                DisplayText = a.DisplayText
            }).ToList()
        };

        return View(model);
    }

    // GET: /OrganizationRelationship/Create
    public async Task<IActionResult> Create()
    {
        var model = new OrganizationRelationshipCreateViewModel();
        await PopulateDropdowns(model);
        return View(model);
    }

    // POST: /OrganizationRelationship/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganizationRelationshipCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Validate that the organizations exist and are of correct types
            var platformOrg = await _context.Organizations.FindAsync(model.PlatformOrganizationId);
            var supplierOrg = await _context.Organizations.FindAsync(model.SupplierOrganizationId);

            if (platformOrg == null || supplierOrg == null)
            {
                ModelState.AddModelError("", "Selected organizations not found.");
            }
            else if (platformOrg.Type != OrganizationType.PlatformOrganization)
            {
                ModelState.AddModelError("PlatformOrganizationId", "Selected organization is not a Platform Organization.");
            }
            else if (supplierOrg.Type != OrganizationType.SupplierOrganization)
            {
                ModelState.AddModelError("SupplierOrganizationId", "Selected organization is not a Supplier Organization.");
            }
            else if (model.PlatformOrganizationId == model.SupplierOrganizationId)
            {
                ModelState.AddModelError("SupplierOrganizationId", "Platform and Supplier organizations must be different.");
            }
            else
            {
                // Check if relationship already exists
                var existingRelationship = await _context.OrganizationRelationships
                    .AnyAsync(or => or.PlatformOrganizationId == model.PlatformOrganizationId &&
                                   or.SupplierOrganizationId == model.SupplierOrganizationId);

                if (existingRelationship)
                {
                    ModelState.AddModelError("", "A relationship between these organizations already exists.");
                }
                else
                {
                    // Access control - non-platform admins can only create relationships involving their organization
                    if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
                        model.PlatformOrganizationId != CurrentOrganizationId.Value &&
                        model.SupplierOrganizationId != CurrentOrganizationId.Value)
                    {
                        ModelState.AddModelError("", "You can only create relationships involving your organization.");
                    }
                    else
                    {
                        var relationship = new OrganizationRelationship
                        {
                            PlatformOrganizationId = model.PlatformOrganizationId,
                            SupplierOrganizationId = model.SupplierOrganizationId,
                            RelationshipType = model.RelationshipType,
                            Description = model.Description,
                            IsActive = model.IsActive,
                            IsPrimaryRelationship = model.IsPrimaryRelationship,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = CurrentUserId,
                            CreatedByOrganizationId = CurrentOrganizationId
                        };

                        _context.OrganizationRelationships.Add(relationship);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Organization relationship created: {PlatformOrgName} → {SupplierOrgName} by {UserId}",
                            platformOrg.Name, supplierOrg.Name, CurrentUserId);

                        TempData["Success"] = $"Relationship created successfully: {platformOrg.Name} → {supplierOrg.Name}";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
        }

        await PopulateDropdowns(model);
        return View(model);
    }

    // GET: /OrganizationRelationship/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var relationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .FirstOrDefaultAsync(or => or.Id == id);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        var model = new OrganizationRelationshipViewModel
        {
            Id = relationship.Id,
            PlatformOrganizationId = relationship.PlatformOrganizationId,
            SupplierOrganizationId = relationship.SupplierOrganizationId,
            RelationshipType = relationship.RelationshipType,
            Description = relationship.Description,
            IsActive = relationship.IsActive,
            IsPrimaryRelationship = relationship.IsPrimaryRelationship,
            PlatformOrganizationName = relationship.PlatformOrganization.Name,
            SupplierOrganizationName = relationship.SupplierOrganization.Name,
            CreatedAt = relationship.CreatedAt,
            UpdatedAt = relationship.UpdatedAt
        };

        await PopulateDropdowns(model);
        return View(model);
    }

    // POST: /OrganizationRelationship/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrganizationRelationshipViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var relationship = await _context.OrganizationRelationships
                .Include(or => or.PlatformOrganization)
                .Include(or => or.SupplierOrganization)
                .FirstOrDefaultAsync(or => or.Id == id);

            if (relationship == null)
            {
                return NotFound();
            }

            // Access control check
            if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
                relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
                relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
            {
                return Forbid();
            }

            relationship.RelationshipType = model.RelationshipType;
            relationship.Description = model.Description;
            relationship.IsActive = model.IsActive;
            relationship.IsPrimaryRelationship = model.IsPrimaryRelationship;
            relationship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization relationship updated: {RelationshipDisplayName} by {UserId}",
                relationship.RelationshipDisplayName, CurrentUserId);

            TempData["Success"] = $"Relationship updated successfully: {relationship.RelationshipDisplayName}";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateDropdowns(model);
        return View(model);
    }

    // GET: /OrganizationRelationship/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var relationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .Include(or => or.CampaignAssignments)
            .Include(or => or.Attributes)
            .FirstOrDefaultAsync(or => or.Id == id);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        // Add relationship counts to ViewBag for display
        ViewBag.CampaignAssignmentCount = relationship.CampaignAssignments.Count;
        ViewBag.AttributeCount = relationship.Attributes.Count;
        ViewBag.HasDependencies = relationship.CampaignAssignments.Any() || relationship.Attributes.Any();

        return View(relationship);
    }

    // POST: /OrganizationRelationship/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var relationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .Include(or => or.CampaignAssignments)
            .Include(or => or.Attributes)
            .FirstOrDefaultAsync(or => or.Id == id);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        // Check for dependencies
        var errors = new List<string>();

        if (relationship.CampaignAssignments.Any())
        {
            errors.Add($"Relationship has {relationship.CampaignAssignments.Count} campaign assignment(s). Please complete or remove assignments first.");
        }

        if (errors.Any())
        {
            var errorMessage = "Cannot delete relationship due to the following dependencies:\n• " + string.Join("\n• ", errors);
            TempData["Error"] = errorMessage;
            return RedirectToAction(nameof(Delete), new { id });
        }

        // If no blocking dependencies, proceed with deletion (attributes will cascade delete)
        _context.OrganizationRelationships.Remove(relationship);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization relationship deleted: {RelationshipDisplayName} by {UserId}",
            relationship.RelationshipDisplayName, CurrentUserId);

        TempData["Success"] = $"Relationship deleted successfully: {relationship.RelationshipDisplayName}";
        return RedirectToAction(nameof(Index));
    }

    // GET: /OrganizationRelationship/ManageAttributes/5
    public async Task<IActionResult> ManageAttributes(int id)
    {
        var relationship = await _context.OrganizationRelationships
            .Include(or => or.PlatformOrganization)
            .Include(or => or.SupplierOrganization)
            .Include(or => or.Attributes)
            .FirstOrDefaultAsync(or => or.Id == id);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        var model = new ManageRelationshipAttributesViewModel
        {
            OrganizationRelationshipId = relationship.Id,
            RelationshipDisplayName = relationship.RelationshipDisplayName,
            Attributes = relationship.Attributes.Select(a => new OrganizationRelationshipAttributeViewModel
            {
                Id = a.Id,
                OrganizationRelationshipId = a.OrganizationRelationshipId,
                AttributeType = a.AttributeType,
                AttributeValue = a.AttributeValue,
                Description = a.Description,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                DisplayText = a.DisplayText
            }).ToList(),
            NewAttribute = new OrganizationRelationshipAttributeViewModel
            {
                OrganizationRelationshipId = relationship.Id,
                IsActive = true
            }
        };

        await PopulateAttributeDropdowns(model);
        return View(model);
    }

    // Helper method to populate dropdown lists
    private async Task PopulateDropdowns(object viewModel)
    {
        var platformOrgs = await _context.Organizations
            .Where(o => o.Type == OrganizationType.PlatformOrganization && o.IsActive)
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        var supplierOrgs = await _context.Organizations
            .Where(o => o.Type == OrganizationType.SupplierOrganization && o.IsActive)
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        var relationshipTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "Supplier", Text = "Supplier" },
            new SelectListItem { Value = "Partner", Text = "Partner" },
            new SelectListItem { Value = "Vendor", Text = "Vendor" },
            new SelectListItem { Value = "Contractor", Text = "Contractor" }
        };

        // Set properties based on ViewModel type
        if (viewModel is OrganizationRelationshipCreateViewModel createModel)
        {
            createModel.AvailablePlatformOrganizations = platformOrgs;
            createModel.AvailableSupplierOrganizations = supplierOrgs;
            createModel.AvailableRelationshipTypes = relationshipTypes;
        }
        else if (viewModel is OrganizationRelationshipViewModel editModel)
        {
            editModel.AvailablePlatformOrganizations = platformOrgs;
            editModel.AvailableSupplierOrganizations = supplierOrgs;
            editModel.AvailableRelationshipTypes = relationshipTypes;
        }
    }

    // Helper method to populate attribute dropdowns
    private async Task PopulateAttributeDropdowns(ManageRelationshipAttributesViewModel model)
    {
        // Get available attribute types from master data
        var attributeTypes = await _context.OrganizationAttributeTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Code, Text = t.Name })
            .ToListAsync();

        model.AvailableAttributeTypes = attributeTypes;

        // Get available values for each attribute type
        var attributeTypesWithValues = await _context.OrganizationAttributeTypes
            .Include(t => t.Values.Where(v => v.IsActive))
            .Where(t => t.IsActive)
            .ToListAsync();

        foreach (var attributeType in attributeTypesWithValues)
        {
            if (attributeType.Values.Any())
            {
                var values = attributeType.Values
                    .OrderBy(v => v.DisplayOrder)
                    .Select(v => new SelectListItem { Value = v.Name, Text = v.Name })
                    .ToList();

                model.AvailableAttributeValues[attributeType.Code] = values;
            }
        }
    }

    // POST: /OrganizationRelationship/AddAttribute
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(ManageRelationshipAttributesViewModel model)
    {
        var relationship = await _context.OrganizationRelationships
            .FirstOrDefaultAsync(or => or.Id == model.OrganizationRelationshipId);

        if (relationship == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            relationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            relationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        if (ModelState.IsValid && model.NewAttribute != null)
        {
            // Check if this attribute already exists for this relationship
            var existingAttribute = await _context.OrganizationRelationshipAttributes
                .FirstOrDefaultAsync(a => a.OrganizationRelationshipId == model.OrganizationRelationshipId &&
                                          a.AttributeType == model.NewAttribute.AttributeType &&
                                          a.AttributeValue == model.NewAttribute.AttributeValue);

            if (existingAttribute != null)
            {
                TempData["Error"] = $"Attribute '{model.NewAttribute.AttributeType}: {model.NewAttribute.AttributeValue}' already exists for this relationship.";
                return RedirectToAction(nameof(ManageAttributes), new { id = model.OrganizationRelationshipId });
            }

            var newAttribute = new OrganizationRelationshipAttribute
            {
                OrganizationRelationshipId = model.OrganizationRelationshipId,
                AttributeType = model.NewAttribute.AttributeType,
                AttributeValue = model.NewAttribute.AttributeValue,
                Description = model.NewAttribute.Description,
                IsActive = model.NewAttribute.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.OrganizationRelationshipAttributes.Add(newAttribute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attribute added to relationship {RelationshipId}: {AttributeType} = {AttributeValue} by {UserId}",
                model.OrganizationRelationshipId, model.NewAttribute.AttributeType, model.NewAttribute.AttributeValue, CurrentUserId);

            TempData["Success"] = $"Attribute '{model.NewAttribute.AttributeType}: {model.NewAttribute.AttributeValue}' added successfully.";
        }
        else
        {
            TempData["Error"] = "Please provide all required attribute information.";
        }

        return RedirectToAction(nameof(ManageAttributes), new { id = model.OrganizationRelationshipId });
    }

    // POST: /OrganizationRelationship/UpdateAttribute
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttribute(int AttributeId, int OrganizationRelationshipId, string AttributeType, string AttributeValue, string? Description, bool IsActive)
    {
        var attribute = await _context.OrganizationRelationshipAttributes
            .Include(a => a.OrganizationRelationship)
            .FirstOrDefaultAsync(a => a.Id == AttributeId);

        if (attribute == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            attribute.OrganizationRelationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            attribute.OrganizationRelationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        // Check if updating the value would create a duplicate
        if (attribute.AttributeValue != AttributeValue)
        {
            var duplicateAttribute = await _context.OrganizationRelationshipAttributes
                .FirstOrDefaultAsync(a => a.OrganizationRelationshipId == OrganizationRelationshipId &&
                                          a.AttributeType == AttributeType &&
                                          a.AttributeValue == AttributeValue &&
                                          a.Id != AttributeId);

            if (duplicateAttribute != null)
            {
                TempData["Error"] = $"Attribute '{AttributeType}: {AttributeValue}' already exists for this relationship.";
                return RedirectToAction(nameof(ManageAttributes), new { id = OrganizationRelationshipId });
            }
        }

        // Update attribute
        var oldValue = attribute.AttributeValue;
        attribute.AttributeValue = AttributeValue;
        attribute.Description = Description;
        attribute.IsActive = IsActive;
        attribute.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Attribute updated for relationship {RelationshipId}: {AttributeType} changed from '{OldValue}' to '{NewValue}' by {UserId}",
            OrganizationRelationshipId, AttributeType, oldValue, AttributeValue, CurrentUserId);

        TempData["Success"] = $"Attribute '{AttributeType}: {AttributeValue}' updated successfully.";
        return RedirectToAction(nameof(ManageAttributes), new { id = OrganizationRelationshipId });
    }

    // POST: /OrganizationRelationship/DeleteAttribute
    [HttpPost]
    [ValidateAntiForgeryToken]  
    public async Task<IActionResult> DeleteAttribute(int attributeId, int relationshipId)
    {
        var attribute = await _context.OrganizationRelationshipAttributes
            .Include(a => a.OrganizationRelationship)
            .FirstOrDefaultAsync(a => a.Id == attributeId);

        if (attribute == null)
        {
            return NotFound();
        }

        // Access control check
        if (!IsPlatformAdmin && CurrentOrganizationId.HasValue &&
            attribute.OrganizationRelationship.PlatformOrganizationId != CurrentOrganizationId.Value &&
            attribute.OrganizationRelationship.SupplierOrganizationId != CurrentOrganizationId.Value)
        {
            return Forbid();
        }

        var displayText = attribute.DisplayText;
        _context.OrganizationRelationshipAttributes.Remove(attribute);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attribute deleted from relationship {RelationshipId}: {AttributeDisplayText} by {UserId}",
            relationshipId, displayText, CurrentUserId);

        TempData["Success"] = $"Attribute '{displayText}' deleted successfully.";
        return RedirectToAction(nameof(ManageAttributes), new { id = relationshipId });
    }
} 