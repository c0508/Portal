using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
public class OrganizationAttributeController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationAttributeController> _logger;

    public OrganizationAttributeController(ApplicationDbContext context, ILogger<OrganizationAttributeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /OrganizationAttribute
    public async Task<IActionResult> Index()
    {
        var attributeTypes = await _context.OrganizationAttributeTypes
            .Include(t => t.Values.Where(v => v.IsActive))
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(attributeTypes);
    }

    // GET: /OrganizationAttribute/CreateType
    public IActionResult CreateType()
    {
        return View();
    }

    // POST: /OrganizationAttribute/CreateType
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(OrganizationAttributeType model)
    {
        if (ModelState.IsValid)
        {
            model.CreatedAt = DateTime.UtcNow;
            _context.OrganizationAttributeTypes.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization attribute type created: {Code} by {UserId}", 
                model.Code, CurrentUserId);

            TempData["Success"] = $"Attribute type '{model.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: /OrganizationAttribute/EditType/5
    public async Task<IActionResult> EditType(int id)
    {
        var attributeType = await _context.OrganizationAttributeTypes.FindAsync(id);
        if (attributeType == null)
        {
            return NotFound();
        }

        return View(attributeType);
    }

    // POST: /OrganizationAttribute/EditType/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditType(int id, OrganizationAttributeType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var attributeType = await _context.OrganizationAttributeTypes.FindAsync(id);
            if (attributeType == null)
            {
                return NotFound();
            }

            attributeType.Code = model.Code;
            attributeType.Name = model.Name;
            attributeType.Description = model.Description;
            attributeType.IsActive = model.IsActive;
            attributeType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization attribute type updated: {Code} by {UserId}", 
                attributeType.Code, CurrentUserId);

            TempData["Success"] = $"Attribute type '{attributeType.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: /OrganizationAttribute/ManageValues/5
    public async Task<IActionResult> ManageValues(int id)
    {
        var attributeType = await _context.OrganizationAttributeTypes
            .Include(t => t.Values)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (attributeType == null)
        {
            return NotFound();
        }

        return View(attributeType);
    }

    // GET: /OrganizationAttribute/CreateValue?typeId=5
    public async Task<IActionResult> CreateValue(int typeId)
    {
        var attributeType = await _context.OrganizationAttributeTypes.FindAsync(typeId);
        if (attributeType == null)
        {
            return NotFound();
        }

        var model = new OrganizationAttributeValue
        {
            AttributeTypeId = typeId
        };

        ViewBag.AttributeTypeName = attributeType.Name;
        return View(model);
    }

    // POST: /OrganizationAttribute/CreateValue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateValue(OrganizationAttributeValue model)
    {
        if (ModelState.IsValid)
        {
            model.CreatedAt = DateTime.UtcNow;
            _context.OrganizationAttributeValues.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization attribute value created: {Code} by {UserId}", 
                model.Code, CurrentUserId);

            TempData["Success"] = $"Attribute value '{model.Name}' created successfully.";
            return RedirectToAction(nameof(ManageValues), new { id = model.AttributeTypeId });
        }

        var attributeType = await _context.OrganizationAttributeTypes.FindAsync(model.AttributeTypeId);
        ViewBag.AttributeTypeName = attributeType?.Name ?? "Unknown";
        return View(model);
    }

    // GET: /OrganizationAttribute/EditValue/5
    public async Task<IActionResult> EditValue(int id)
    {
        var attributeValue = await _context.OrganizationAttributeValues
            .Include(v => v.AttributeType)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (attributeValue == null)
        {
            return NotFound();
        }

        ViewBag.AttributeTypeName = attributeValue.AttributeType.Name;
        return View(attributeValue);
    }

    // POST: /OrganizationAttribute/EditValue/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditValue(int id, OrganizationAttributeValue model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var attributeValue = await _context.OrganizationAttributeValues.FindAsync(id);
            if (attributeValue == null)
            {
                return NotFound();
            }

            attributeValue.Code = model.Code;
            attributeValue.Name = model.Name;
            attributeValue.Description = model.Description;
            attributeValue.Color = model.Color;
            attributeValue.IsActive = model.IsActive;
            attributeValue.DisplayOrder = model.DisplayOrder;
            attributeValue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Organization attribute value updated: {Code} by {UserId}", 
                attributeValue.Code, CurrentUserId);

            TempData["Success"] = $"Attribute value '{attributeValue.Name}' updated successfully.";
            return RedirectToAction(nameof(ManageValues), new { id = attributeValue.AttributeTypeId });
        }

        var attributeType = await _context.OrganizationAttributeTypes.FindAsync(model.AttributeTypeId);
        ViewBag.AttributeTypeName = attributeType?.Name ?? "Unknown";
        return View(model);
    }

    // POST: /OrganizationAttribute/DeleteValue/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteValue(int id)
    {
        var attributeValue = await _context.OrganizationAttributeValues.FindAsync(id);
        if (attributeValue == null)
        {
            return NotFound();
        }

        var typeId = attributeValue.AttributeTypeId;
        
        // Soft delete
        attributeValue.IsActive = false;
        attributeValue.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization attribute value deleted: {Code} by {UserId}", 
            attributeValue.Code, CurrentUserId);

        TempData["Success"] = $"Attribute value '{attributeValue.Name}' deleted successfully.";
        return RedirectToAction(nameof(ManageValues), new { id = typeId });
    }

    // POST: /OrganizationAttribute/ReorderValues
    [HttpPost]
    public async Task<IActionResult> ReorderValues([FromBody] Dictionary<int, int> orders)
    {
        try
        {
            foreach (var order in orders)
            {
                var value = await _context.OrganizationAttributeValues.FindAsync(order.Key);
                if (value != null)
                {
                    value.DisplayOrder = order.Value;
                    value.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering attribute values");
            return Json(new { success = false, message = "Error updating order" });
        }
    }

    // GET: /OrganizationAttribute/DeleteType/5
    public async Task<IActionResult> DeleteType(int id)
    {
        var attributeType = await _context.OrganizationAttributeTypes
            .Include(t => t.Values)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (attributeType == null)
        {
            return NotFound();
        }

        // Check if any organizations are using values from this attribute type
        var usageCount = await GetAttributeTypeUsageCount(attributeType);
        ViewBag.UsageCount = usageCount;
        ViewBag.HasUsage = usageCount > 0;

        return View(attributeType);
    }

    // POST: /OrganizationAttribute/DeleteType/5
    [HttpPost, ActionName("DeleteType")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTypeConfirmed(int id, bool forceDelete = false)
    {
        var attributeType = await _context.OrganizationAttributeTypes
            .Include(t => t.Values)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (attributeType == null)
        {
            return NotFound();
        }

        // Check for usage unless force delete is specified
        if (!forceDelete)
        {
            var usageCount = await GetAttributeTypeUsageCount(attributeType);
            if (usageCount > 0)
            {
                TempData["Error"] = $"Cannot delete attribute type '{attributeType.Name}' because it is being used by {usageCount} organization(s). Please remove all references first or use force delete.";
                return RedirectToAction(nameof(DeleteType), new { id });
            }
        }
        else
        {
            // Force delete: Clear all organization references to this attribute type
            await ClearOrganizationReferences(attributeType);
        }

        // Delete all attribute values first (cascade delete)
        _context.OrganizationAttributeValues.RemoveRange(attributeType.Values);

        // Delete the attribute type
        _context.OrganizationAttributeTypes.Remove(attributeType);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Organization attribute type deleted: {Code} by {UserId}", 
            attributeType.Code, CurrentUserId);

        TempData["Success"] = $"Attribute type '{attributeType.Name}' and all its values deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /OrganizationAttribute/InitializeDefaultData
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitializeDefaultData()
    {
        try
        {
            await InitializeDefaultAttributeData();
            TempData["Success"] = "Default organization attribute data initialized successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default attribute data");
            TempData["Error"] = "Error initializing default data. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Helper method to count how many relationship-specific attributes are using values from this attribute type
    private async Task<int> GetAttributeTypeUsageCount(OrganizationAttributeType attributeType)
    {
        var valueNames = attributeType.Values.Select(v => v.Name).ToList();
        if (!valueNames.Any()) return 0;

        // Count usage in relationship-specific attributes
        return await _context.OrganizationRelationshipAttributes
            .Where(ora => ora.AttributeType == attributeType.Code && valueNames.Contains(ora.AttributeValue))
            .CountAsync();
    }

    // Helper method to clear relationship-specific attributes when force deleting
    private async Task ClearOrganizationReferences(OrganizationAttributeType attributeType)
    {
        var valueNames = attributeType.Values.Select(v => v.Name).ToList();
        if (!valueNames.Any()) return;

        // Remove relationship-specific attributes for this type
        var attributesToRemove = await _context.OrganizationRelationshipAttributes
            .Where(ora => ora.AttributeType == attributeType.Code && valueNames.Contains(ora.AttributeValue))
            .ToListAsync();

        _context.OrganizationRelationshipAttributes.RemoveRange(attributesToRemove);
    }

    private async Task InitializeDefaultAttributeData()
    {
        // Check if data already exists
        if (await _context.OrganizationAttributeTypes.AnyAsync())
        {
            return;
        }

        var industryType = new OrganizationAttributeType
        {
            Code = "INDUSTRY",
            Name = "Industry Types",
            Description = "Standard industry classifications",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var regionType = new OrganizationAttributeType
        {
            Code = "REGION",
            Name = "Geographic Regions",
            Description = "Geographic regions and locations",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var sizeType = new OrganizationAttributeType
        {
            Code = "SIZE_CATEGORY",
            Name = "Size Categories",
            Description = "Organization size classifications",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var categoryType = new OrganizationAttributeType
        {
            Code = "BUSINESS_CATEGORY",
            Name = "Business Categories",
            Description = "Business sector categories",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.OrganizationAttributeTypes.AddRange(industryType, regionType, sizeType, categoryType);
        await _context.SaveChangesAsync();

        // Add default values
        var defaultValues = new List<OrganizationAttributeValue>
        {
            // Industries
            new() { AttributeTypeId = industryType.Id, Code = "TECH", Name = "Technology", DisplayOrder = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "MANUFACTURING", Name = "Manufacturing", DisplayOrder = 20, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "HEALTHCARE", Name = "Healthcare", DisplayOrder = 30, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "FINANCE", Name = "Financial Services", DisplayOrder = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "RETAIL", Name = "Retail", DisplayOrder = 50, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "ENERGY", Name = "Energy & Utilities", DisplayOrder = 60, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = industryType.Id, Code = "TRANSPORT", Name = "Transportation & Logistics", DisplayOrder = 70, IsActive = true, CreatedAt = DateTime.UtcNow },

            // Regions
            new() { AttributeTypeId = regionType.Id, Code = "NA", Name = "North America", DisplayOrder = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = regionType.Id, Code = "EU", Name = "Europe", DisplayOrder = 20, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = regionType.Id, Code = "APAC", Name = "Asia-Pacific", DisplayOrder = 30, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = regionType.Id, Code = "LATAM", Name = "Latin America", DisplayOrder = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = regionType.Id, Code = "MEA", Name = "Middle East & Africa", DisplayOrder = 50, IsActive = true, CreatedAt = DateTime.UtcNow },

            // Size Categories
            new() { AttributeTypeId = sizeType.Id, Code = "SMALL", Name = "Small", Description = "1-50 employees", DisplayOrder = 10, IsActive = true, CreatedAt = DateTime.UtcNow, Color = "#28a745" },
            new() { AttributeTypeId = sizeType.Id, Code = "MEDIUM", Name = "Medium", Description = "51-250 employees", DisplayOrder = 20, IsActive = true, CreatedAt = DateTime.UtcNow, Color = "#ffc107" },
            new() { AttributeTypeId = sizeType.Id, Code = "LARGE", Name = "Large", Description = "251-1000 employees", DisplayOrder = 30, IsActive = true, CreatedAt = DateTime.UtcNow, Color = "#fd7e14" },
            new() { AttributeTypeId = sizeType.Id, Code = "ENTERPRISE", Name = "Enterprise", Description = "1000+ employees", DisplayOrder = 40, IsActive = true, CreatedAt = DateTime.UtcNow, Color = "#dc3545" },

            // Business Categories
            new() { AttributeTypeId = categoryType.Id, Code = "SERVICES", Name = "Services", DisplayOrder = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = categoryType.Id, Code = "MANUFACTURING", Name = "Manufacturing", DisplayOrder = 20, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = categoryType.Id, Code = "CONSULTING", Name = "Consulting", DisplayOrder = 30, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = categoryType.Id, Code = "SOFTWARE", Name = "Software Development", DisplayOrder = 40, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { AttributeTypeId = categoryType.Id, Code = "DISTRIBUTION", Name = "Distribution", DisplayOrder = 50, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        _context.OrganizationAttributeValues.AddRange(defaultValues);
        await _context.SaveChangesAsync();
    }

    /* TEMPORARILY DISABLED - Will be updated to work with relationship-specific attributes
    private async Task<int> GetUsageCount(string attributeTypeCode, string valueName)
    {
        switch (attributeTypeCode.ToUpper())
        {
            case "BUSINESS_CATEGORY":
            case "CATEGORY":
                return await _context.Organizations.CountAsync(o => o.Category == valueName);
            case "INDUSTRY":
                return await _context.Organizations.CountAsync(o => o.Industry == valueName);
            case "REGION":
                return await _context.Organizations.CountAsync(o => o.Region == valueName);
            case "SIZE_CATEGORY":
                return await _context.Organizations.CountAsync(o => o.SizeCategory == valueName);
            case "ABC_SEGMENTATION":
                if (Enum.TryParse<ABCSegmentation>(valueName, true, out var abcValue))
                {
                    return await _context.Organizations.CountAsync(o => o.ABCSegmentation == abcValue);
                }
                return 0;
            case "SUPPLIER_CLASSIFICATION":
                if (Enum.TryParse<SupplierClassification>(valueName, true, out var supplierValue))
                {
                    return await _context.Organizations.CountAsync(o => o.SupplierClassification == supplierValue);
                }
                return 0;
            default:
                return 0;
        }
    }

    private async Task<bool> IsValueInUse(string attributeTypeCode, string valueName)
    {
        switch (attributeTypeCode.ToUpper())
        {
            case "BUSINESS_CATEGORY":
            case "CATEGORY":
                return await _context.Organizations.AnyAsync(o => o.Category == valueName);
            case "INDUSTRY":
                return await _context.Organizations.AnyAsync(o => o.Industry == valueName);
            case "REGION":
                return await _context.Organizations.AnyAsync(o => o.Region == valueName);
            case "SIZE_CATEGORY":
                return await _context.Organizations.AnyAsync(o => o.SizeCategory == valueName);
            case "ABC_SEGMENTATION":
                if (Enum.TryParse<ABCSegmentation>(valueName, true, out var abcValue))
                {
                    return await _context.Organizations.AnyAsync(o => o.ABCSegmentation == abcValue);
                }
                return false;
            case "SUPPLIER_CLASSIFICATION":
                if (Enum.TryParse<SupplierClassification>(valueName, true, out var supplierValue))
                {
                    return await _context.Organizations.AnyAsync(o => o.SupplierClassification == supplierValue);
                }
                return false;
            default:
                return false;
        }
    }
    */

    // Placeholder methods for relationship-specific attribute system
    private async Task<int> GetUsageCount(string attributeTypeCode, string valueName)
    {
        // TODO: Implement usage count for relationship-specific attributes
        return await _context.OrganizationRelationshipAttributes
            .CountAsync(ora => ora.AttributeType == attributeTypeCode && ora.AttributeValue == valueName);
    }

    private async Task<bool> IsValueInUse(string attributeTypeCode, string valueName)
    {
        // TODO: Implement usage check for relationship-specific attributes
        return await _context.OrganizationRelationshipAttributes
            .AnyAsync(ora => ora.AttributeType == attributeTypeCode && ora.AttributeValue == valueName);
    }
} 