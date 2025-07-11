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

    // GET: Campaign/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var dashboardViewModel = await BuildCampaignDashboardAsync();
        return View(dashboardViewModel);
    }

    // GET: Campaign
    public async Task<IActionResult> Index()
    {
        IQueryable<Campaign> campaignQuery;
        
        // For Platform Admins, bypass global query filters to see all campaigns
        if (IsPlatformAdmin)
        {
            campaignQuery = _context.Campaigns
                .IgnoreQueryFilters()
                .Include(c => c.Organization)
                .Include(c => c.CreatedBy)
                .Include(c => c.Assignments);
        }
        else
        {
            campaignQuery = _context.Campaigns
                .Include(c => c.Organization)
                .Include(c => c.CreatedBy)
                .Include(c => c.Assignments);
        }

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
                .Where(c => c.Id == id!.Value && c.Assignments.Any(a => a.TargetOrganizationId == CurrentOrganizationId))
                .FirstOrDefaultAsync();
        }
        else
        {
            // For platform admins and platform organizations, load all assignments
            IQueryable<Campaign> campaignQuery = _context.Campaigns;
            
            // Bypass global query filters for platform admins
            if (IsPlatformAdmin)
            {
                campaignQuery = campaignQuery.IgnoreQueryFilters();
            }
            
            campaignQuery = campaignQuery
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
        if (id.HasValue)
        {
            await SetBrandingContextAsync(campaignId: id.Value);
        }

        // Pass additional data to view
        ViewBag.CanCloseCampaign = CanCloseCampaign(campaign);

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
                OrganizationId = CurrentOrganizationId ?? 1, // Use current org or fallback (ensure 1 is a valid org ID)
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
                campaign.Status = model.Status;
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
            try
            {
                // Get all campaign assignment IDs for this campaign (ignore global filters)
                var assignmentIds = await _context.CampaignAssignments
                    .IgnoreQueryFilters()
                    .Where(ca => ca.CampaignId == id)
                    .Select(ca => ca.Id)
                    .ToListAsync();

                if (assignmentIds.Any())
                {
                    // Delete all related data in the correct order to avoid foreign key constraints
                    
                    // 1. Delete responses and related data
                    var responseIds = await _context.Responses
                        .IgnoreQueryFilters()
                        .Where(r => assignmentIds.Contains(r.CampaignAssignmentId))
                        .Select(r => r.Id)
                        .ToListAsync();

                    if (responseIds.Any())
                    {
                        // Delete file uploads
                        var fileUploads = await _context.FileUploads
                            .IgnoreQueryFilters()
                            .Where(f => responseIds.Contains(f.ResponseId))
                            .ToListAsync();
                        _context.FileUploads.RemoveRange(fileUploads);

                        // Delete response changes
                        var responseChanges = await _context.ResponseChanges
                            .IgnoreQueryFilters()
                            .Where(rc => responseIds.Contains(rc.ResponseId))
                            .ToListAsync();
                        _context.ResponseChanges.RemoveRange(responseChanges);

                        // Delete response overrides
                        var responseOverrides = await _context.ResponseOverrides
                            .IgnoreQueryFilters()
                            .Where(ro => responseIds.Contains(ro.ResponseId))
                            .ToListAsync();
                        _context.ResponseOverrides.RemoveRange(responseOverrides);

                        // Delete review comments
                        var reviewComments = await _context.ReviewComments
                            .IgnoreQueryFilters()
                            .Where(rc => responseIds.Contains(rc.ResponseId))
                            .ToListAsync();
                        _context.ReviewComments.RemoveRange(reviewComments);

                        // Delete response workflows
                        var responseWorkflows = await _context.ResponseWorkflows
                            .IgnoreQueryFilters()
                            .Where(rw => responseIds.Contains(rw.ResponseId))
                            .ToListAsync();
                        _context.ResponseWorkflows.RemoveRange(responseWorkflows);

                        // Clear SourceResponseId references to prevent foreign key conflicts
                        // This handles cases where responses reference other responses within the same campaign
                        await _context.Responses
                            .IgnoreQueryFilters()
                            .Where(r => responseIds.Contains(r.SourceResponseId ?? 0))
                            .ExecuteUpdateAsync(r => r.SetProperty(x => x.SourceResponseId, (int?)null));

                        // Delete responses
                        var responses = await _context.Responses
                            .IgnoreQueryFilters()
                            .Where(r => responseIds.Contains(r.Id))
                            .ToListAsync();
                        _context.Responses.RemoveRange(responses);
                    }

                    // 2. Delete delegations
                    var delegations = await _context.Delegations
                        .IgnoreQueryFilters()
                        .Where(d => assignmentIds.Contains(d.CampaignAssignmentId))
                        .ToListAsync();
                    _context.Delegations.RemoveRange(delegations);

                    // 3. Delete question assignments
                    var questionAssignments = await _context.QuestionAssignments
                        .IgnoreQueryFilters()
                        .Where(qa => assignmentIds.Contains(qa.CampaignAssignmentId))
                        .ToListAsync();
                    _context.QuestionAssignments.RemoveRange(questionAssignments);

                    // 4. Delete question assignment changes
                    var questionAssignmentChanges = await _context.QuestionAssignmentChanges
                        .IgnoreQueryFilters()
                        .Where(qac => assignmentIds.Contains(qac.CampaignAssignmentId))
                        .ToListAsync();
                    _context.QuestionAssignmentChanges.RemoveRange(questionAssignmentChanges);

                    // 5. Delete review assignments and audit logs (these have cascade delete from campaign assignments)
                    var reviewAssignments = await _context.ReviewAssignments
                        .IgnoreQueryFilters()
                        .Where(ra => assignmentIds.Contains(ra.CampaignAssignmentId))
                        .ToListAsync();
                    _context.ReviewAssignments.RemoveRange(reviewAssignments);

                    var reviewAuditLogs = await _context.ReviewAuditLogs
                        .IgnoreQueryFilters()
                        .Where(ral => assignmentIds.Contains(ral.CampaignAssignmentId))
                        .ToListAsync();
                    _context.ReviewAuditLogs.RemoveRange(reviewAuditLogs);

                    // 6. Delete campaign assignments
                    var campaignAssignments = await _context.CampaignAssignments
                        .IgnoreQueryFilters()
                        .Where(ca => assignmentIds.Contains(ca.Id))
                        .ToListAsync();
                    _context.CampaignAssignments.RemoveRange(campaignAssignments);
                }

                // 7. Finally delete the campaign
                _context.Campaigns.Remove(campaign);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Campaign and all related data deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting campaign: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
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
                    Email = u.Email ?? ""
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
                    Email = u.Email ?? ""
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

    // POST: Campaign/CloseCampaign/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "OrgAdminOrHigher")]
    public async Task<IActionResult> CloseCampaign(int id)
    {
        var campaign = await GetCampaignWithAccessCheckAsync(id);
        if (campaign == null) return NotFound();

        // Check if all assignments are submitted or approved
        var allSubmitted = campaign.Assignments.All(a => 
            a.Status == AssignmentStatus.Submitted || 
            a.Status == AssignmentStatus.Approved);

        if (!allSubmitted)
        {
            TempData["Error"] = "Cannot close campaign - not all assignments have been submitted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Close the campaign
        campaign.Status = CampaignStatus.Completed;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Campaign closed successfully!";
        return RedirectToAction(nameof(Dashboard));
    }

    // Helper method to check if campaign can be closed
    private bool CanCloseCampaign(Campaign campaign)
    {
        return campaign.Status == CampaignStatus.Active && 
               campaign.Assignments.Any() && 
               campaign.Assignments.All(a => 
                   a.Status == AssignmentStatus.Submitted || 
                   a.Status == AssignmentStatus.Approved);
    }

    #region Campaign Dashboard Methods (Phase 5.1)

    private async Task<CampaignDashboardViewModel> BuildCampaignDashboardAsync()
    {
        IQueryable<Campaign> campaignQuery;
        IQueryable<CampaignAssignment> assignmentQuery;

        // Apply access control filters based on user role
        if (IsPlatformAdmin)
        {
            campaignQuery = _context.Campaigns.IgnoreQueryFilters();
            assignmentQuery = _context.CampaignAssignments.IgnoreQueryFilters();
        }
        else
        {
            campaignQuery = _context.Campaigns;
            assignmentQuery = _context.CampaignAssignments;

            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see campaigns where they have assignments
                campaignQuery = campaignQuery.Where(c => 
                    c.Assignments.Any(a => a.TargetOrganizationId == CurrentOrganizationId));
                assignmentQuery = assignmentQuery.Where(a => a.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see their own campaigns
                campaignQuery = campaignQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
                assignmentQuery = assignmentQuery.Where(a => a.Campaign.OrganizationId == CurrentOrganizationId);
            }
        }

        // Load campaigns with necessary includes
        var allCampaigns = await campaignQuery
            .Include(c => c.Organization)
            .Include(c => c.CreatedBy)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.TargetOrganization)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.Responses)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
                        .ThenInclude(q => q.Questions)
            .ToListAsync();

        // Load all assignments with response data for detailed metrics
        var allAssignments = await assignmentQuery
            .Include(a => a.Campaign)
                .ThenInclude(c => c.Organization)
            .Include(a => a.TargetOrganization)
            .Include(a => a.Responses)
            .Include(a => a.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .ToListAsync();

        // Build dashboard summary
        var summary = BuildDashboardSummary(allCampaigns, allAssignments);

        // Get open campaigns (Draft, Active, Paused - excluding Completed and Cancelled)
        var activeCampaigns = allCampaigns
            .Where(c => c.Status == CampaignStatus.Draft || c.Status == CampaignStatus.Active || c.Status == CampaignStatus.Paused)
            .Select(c => BuildCampaignDashboardItem(c, allAssignments.Where(a => a.CampaignId == c.Id).ToList()))
            .OrderBy(c => c.Status == CampaignStatus.Draft ? 0 : c.Status == CampaignStatus.Active ? 1 : 2) // Draft first, then Active, then Paused
            .ThenBy(c => c.Deadline ?? DateTime.MaxValue)
            .ToList();

        // Get recent campaigns (completed or active, ordered by creation date)
        var recentCampaigns = allCampaigns
            .Where(c => c.Status != CampaignStatus.Draft)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => BuildCampaignDashboardItem(c, allAssignments.Where(a => a.CampaignId == c.Id).ToList()))
            .ToList();

        // Build progress metrics
        var progressMetrics = await BuildProgressMetricsAsync(allAssignments);

        // Build campaign performance data
        var campaignPerformance = allCampaigns
            .Where(c => c.Status == CampaignStatus.Active)
            .Select(c => BuildCampaignPerformance(c, allAssignments.Where(a => a.CampaignId == c.Id).ToList()))
            .OrderByDescending(c => c.CompletionRate)
            .ToList();

        // Build assignment-focused sections
        var companyBreakdown = await BuildCompanyBreakdownAsync(allAssignments);
        var responderBreakdown = await BuildResponderBreakdownAsync(allAssignments);
        var statusDistribution = BuildStatusDistribution(allAssignments);

        return new CampaignDashboardViewModel
        {
            Summary = summary,
            ActiveCampaigns = activeCampaigns,
            RecentCampaigns = recentCampaigns,
            ProgressMetrics = progressMetrics,
            CampaignPerformance = campaignPerformance,
            CompanyBreakdown = companyBreakdown,
            ResponderBreakdown = responderBreakdown,
            StatusDistribution = statusDistribution
        };
    }

    private CampaignDashboardSummaryViewModel BuildDashboardSummary(List<Campaign> campaigns, List<CampaignAssignment> assignments)
    {
        var now = DateTime.Now;

        return new CampaignDashboardSummaryViewModel
        {
            TotalActiveCampaigns = campaigns.Count(c => c.Status == CampaignStatus.Active),
            TotalCompletedCampaigns = campaigns.Count(c => c.Status == CampaignStatus.Completed),
            TotalDraftCampaigns = campaigns.Count(c => c.Status == CampaignStatus.Draft),
            TotalPausedCampaigns = campaigns.Count(c => c.Status == CampaignStatus.Paused),
            TotalAssignments = assignments.Count,
            TotalActiveAssignments = assignments.Count(a => a.Status == AssignmentStatus.InProgress || a.Status == AssignmentStatus.NotStarted),
            TotalCompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved),
            TotalOverdueAssignments = assignments.Count(a => 
                a.Campaign.Deadline.HasValue && 
                a.Campaign.Deadline.Value < now && 
                a.Status != AssignmentStatus.Approved)
        };
    }

    private CampaignDashboardItemViewModel BuildCampaignDashboardItem(Campaign campaign, List<CampaignAssignment> assignments)
    {
        var now = DateTime.Now;

        // Calculate response time analytics
        var completedAssignments = assignments.Where(a => a.Status == AssignmentStatus.Approved).ToList();
        var responseTimeHours = CalculateAverageResponseTime(assignments);

        var item = new CampaignDashboardItemViewModel
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description ?? string.Empty,
            Status = campaign.Status,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Deadline = campaign.Deadline,
            CreatedAt = campaign.CreatedAt,
            OrganizationName = campaign.Organization?.Name ?? "Unknown",
            CreatedByName = campaign.CreatedBy != null ? $"{campaign.CreatedBy.FirstName} {campaign.CreatedBy.LastName}" : "Unknown",
            TotalAssignments = assignments.Count,
            CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved),
            InProgressAssignments = assignments.Count(a => a.Status == AssignmentStatus.InProgress),
            NotStartedAssignments = assignments.Count(a => a.Status == AssignmentStatus.NotStarted),
            OverdueAssignments = assignments.Count(a => 
                campaign.Deadline.HasValue && 
                campaign.Deadline.Value < now && 
                a.Status != AssignmentStatus.Submitted && a.Status != AssignmentStatus.Approved),
            AverageResponseTimeHours = responseTimeHours,
            FirstResponseAt = GetFirstResponseDate(assignments),
            LastResponseAt = GetLastResponseDate(assignments)
        };

        return item;
    }

    private async Task<CampaignProgressMetricsViewModel> BuildProgressMetricsAsync(List<CampaignAssignment> assignments)
    {
        var totalQuestions = assignments.Sum(a => a.QuestionnaireVersion?.Questionnaire?.Questions?.Count ?? 0);
        var answeredQuestions = assignments.Sum(a => a.Responses?.Count ?? 0);
        var activeAssignments = assignments.Where(a => a.Status == AssignmentStatus.InProgress).ToList();

        // Calculate daily progress for the last 30 days
        var dailyProgress = await BuildDailyProgressAsync(assignments);
        
        // Calculate weekly progress for the last 12 weeks
        var weeklyProgress = BuildWeeklyProgress(assignments);

        return new CampaignProgressMetricsViewModel
        {
            OverallCompletionRate = assignments.Count > 0 ? 
                (decimal)assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved) / assignments.Count * 100 : 0,
            OverallResponseRate = totalQuestions > 0 ? (decimal)answeredQuestions / totalQuestions * 100 : 0,
            AverageResponseTimeHours = CalculateAverageResponseTime(assignments) ?? 0,
            TotalQuestionsAnswered = answeredQuestions,
            TotalQuestionsAssigned = totalQuestions,
            ActiveRespondents = activeAssignments.Select(a => a.LeadResponderId).Distinct().Count(),
            TotalRespondents = assignments.Select(a => a.LeadResponderId).Where(id => !string.IsNullOrEmpty(id)).Distinct().Count(),
            DailyProgress = dailyProgress,
            WeeklyProgress = weeklyProgress
        };
    }

    private CampaignPerformanceViewModel BuildCampaignPerformance(Campaign campaign, List<CampaignAssignment> assignments)
    {
        var completionRate = assignments.Count > 0 ? 
            (decimal)assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved) / assignments.Count * 100 : 0;
        var averageResponseTime = CalculateAverageResponseTime(assignments) ?? 0;
        var overdueAssignments = assignments.Count(a => 
            campaign.Deadline.HasValue && 
            campaign.Deadline.Value < DateTime.Now && 
            a.Status != AssignmentStatus.Submitted && a.Status != AssignmentStatus.Approved);

        // Determine performance indicator
        var (indicator, color) = GetPerformanceIndicator(completionRate, overdueAssignments, assignments.Count, campaign.Deadline);

        return new CampaignPerformanceViewModel
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            CompletionRate = completionRate,
            AverageResponseTimeHours = averageResponseTime,
            TotalAssignments = assignments.Count,
            CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved),
            OverdueAssignments = overdueAssignments,
            Deadline = campaign.Deadline,
            IsOnTrack = overdueAssignments == 0 && completionRate >= 50,
            PerformanceIndicator = indicator,
            PerformanceColor = color
        };
    }

    private double? CalculateAverageResponseTime(List<CampaignAssignment> assignments)
    {
        var completedAssignments = assignments.Where(a => a.StartedAt.HasValue && a.SubmittedAt.HasValue).ToList();
        
        if (!completedAssignments.Any())
            return null;

        var totalHours = completedAssignments.Sum(a => (a.SubmittedAt!.Value - a.StartedAt!.Value).TotalHours);
        return totalHours / completedAssignments.Count;
    }

    private DateTime? GetFirstResponseDate(List<CampaignAssignment> assignments)
    {
        return assignments
            .SelectMany(a => a.Responses ?? new List<Response>())
            .Where(r => r.CreatedAt != default)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefault()?.CreatedAt;
    }

    private DateTime? GetLastResponseDate(List<CampaignAssignment> assignments)
    {
        return assignments
            .SelectMany(a => a.Responses ?? new List<Response>())
            .Where(r => r.UpdatedAt.HasValue)
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefault()?.UpdatedAt ?? 
            assignments
            .SelectMany(a => a.Responses ?? new List<Response>())
            .Where(r => r.CreatedAt != default)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault()?.CreatedAt;
    }

    private Task<List<DailyProgressViewModel>> BuildDailyProgressAsync(List<CampaignAssignment> assignments)
    {
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => DateTime.Now.Date.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var dailyProgress = new List<DailyProgressViewModel>();

        foreach (var date in last30Days)
        {
            var nextDay = date.AddDays(1);
            
            var responsesOnDate = assignments
                .SelectMany(a => a.Responses ?? new List<Response>())
                .Count(r => r.CreatedAt.Date == date);

            var completedOnDate = assignments
                .Count(a => a.SubmittedAt.HasValue && a.SubmittedAt.Value.Date == date);

            var newAssignmentsOnDate = assignments
                .Count(a => a.CreatedAt.Date == date);

            dailyProgress.Add(new DailyProgressViewModel
            {
                Date = date,
                ResponsesReceived = responsesOnDate,
                AssignmentsCompleted = completedOnDate,
                NewAssignments = newAssignmentsOnDate
            });
        }

        return Task.FromResult(dailyProgress);
    }

    private List<WeeklyProgressViewModel> BuildWeeklyProgress(List<CampaignAssignment> assignments)
    {
        var last12Weeks = Enumerable.Range(0, 12)
            .Select(i => DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek + (int)DayOfWeek.Monday - (i * 7)))
            .OrderBy(d => d)
            .ToList();

        var weeklyProgress = new List<WeeklyProgressViewModel>();

        foreach (var weekStart in last12Weeks)
        {
            var weekEnd = weekStart.AddDays(7);
            
            var responsesInWeek = assignments
                .SelectMany(a => a.Responses ?? new List<Response>())
                .Count(r => r.CreatedAt.Date >= weekStart && r.CreatedAt.Date < weekEnd);

            var completedInWeek = assignments
                .Count(a => a.SubmittedAt.HasValue && 
                           a.SubmittedAt.Value.Date >= weekStart && 
                           a.SubmittedAt.Value.Date < weekEnd);

            var newAssignmentsInWeek = assignments
                .Count(a => a.CreatedAt.Date >= weekStart && a.CreatedAt.Date < weekEnd);

            var totalAssignmentsInWeek = assignments.Count(a => a.CreatedAt.Date < weekEnd);
            var completedByWeekEnd = assignments.Count(a => 
                a.SubmittedAt.HasValue && a.SubmittedAt.Value.Date < weekEnd);

            weeklyProgress.Add(new WeeklyProgressViewModel
            {
                WeekStartDate = weekStart,
                ResponsesReceived = responsesInWeek,
                AssignmentsCompleted = completedInWeek,  
                NewAssignments = newAssignmentsInWeek,
                CompletionRate = totalAssignmentsInWeek > 0 ? 
                    (decimal)completedByWeekEnd / totalAssignmentsInWeek * 100 : 0
            });
        }

        return weeklyProgress;
    }

    private (string indicator, string color) GetPerformanceIndicator(decimal completionRate, int overdueCount, int totalAssignments, DateTime? deadline)
    {
        if (overdueCount > 0)
        {
            var overduePercentage = totalAssignments > 0 ? (decimal)overdueCount / totalAssignments * 100 : 0;
            if (overduePercentage > 25)
                return ("Behind", "danger");
            else
                return ("At Risk", "warning");
        }

        if (completionRate >= 90)
            return ("Excellent", "success");
        else if (completionRate >= 70)
            return ("Good", "info");
        else if (completionRate >= 50)
            return ("On Track", "primary");
        else
        {
            // Check if we're near deadline
            if (deadline.HasValue && deadline.Value.Subtract(DateTime.Now).TotalDays <= 7)
                return ("At Risk", "warning");
            else
                return ("In Progress", "secondary");
        }
    }

    // Enhanced assignment-focused helper methods for dashboard

    private async Task<List<CompanyAssignmentStatusViewModel>> BuildCompanyBreakdownAsync(List<CampaignAssignment> assignments)
    {
        // Group assignments by organization
        var companyGroups = assignments
            .GroupBy(a => a.TargetOrganizationId)
            .ToList();

        var companyBreakdown = new List<CompanyAssignmentStatusViewModel>();

        foreach (var group in companyGroups)
        {
            var companyAssignments = group.ToList();
            var organization = companyAssignments.First().TargetOrganization;
            if (organization == null) continue;

            var now = DateTime.Now;
            var completedAssignments = companyAssignments.Where(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved).ToList();
            var overdueAssignments = companyAssignments.Where(a => 
                a.Campaign.Deadline.HasValue && 
                a.Campaign.Deadline.Value < now && 
                a.Status != AssignmentStatus.Submitted && a.Status != AssignmentStatus.Approved).ToList();

            // Get organization attributes from relationship
            var attributes = new List<OrganizationAttributeViewModel>();
            var relationship = await _context.OrganizationRelationships
                .Include(or => or.Attributes)
                .FirstOrDefaultAsync(or => or.SupplierOrganizationId == organization.Id && or.IsActive);

            if (relationship?.Attributes != null)
            {
                attributes = relationship.Attributes
                    .Where(a => a.IsActive)
                    .Select(a => new OrganizationAttributeViewModel
                    {
                        AttributeType = a.AttributeType,
                        Value = a.AttributeValue
                    }).ToList();
            }

            // Calculate response time
            var responseTimeHours = CalculateAverageResponseTime(companyAssignments);

            // Get active responders
            var activeResponders = companyAssignments
                .Where(a => !string.IsNullOrEmpty(a.LeadResponderId))
                .Select(a => a.LeadResponderId)
                .Distinct()
                .Count();

            // Get total responders (including question assignments)
            var totalResponders = await _context.QuestionAssignments
                .Where(qa => companyAssignments.Select(ca => ca.Id).Contains(qa.CampaignAssignmentId) && qa.AssignedUserId != null)
                .Select(qa => qa.AssignedUserId)
                .Distinct()
                .CountAsync();

            // Build campaign assignment summaries
            var campaignSummaries = companyAssignments.Select(a => new CampaignAssignmentSummaryViewModel
            {
                CampaignId = a.CampaignId,
                CampaignName = a.Campaign.Name,
                CampaignStatus = a.Campaign.Status,
                AssignmentStatus = a.Status,
                Deadline = a.Campaign.Deadline,
                LastActivityDate = a.Responses?.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt).FirstOrDefault()?.UpdatedAt ?? a.Responses?.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.CreatedAt,
                LeadResponderName = a.LeadResponder != null ? $"{a.LeadResponder.FirstName} {a.LeadResponder.LastName}" : null,
                QuestionsAnswered = a.Responses?.Count ?? 0,
                TotalQuestions = a.QuestionnaireVersion?.Questionnaire?.Questions?.Count ?? 0
            }).ToList();

            var companyStatus = new CompanyAssignmentStatusViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationType = organization.TypeDisplayName,
                Attributes = attributes,
                RelationshipType = relationship?.RelationshipType ?? "Unknown",
                TotalAssignments = companyAssignments.Count,
                CompletedAssignments = completedAssignments.Count,
                InProgressAssignments = companyAssignments.Count(a => a.Status == AssignmentStatus.InProgress),
                NotStartedAssignments = companyAssignments.Count(a => a.Status == AssignmentStatus.NotStarted),
                OverdueAssignments = overdueAssignments.Count,
                UnderReviewAssignments = companyAssignments.Count(a => a.Status == AssignmentStatus.UnderReview),
                ActiveResponders = activeResponders,
                TotalResponders = Math.Max(totalResponders, activeResponders),
                LastResponseDate = GetLastResponseDate(companyAssignments),
                AverageResponseTimeHours = responseTimeHours,
                NextDeadline = companyAssignments
                    .Where(a => a.Campaign.Deadline.HasValue)
                    .OrderBy(a => a.Campaign.Deadline)
                    .FirstOrDefault()?.Campaign.Deadline,
                CampaignAssignments = campaignSummaries
            };

            companyBreakdown.Add(companyStatus);
        }

        return companyBreakdown
            .OrderByDescending(c => c.IsAtRisk)
            .ThenByDescending(c => c.OverdueAssignments)
            .ThenBy(c => c.CompletionRate)
            .ToList();
    }

    private async Task<List<ResponderWorkloadViewModel>> BuildResponderBreakdownAsync(List<CampaignAssignment> assignments)
    {
        var responderBreakdown = new List<ResponderWorkloadViewModel>();

        // Get all lead responders
        var leadResponders = assignments
            .Where(a => !string.IsNullOrEmpty(a.LeadResponderId))
            .GroupBy(a => a.LeadResponderId)
            .ToList();

        foreach (var group in leadResponders)
        {
            var responderAssignments = group.ToList();
            var leadResponder = responderAssignments.First().LeadResponder;
            if (leadResponder == null) continue;

            var organization = responderAssignments.First().TargetOrganization;

            // Get question assignments for this user
            var questionAssignments = await _context.QuestionAssignments
                .Include(qa => qa.CampaignAssignment)
                    .ThenInclude(ca => ca.Campaign)
                .Include(qa => qa.Question)
                .Where(qa => qa.AssignedUserId == leadResponder.Id)
                .ToListAsync();

            // Get delegated assignments
            var delegatedAssignments = await _context.Delegations
                .Where(d => d.ToUserId == leadResponder.Id)
                .CountAsync();

            // Calculate metrics
            var completedAssignments = responderAssignments.Where(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved).ToList();
            var overdueAssignments = responderAssignments.Where(a => 
                a.Campaign.Deadline.HasValue && 
                a.Campaign.Deadline.Value < DateTime.Now && 
                a.Status != AssignmentStatus.Submitted && a.Status != AssignmentStatus.Approved).ToList();

            var responseTimeHours = CalculateAverageResponseTime(responderAssignments);
            var lastActivityDate = GetLastResponseDate(responderAssignments);

            // Build assignment details
            var assignmentDetails = responderAssignments.Select(a => new ResponderAssignmentSummaryViewModel
            {
                AssignmentId = a.Id,
                CampaignName = a.Campaign.Name,
                OrganizationName = a.TargetOrganization?.Name ?? "Unknown",
                Status = a.Status,
                Deadline = a.Campaign.Deadline,
                LastActivityDate = a.Responses?.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt).FirstOrDefault()?.UpdatedAt ?? a.Responses?.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.CreatedAt,
                QuestionsAnswered = a.Responses?.Count ?? 0,
                TotalQuestions = a.QuestionnaireVersion?.Questionnaire?.Questions?.Count ?? 0,
                IsLeadResponder = true,
                IsDelegated = false
            }).ToList();

            // Add delegated assignments to details
            var delegatedDetails = questionAssignments.Select(qa => new ResponderAssignmentSummaryViewModel
            {
                AssignmentId = qa.CampaignAssignmentId,
                CampaignName = qa.CampaignAssignment.Campaign.Name,
                OrganizationName = qa.CampaignAssignment.TargetOrganization?.Name ?? "Unknown",
                Status = qa.CampaignAssignment.Status,
                Deadline = qa.CampaignAssignment.Campaign.Deadline,
                LastActivityDate = qa.CampaignAssignment.Responses?
                    .Where(r => r.QuestionId == qa.QuestionId)
                    .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                    .FirstOrDefault()?.UpdatedAt ??
                    qa.CampaignAssignment.Responses?
                    .Where(r => r.QuestionId == qa.QuestionId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault()?.CreatedAt,
                QuestionsAnswered = qa.CampaignAssignment.Responses?.Count(r => r.QuestionId == qa.QuestionId) ?? 0,
                TotalQuestions = 1, // Individual question assignment
                IsLeadResponder = false,
                IsDelegated = true
            }).ToList();

            assignmentDetails.AddRange(delegatedDetails);

            var responderWorkload = new ResponderWorkloadViewModel
            {
                UserId = leadResponder.Id,
                UserName = $"{leadResponder.FirstName} {leadResponder.LastName}",
                UserEmail = leadResponder.Email ?? "",
                OrganizationName = organization?.Name ?? "Unknown",
                OrganizationId = organization?.Id ?? 0,
                UserRole = "User", // TODO: Load user roles if needed
                TotalAssignments = responderAssignments.Count,
                CompletedAssignments = completedAssignments.Count,
                InProgressAssignments = responderAssignments.Count(a => a.Status == AssignmentStatus.InProgress),
                NotStartedAssignments = responderAssignments.Count(a => a.Status == AssignmentStatus.NotStarted),
                OverdueAssignments = overdueAssignments.Count,
                AssignmentsAsLeadResponder = responderAssignments.Count,
                DelegatedAssignments = delegatedAssignments,
                AverageResponseTimeHours = responseTimeHours,
                LastActivityDate = lastActivityDate,
                QuestionsAnswered = responderAssignments.Sum(a => a.Responses?.Count ?? 0),
                TotalQuestionsAssigned = responderAssignments.Sum(a => a.QuestionnaireVersion?.Questionnaire?.Questions?.Count ?? 0) + questionAssignments.Count,
                AssignmentDetails = assignmentDetails.OrderByDescending(a => a.LastActivityDate).ToList()
            };

            responderBreakdown.Add(responderWorkload);
        }

        return responderBreakdown
            .OrderByDescending(r => r.IsOverloaded)
            .ThenByDescending(r => r.OverdueAssignments)
            .ThenBy(r => r.CompletionRate)
            .ToList();
    }

    private AssignmentStatusDistributionViewModel BuildStatusDistribution(List<CampaignAssignment> assignments)
    {
        var now = DateTime.Now;
        var overdueAssignments = assignments.Where(a => 
            a.Campaign.Deadline.HasValue && 
            a.Campaign.Deadline.Value < now && 
            a.Status != AssignmentStatus.Submitted && a.Status != AssignmentStatus.Approved).ToList();

        return new AssignmentStatusDistributionViewModel
        {
            TotalAssignments = assignments.Count,
            NotStartedCount = assignments.Count(a => a.Status == AssignmentStatus.NotStarted),
            InProgressCount = assignments.Count(a => a.Status == AssignmentStatus.InProgress),
            SubmittedCount = assignments.Count(a => a.Status == AssignmentStatus.Submitted),
            UnderReviewCount = assignments.Count(a => a.Status == AssignmentStatus.UnderReview),
            ApprovedCount = assignments.Count(a => a.Status == AssignmentStatus.Approved),
            ChangesRequestedCount = assignments.Count(a => a.Status == AssignmentStatus.ChangesRequested),
            OverdueCount = overdueAssignments.Count
        };
    }

    #endregion
} 