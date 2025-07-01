using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Controllers;

[Authorize]
public class CampaignController : BaseController
{
    private readonly ApplicationDbContext _context;

    public CampaignController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Campaign
    public async Task<IActionResult> Index()
    {
        IQueryable<Campaign> campaignQuery = _context.Campaigns
            .Include(c => c.Organization)
            .Include(c => c.CreatedBy)
            .Include(c => c.Assignments);

        // Apply user rights filtering
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see campaigns where they have assignments
                campaignQuery = campaignQuery.Where(c => 
                    c.Assignments.Any(a => a.TargetOrganizationId == CurrentOrganizationId));
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see their own campaigns
                campaignQuery = campaignQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
            }
        }
        // Platform Admins see everything (no filter applied)

        var campaigns = await campaignQuery
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(campaigns);
    }

    // GET: Campaign/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        Campaign campaign;

        if (!IsPlatformAdmin && IsCurrentOrgSupplierType)
        {
            // For supplier organizations, load campaign with only their assignments
            campaign = await _context.Campaigns
                .Include(c => c.Organization)
                .Include(c => c.CreatedBy)
                .Include(c => c.Assignments.Where(a => a.TargetOrganizationId == CurrentOrganizationId))
                    .ThenInclude(ca => ca.TargetOrganization)
                .Include(c => c.Assignments.Where(a => a.TargetOrganizationId == CurrentOrganizationId))
                    .ThenInclude(ca => ca.QuestionnaireVersion)
                        .ThenInclude(qv => qv.Questionnaire)
                .Include(c => c.Assignments.Where(a => a.TargetOrganizationId == CurrentOrganizationId))
                    .ThenInclude(ca => ca.LeadResponder)
                .Where(c => c.Id == id && c.Assignments.Any(a => a.TargetOrganizationId == CurrentOrganizationId))
                .FirstOrDefaultAsync();
        }
        else
        {
            // For platform admins and platform organizations, load all assignments
            var campaignQuery = _context.Campaigns
                .Include(c => c.Organization)
                .Include(c => c.CreatedBy)
                .Include(c => c.Assignments)
                    .ThenInclude(ca => ca.TargetOrganization)
                .Include(c => c.Assignments)
                    .ThenInclude(ca => ca.QuestionnaireVersion)
                        .ThenInclude(qv => qv.Questionnaire)
                .Include(c => c.Assignments)
                    .ThenInclude(ca => ca.LeadResponder)
                .Where(c => c.Id == id);

            // Apply organization-level filtering for platform organizations
            if (!IsPlatformAdmin && IsCurrentOrgPlatformType)
            {
                campaignQuery = campaignQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
            }

            campaign = await campaignQuery.FirstOrDefaultAsync();
        }

        if (campaign == null) return NotFound();

        // Set campaign-specific branding context
        await SetBrandingContextAsync(campaignId: id.Value);

        return View(campaign);
    }

    // GET: Campaign/Create
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> Create()
    {
        // Only platform organizations can create campaigns
        if (!IsCurrentOrgPlatformType && !IsPlatformAdmin)
        {
            return Forbid();
        }

        var model = new CampaignCreateViewModel();
        
        // Load available questionnaires for the current organization
        var questionnaires = await _context.Questionnaires
            .Include(q => q.Versions.Where(v => v.IsActive))
            .Where(q => q.IsActive)
            .ToListAsync();

        model.AvailableQuestionnaires = questionnaires.Select(q => new QuestionnaireSelectionViewModel
        {
            QuestionnaireId = q.Id,
            Title = q.Title,
            Description = q.Description,
            Category = q.Category,
            Versions = q.Versions.Select(v => new QuestionnaireVersionViewModel
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                IsActive = v.IsActive
            }).ToList()
        }).ToList();

        // Load available organizations for assignment
        if (IsPlatformAdmin)
        {
            // Platform admins can assign to any organization
            model.AvailableOrganizations = await _context.Organizations
                .Where(o => o.IsActive)
                .Select(o => new OrganizationSelectionViewModel
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description
                }).ToListAsync();
        }
        else
        {
            // Platform organizations can only assign to organizations they have relationships with
            model.AvailableOrganizations = await _context.OrganizationRelationships
                .Where(or => or.PlatformOrganizationId == CurrentOrganizationId && or.IsActive)
                .Select(or => new OrganizationSelectionViewModel
                {
                    Id = or.SupplierOrganization.Id,
                    Name = or.SupplierOrganization.Name,
                    Description = or.SupplierOrganization.Description
                }).ToListAsync();
        }

        return View(model);
    }

    // POST: Campaign/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> Create(CampaignCreateViewModel model)
    {
        // Only platform organizations can create campaigns
        if (!IsCurrentOrgPlatformType && !IsPlatformAdmin)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            var campaign = new Campaign
            {
                Name = model.Name,
                Description = model.Description,
                OrganizationId = CurrentOrganizationId ?? 1, // Use current org or fallback
                Status = model.Status,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Deadline = model.Deadline,
                ReportingPeriodStart = model.ReportingPeriodStart,
                ReportingPeriodEnd = model.ReportingPeriodEnd,
                Instructions = model.Instructions,
                CreatedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(campaign);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Campaign created successfully!";
            return RedirectToAction(nameof(Details), new { id = campaign.Id });
        }

        await LoadCreateViewModelData(model);
        return View(model);
    }

    // GET: Campaign/Edit/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var campaign = await GetCampaignWithAccessCheckAsync(id.Value);
        if (campaign == null) return NotFound();

        var model = new CampaignEditViewModel
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Status = campaign.Status,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Deadline = campaign.Deadline,
            ReportingPeriodStart = campaign.ReportingPeriodStart,
            ReportingPeriodEnd = campaign.ReportingPeriodEnd,
            Instructions = campaign.Instructions
        };

        return View(model);
    }

    // POST: Campaign/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> Edit(int id, CampaignEditViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var campaign = await GetCampaignWithAccessCheckAsync(id);
                if (campaign == null) return NotFound();

                campaign.Name = model.Name;
                campaign.Description = model.Description;
                campaign.StartDate = model.StartDate;
                campaign.EndDate = model.EndDate;
                campaign.Deadline = model.Deadline;
                campaign.ReportingPeriodStart = model.ReportingPeriodStart;
                campaign.ReportingPeriodEnd = model.ReportingPeriodEnd;
                campaign.Instructions = model.Instructions;
                campaign.UpdatedAt = DateTime.UtcNow;

                _context.Update(campaign);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Campaign updated successfully!";
                return RedirectToAction(nameof(Details), new { id = campaign.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CampaignExists(model.Id))
                    return NotFound();
                else
                    throw;
            }
        }

        return View(model);
    }

    // GET: Campaign/Delete/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var campaign = await GetCampaignWithAccessCheckAsync(id.Value);
        if (campaign == null) return NotFound();

        return View(campaign);
    }

    // POST: Campaign/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var campaign = await GetCampaignWithAccessCheckAsync(id);
        if (campaign != null)
        {
            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Campaign deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Campaign/ManageAssignments/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> ManageAssignments(int? id)
    {
        if (id == null) return NotFound();

        var campaign = await GetCampaignWithAccessCheckAsync(id.Value);
        if (campaign == null) return NotFound();

        return View(campaign);
    }

    // GET: Campaign/CreateAssignment/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> CreateAssignment(int? campaignId)
    {
        if (campaignId == null) return NotFound();

        var campaign = await GetCampaignWithAccessCheckAsync(campaignId.Value);
        if (campaign == null) return NotFound();

        var model = new CampaignAssignmentCreateViewModel
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name
        };

        await LoadAssignmentViewModelData(model);
        return View(model);
    }

    // GET: Campaign/BulkAssignment/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> BulkAssignment(int? campaignId)
    {
        if (campaignId == null) return NotFound();

        var campaign = await GetCampaignWithAccessCheckAsync(campaignId.Value);
        if (campaign == null) return NotFound();

        var model = new CampaignBulkAssignmentViewModel
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name
        };

        await LoadBulkAssignmentViewModelData(model);
        return View(model);
    }

    // POST: Campaign/CreateAssignment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> CreateAssignment(CampaignAssignmentCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Validate that the campaign belongs to current organization or user is platform admin
            var campaign = await GetCampaignWithAccessCheckAsync(model.CampaignId);
            if (campaign == null)
            {
                ModelState.AddModelError("", "Campaign not found or access denied.");
                await LoadAssignmentViewModelData(model);
                return View(model);
            }

            // Find the organization relationship and validate it exists (unless platform admin)
            OrganizationRelationship? organizationRelationship = null;
            
            if (!IsPlatformAdmin)
            {
                organizationRelationship = await _context.OrganizationRelationships
                    .FirstOrDefaultAsync(or => or.PlatformOrganizationId == CurrentOrganizationId 
                                            && or.SupplierOrganizationId == model.TargetOrganizationId 
                                            && or.IsActive);

                if (organizationRelationship == null)
                {
                    ModelState.AddModelError("TargetOrganizationId", 
                        "Cannot create assignment. No active relationship exists with the selected organization.");
                    await LoadAssignmentViewModelData(model);
                    return View(model);
                }
            }
            else
            {
                // Platform admin: Find any active relationship where the target org is the supplier
                organizationRelationship = await _context.OrganizationRelationships
                    .FirstOrDefaultAsync(or => or.SupplierOrganizationId == model.TargetOrganizationId 
                                            && or.IsActive);
            }

            var assignment = new CampaignAssignment
            {
                CampaignId = model.CampaignId,
                TargetOrganizationId = model.TargetOrganizationId,
                OrganizationRelationshipId = organizationRelationship?.Id, // SET THE RELATIONSHIP ID
                QuestionnaireVersionId = model.QuestionnaireVersionId,
                LeadResponderId = string.IsNullOrEmpty(model.LeadResponderId) ? null : model.LeadResponderId,
                Status = AssignmentStatus.NotStarted,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Campaign assignment created successfully!";
            return RedirectToAction(nameof(ManageAssignments), new { id = model.CampaignId });
        }

        await LoadAssignmentViewModelData(model);
        return View(model);
    }

    // POST: Campaign/BulkAssignment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> BulkAssignment(CampaignBulkAssignmentViewModel model)
    {
        if (ModelState.IsValid)
        {
            var campaign = await GetCampaignWithAccessCheckAsync(model.CampaignId);
            if (campaign == null)
            {
                ModelState.AddModelError("", "Campaign not found or access denied.");
                await LoadBulkAssignmentViewModelData(model);
                return View(model);
            }

            if (!model.SelectedOrganizationIds.Any())
            {
                ModelState.AddModelError("SelectedOrganizationIds", "Please select at least one organization.");
                await LoadBulkAssignmentViewModelData(model);
                return View(model);
            }

            var createdCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            foreach (var orgId in model.SelectedOrganizationIds)
            {
                try
                {
                    // Check if assignment already exists
                    var existingAssignment = await _context.CampaignAssignments
                        .AnyAsync(ca => ca.CampaignId == model.CampaignId && 
                                       ca.TargetOrganizationId == orgId && 
                                       ca.QuestionnaireVersionId == model.QuestionnaireVersionId);

                    if (existingAssignment)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Find the organization relationship and validate it exists (unless platform admin)
                    OrganizationRelationship? organizationRelationship = null;
                    
                    if (!IsPlatformAdmin)
                    {
                        organizationRelationship = await _context.OrganizationRelationships
                            .FirstOrDefaultAsync(or => or.PlatformOrganizationId == CurrentOrganizationId 
                                                    && or.SupplierOrganizationId == orgId 
                                                    && or.IsActive);

                        if (organizationRelationship == null)
                        {
                            var orgName = await _context.Organizations
                                .Where(o => o.Id == orgId)
                                .Select(o => o.Name)
                                .FirstOrDefaultAsync();
                            errors.Add($"No active relationship with {orgName}");
                            continue;
                        }
                    }
                    else
                    {
                        // Platform admin: Find any active relationship where the target org is the supplier
                        organizationRelationship = await _context.OrganizationRelationships
                            .FirstOrDefaultAsync(or => or.SupplierOrganizationId == orgId 
                                                    && or.IsActive);
                    }

                    var assignment = new CampaignAssignment
                    {
                        CampaignId = model.CampaignId,
                        TargetOrganizationId = orgId,
                        OrganizationRelationshipId = organizationRelationship?.Id, // SET THE RELATIONSHIP ID
                        QuestionnaireVersionId = model.QuestionnaireVersionId,
                        Status = AssignmentStatus.NotStarted,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Add(assignment);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error creating assignment: {ex.Message}");
                }
            }

            if (createdCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            // Build success/error messages
            var messages = new List<string>();
            if (createdCount > 0)
                messages.Add($"{createdCount} assignments created successfully!");
            if (skippedCount > 0)
                messages.Add($"{skippedCount} assignments already existed and were skipped.");
            if (errors.Any())
                messages.AddRange(errors);

            if (createdCount > 0)
            {
                TempData["Success"] = string.Join(" ", messages.Where(m => !m.Contains("Error")));
                if (errors.Any())
                    TempData["Warning"] = string.Join(" ", errors);
            }
            else
            {
                TempData["Error"] = string.Join(" ", messages);
            }

            return RedirectToAction(nameof(ManageAssignments), new { id = model.CampaignId });
        }

        await LoadBulkAssignmentViewModelData(model);
        return View(model);
    }

    private async Task LoadCreateViewModelData(CampaignCreateViewModel model)
    {
        var questionnaires = await _context.Questionnaires
            .Include(q => q.Versions.Where(v => v.IsActive))
            .Where(q => q.IsActive)
            .ToListAsync();

        model.AvailableQuestionnaires = questionnaires.Select(q => new QuestionnaireSelectionViewModel
        {
            QuestionnaireId = q.Id,
            Title = q.Title,
            Description = q.Description,
            Category = q.Category,
            Versions = q.Versions.Select(v => new QuestionnaireVersionViewModel
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                IsActive = v.IsActive
            }).ToList()
        }).ToList();

        if (IsPlatformAdmin)
        {
            model.AvailableOrganizations = await _context.Organizations
                .Where(o => o.IsActive)
                .Select(o => new OrganizationSelectionViewModel
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description
                }).ToListAsync();
        }
        else
        {
            // Platform organizations can only assign to organizations they have relationships with
            model.AvailableOrganizations = await _context.OrganizationRelationships
                .Where(or => or.PlatformOrganizationId == CurrentOrganizationId && or.IsActive)
                .Select(or => new OrganizationSelectionViewModel
                {
                    Id = or.SupplierOrganization.Id,
                    Name = or.SupplierOrganization.Name,
                    Description = or.SupplierOrganization.Description
                }).ToListAsync();
        }
    }

    private async Task LoadAssignmentViewModelData(CampaignAssignmentCreateViewModel model)
    {
        // Load questionnaire versions
        var questionnaires = await _context.Questionnaires
            .Include(q => q.Versions.Where(v => v.IsActive))
            .Where(q => q.IsActive)
            .ToListAsync();

        model.AvailableQuestionnaireVersions = questionnaires
            .SelectMany(q => q.Versions.Select(v => new QuestionnaireVersionSelectionViewModel
            {
                Id = v.Id,
                QuestionnaireTitle = q.Title,
                VersionNumber = v.VersionNumber,
                DisplayText = $"{q.Title} (v{v.VersionNumber})"
            })).ToList();

        // Load organizations with relationship validation
        if (IsPlatformAdmin)
        {
            model.AvailableOrganizations = await _context.Organizations
                .Where(o => o.IsActive)
                .Select(o => new OrganizationSelectionViewModel
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description
                }).ToListAsync();
        }
        else
        {
            // Only organizations with active relationships
            model.AvailableOrganizations = await _context.OrganizationRelationships
                .Where(or => or.PlatformOrganizationId == CurrentOrganizationId && or.IsActive)
                .Select(or => new OrganizationSelectionViewModel
                {
                    Id = or.SupplierOrganization.Id,
                    Name = or.SupplierOrganization.Name,
                    Description = or.SupplierOrganization.Description
                }).ToListAsync();
        }

        // Load users from selected organization (if any)
        if (model.TargetOrganizationId > 0)
        {
            model.AvailableLeadResponders = await _context.Users
                .Where(u => u.OrganizationId == model.TargetOrganizationId && u.IsActive)
                .Select(u => new UserSelectionViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email
                }).ToListAsync();
        }
    }

    private async Task LoadBulkAssignmentViewModelData(CampaignBulkAssignmentViewModel model)
    {
        // Load questionnaire versions
        var questionnaires = await _context.Questionnaires
            .Include(q => q.Versions.Where(v => v.IsActive))
            .Where(q => q.IsActive)
            .ToListAsync();

        model.AvailableQuestionnaireVersions = questionnaires
            .SelectMany(q => q.Versions.Select(v => new QuestionnaireVersionSelectionViewModel
            {
                Id = v.Id,
                QuestionnaireTitle = q.Title,
                VersionNumber = v.VersionNumber,
                DisplayText = $"{q.Title} (v{v.VersionNumber})"
            })).ToList();

        // Get existing assignments for this campaign
        var existingAssignments = await _context.CampaignAssignments
            .Where(ca => ca.CampaignId == model.CampaignId)
            .Select(ca => ca.TargetOrganizationId)
            .ToListAsync();

        // Load organizations with relationship-specific attributes
        List<OrganizationSelectionWithDetailsViewModel> availableOrganizations;
        

        
        if (IsPlatformAdmin)
        {
            // Platform admin sees all organizations
            var allOrgs = await _context.Organizations
                .Where(o => o.IsActive)
                .Include(o => o.Users)
                .ToListAsync();



            availableOrganizations = new List<OrganizationSelectionWithDetailsViewModel>();

            foreach (var org in allOrgs)
            {
                List<OrganizationAttributeViewModel> attributes = new List<OrganizationAttributeViewModel>();
                OrganizationRelationship? relationship = null;

                if (org.Type == OrganizationType.SupplierOrganization)
                {
                    // For suppliers, get attributes from relationships where they are the supplier
                    relationship = await _context.OrganizationRelationships
                        .Include(r => r.Attributes.Where(a => a.IsActive))
                        .FirstOrDefaultAsync(r => r.SupplierOrganizationId == org.Id && r.IsActive);
                }
                // Platform organizations don't get attributes from relationships
                // Attributes in relationships describe the SUPPLIER, not the platform organization

                if (relationship?.Attributes != null)
                {
                    attributes = relationship.Attributes.Select(a => new OrganizationAttributeViewModel
                    {
                        AttributeType = a.AttributeType,
                        Value = a.AttributeValue
                    }).ToList();
                }



                availableOrganizations.Add(new OrganizationSelectionWithDetailsViewModel
                {
                    Id = org.Id,
                    Name = org.Name,
                    Description = org.Description,
                    Type = org.TypeDisplayName,
                    UsersCount = org.Users.Count(u => u.IsActive),
                    AlreadyAssigned = existingAssignments.Contains(org.Id),
                    HasActiveRelationship = relationship != null,
                    RelationshipType = relationship?.RelationshipType,
                    Attributes = attributes
                });
            }
        }
        else
        {
            // For platform orgs, load organizations with their specific relationship attributes
            var allRelationships = await _context.OrganizationRelationships
                .Where(or => or.PlatformOrganizationId == CurrentOrganizationId)
                .Include(or => or.PlatformOrganization)
                .Include(or => or.SupplierOrganization)
                    .ThenInclude(o => o.Users)
                .Include(or => or.Attributes)
                .ToListAsync();

            var activeRelationships = allRelationships.Where(or => or.IsActive).ToList();

            availableOrganizations = activeRelationships.Select(rel => 
            {
                var org = rel.SupplierOrganization;
                var attributes = rel.Attributes?.Where(a => a.IsActive).Select(a => new OrganizationAttributeViewModel
                {
                    AttributeType = a.AttributeType,
                    Value = a.AttributeValue
                }).ToList() ?? new List<OrganizationAttributeViewModel>();

                return new OrganizationSelectionWithDetailsViewModel
                {
                    Id = org.Id,
                    Name = org.Name,
                    Description = org.Description,
                    Type = org.TypeDisplayName,
                    UsersCount = org.Users?.Count(u => u.IsActive) ?? 0,
                    AlreadyAssigned = existingAssignments.Contains(org.Id),
                    HasActiveRelationship = true,
                    RelationshipType = rel.RelationshipType,
                    Attributes = attributes
                };
            }).ToList();
        }

        model.AvailableOrganizations = availableOrganizations;

        // Apply filters if provided
        if (!string.IsNullOrEmpty(model.NameFilter))
        {
            model.AvailableOrganizations = model.AvailableOrganizations
                .Where(o => o.Name.Contains(model.NameFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrEmpty(model.AttributeFilter))
        {
            model.AvailableOrganizations = model.AvailableOrganizations
                .Where(o => o.Attributes.Any(a => a.Value.Contains(model.AttributeFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    // Helper method to get campaign with proper access control
    private async Task<Campaign?> GetCampaignWithAccessCheckAsync(int campaignId)
    {
        var campaignQuery = _context.Campaigns
            .Include(c => c.Organization)
            .Include(c => c.CreatedBy)
            .Include(c => c.Assignments)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(c => c.Assignments)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(c => c.Assignments)
                .ThenInclude(ca => ca.LeadResponder)
            .Where(c => c.Id == campaignId);

        // Apply user rights filtering
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see campaigns where they have assignments
                campaignQuery = campaignQuery.Where(c => 
                    c.Assignments.Any(a => a.TargetOrganizationId == CurrentOrganizationId));
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see their own campaigns
                campaignQuery = campaignQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
            }
        }

        return await campaignQuery.FirstOrDefaultAsync();
    }

    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> ViewAssignment(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
            .Include(ca => ca.LeadResponder)
            .Include(ca => ca.Responses)
                .ThenInclude(r => r.Question)
            .FirstOrDefaultAsync(ca => ca.Id == id);

        if (assignment == null) return NotFound();

        // Check access rights
        if (!IsPlatformAdmin && !IsCurrentOrgPlatformType && assignment.TargetOrganizationId != CurrentOrganizationId)
        {
            return Forbid();
        }

        return View(assignment);
    }

    // GET: Campaign/EditAssignment/5
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> EditAssignment(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
            .Include(ca => ca.LeadResponder)
            .FirstOrDefaultAsync(ca => ca.Id == id);

        if (assignment == null) return NotFound();

        // Check access rights
        if (!IsPlatformAdmin && !IsCurrentOrgPlatformType && assignment.TargetOrganizationId != CurrentOrganizationId)
        {
            return Forbid();
        }

        var model = new CampaignAssignmentEditViewModel
        {
            Id = assignment.Id,
            CampaignId = assignment.CampaignId,
            CampaignName = assignment.Campaign.Name,
            TargetOrganizationId = assignment.TargetOrganizationId,
            QuestionnaireVersionId = assignment.QuestionnaireVersionId,
            LeadResponderId = assignment.LeadResponderId,
            Status = assignment.Status,
            ReviewNotes = assignment.ReviewNotes
        };

        await LoadEditAssignmentViewModelData(model);
        return View(model);
    }

    // POST: Campaign/EditAssignment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> EditAssignment(int id, CampaignAssignmentEditViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var assignment = await _context.CampaignAssignments.FindAsync(id);
                if (assignment == null) return NotFound();

                // Check access rights
                if (!IsPlatformAdmin && !IsCurrentOrgPlatformType && assignment.TargetOrganizationId != CurrentOrganizationId)
                {
                    return Forbid();
                }

                assignment.TargetOrganizationId = model.TargetOrganizationId;
                assignment.QuestionnaireVersionId = model.QuestionnaireVersionId;
                assignment.LeadResponderId = string.IsNullOrEmpty(model.LeadResponderId) ? null : model.LeadResponderId;
                assignment.Status = model.Status;
                assignment.ReviewNotes = model.ReviewNotes;
                assignment.UpdatedAt = DateTime.UtcNow;

                _context.Update(assignment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Assignment updated successfully!";
                return RedirectToAction(nameof(ManageAssignments), new { id = model.CampaignId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(model.Id))
                    return NotFound();
                throw;
            }
        }

        await LoadEditAssignmentViewModelData(model);
        return View(model);
    }

    // POST: Campaign/DeleteAssignment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await _context.CampaignAssignments.FindAsync(id);
        if (assignment != null)
        {
            // Check access rights
            if (!IsPlatformAdmin && !IsCurrentOrgPlatformType)
            {
                return Forbid();
            }

            var campaignId = assignment.CampaignId;
            _context.CampaignAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment deleted successfully!";
            return RedirectToAction(nameof(ManageAssignments), new { id = campaignId });
        }

        return NotFound();
    }

    private async Task LoadEditAssignmentViewModelData(CampaignAssignmentEditViewModel model)
    {
        // Load questionnaire versions
        var questionnaires = await _context.Questionnaires
            .Include(q => q.Versions.Where(v => v.IsActive))
            .Where(q => q.IsActive)
            .ToListAsync();

        model.AvailableQuestionnaireVersions = questionnaires
            .SelectMany(q => q.Versions.Select(v => new QuestionnaireVersionSelectionViewModel
            {
                Id = v.Id,
                QuestionnaireTitle = q.Title,
                VersionNumber = v.VersionNumber,
                DisplayText = $"{q.Title} (v{v.VersionNumber})"
            })).ToList();

        // Load organizations with relationship validation
        if (IsPlatformAdmin)
        {
            model.AvailableOrganizations = await _context.Organizations
                .Where(o => o.IsActive)
                .Select(o => new OrganizationSelectionViewModel
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description
                }).ToListAsync();
        }
        else
        {
            // Only organizations with active relationships
            model.AvailableOrganizations = await _context.OrganizationRelationships
                .Where(or => or.PlatformOrganizationId == CurrentOrganizationId && or.IsActive)
                .Select(or => new OrganizationSelectionViewModel
                {
                    Id = or.SupplierOrganization.Id,
                    Name = or.SupplierOrganization.Name,
                    Description = or.SupplierOrganization.Description
                }).ToListAsync();
        }

        // Load users from target organization
        if (model.TargetOrganizationId > 0)
        {
            model.AvailableLeadResponders = await _context.Users
                .Where(u => u.OrganizationId == model.TargetOrganizationId && u.IsActive)
                .Select(u => new UserSelectionViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email
                }).ToListAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUsersByOrganization(int organizationId)
    {
        var users = await _context.Users
            .Where(u => u.OrganizationId == organizationId && u.IsActive)
            .Select(u => new
            {
                id = u.Id,
                displayName = $"{u.FirstName} {u.LastName}",
                email = u.Email
            })
            .ToListAsync();

        return Json(users);
    }

    private bool CampaignExists(int id)
    {
        return _context.Campaigns.Any(e => e.Id == id);
    }

    private bool AssignmentExists(int id)
    {
        return _context.CampaignAssignments.Any(e => e.Id == id);
    }
} 