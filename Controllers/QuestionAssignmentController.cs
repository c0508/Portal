using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Services;

namespace ESGPlatform.Controllers;

[Authorize]
public class QuestionAssignmentController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly QuestionAssignmentService _assignmentService;
    private readonly ILogger<QuestionAssignmentController> _logger;

    public QuestionAssignmentController(
        ApplicationDbContext context, 
        QuestionAssignmentService assignmentService,
        ILogger<QuestionAssignmentController> logger)
    {
        _context = context;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    // GET: /QuestionAssignment/Manage/5
    [HttpGet]
    public async Task<IActionResult> Manage(int id)
    {
        try
        {
            // Verify user has permission to manage assignments for this campaign assignment
            var campaignAssignment = await _context.CampaignAssignments
                .Include(ca => ca.Campaign)
                .FirstOrDefaultAsync(ca => ca.Id == id);

            if (campaignAssignment == null)
            {
                return NotFound();
            }

            // Only campaign creators and organization admins can manage assignments
            if (!CanManageAssignments(campaignAssignment))
            {
                return Forbid();
            }

            var viewModel = await _assignmentService.GetAssignmentManagementViewModelAsync(id);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment management for campaign assignment {Id}", id);
            TempData["Error"] = "Error loading assignment management. Please try again.";
            return RedirectToAction("Details", "Campaign", new { id = id });
        }
    }

    // POST: /QuestionAssignment/AssignQuestion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignQuestion(int campaignAssignmentId, int questionId, string assignedUserId, string? instructions)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(campaignAssignmentId))
            {
                return Forbid();
            }

            // Check if question is already assigned
            if (await _assignmentService.IsQuestionAssignedAsync(questionId, campaignAssignmentId))
            {
                TempData["Error"] = "Question is already assigned to a user.";
                return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
            }

            if (CurrentUserId == null)
            {
                return Forbid();
            }
            await _assignmentService.CreateQuestionAssignmentAsync(campaignAssignmentId, questionId, assignedUserId, CurrentUserId, instructions);

            TempData["Success"] = "Question assigned successfully.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning question {QuestionId} to user {UserId}", questionId, assignedUserId);
            TempData["Error"] = "Error assigning question. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
    }

    // POST: /QuestionAssignment/AssignSection
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSection(int campaignAssignmentId, string sectionName, string assignedUserId, string? instructions)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(campaignAssignmentId))
            {
                return Forbid();
            }

            // Check if section is already assigned
            if (await _assignmentService.IsSectionAssignedAsync(sectionName, campaignAssignmentId))
            {
                TempData["Error"] = "Section is already assigned to a user.";
                return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
            }

            if (CurrentUserId == null)
            {
                return Forbid();
            }
            await _assignmentService.CreateSectionAssignmentAsync(campaignAssignmentId, sectionName, assignedUserId, CurrentUserId, instructions);

            TempData["Success"] = "Section assigned successfully.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning section {SectionName} to user {UserId}", sectionName, assignedUserId);
            TempData["Error"] = "Error assigning section. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
    }

    // POST: /QuestionAssignment/UnassignSection
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignSection(int assignmentId, int campaignAssignmentId)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(campaignAssignmentId))
            {
                return Forbid();
            }

            if (CurrentUserId == null)
            {
                return Forbid();
            }
            var success = await _assignmentService.DeleteAssignmentAsync(assignmentId, CurrentUserId);
            if (success)
            {
                TempData["Success"] = "Section assignment removed successfully.";
            }
            else
            {
                TempData["Error"] = "Section assignment not found.";
            }

            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning section assignment {AssignmentId}", assignmentId);
            TempData["Error"] = "Error removing section assignment. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
    }

    // POST: /QuestionAssignment/BulkAssign
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAssign(BulkAssignmentViewModel model)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(model.CampaignAssignmentId))
            {
                return Forbid();
            }

            if (model.AssignmentType == "Questions" && model.SelectedQuestionIds?.Any() == true)
            {
                if (CurrentUserId == null)
                {
                    return Forbid();
                }
                await _assignmentService.CreateBulkAssignmentsAsync(
                    model.CampaignAssignmentId, 
                    model.SelectedQuestionIds, 
                    model.AssignedUserId, 
                    CurrentUserId, 
                    model.Instructions);

                TempData["Success"] = $"{model.SelectedQuestionIds.Count} questions assigned successfully.";
            }
            else if (model.AssignmentType == "Section" && model.SelectedSections?.Any() == true)
            {
                foreach (var sectionName in model.SelectedSections)
                {
                    if (!await _assignmentService.IsSectionAssignedAsync(sectionName, model.CampaignAssignmentId))
                    {
                        if (CurrentUserId == null)
                        {
                            return Forbid();
                        }
                        await _assignmentService.CreateSectionAssignmentAsync(
                            model.CampaignAssignmentId, 
                            sectionName, 
                            model.AssignedUserId, 
                            CurrentUserId, 
                            model.Instructions);
                    }
                }

                TempData["Success"] = $"{model.SelectedSections.Count} sections assigned successfully.";
            }
            else
            {
                TempData["Error"] = "Please select questions or sections to assign.";
            }

            return RedirectToAction(nameof(Manage), new { id = model.CampaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk assignment for campaign assignment {Id}", model.CampaignAssignmentId);
            TempData["Error"] = "Error in bulk assignment. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = model.CampaignAssignmentId });
        }
    }

    // POST: /QuestionAssignment/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int campaignAssignmentId)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(campaignAssignmentId))
            {
                return Forbid();
            }

            if (CurrentUserId == null)
            {
                return Forbid();
            }
            var success = await _assignmentService.DeleteAssignmentAsync(id, CurrentUserId);
            if (success)
            {
                TempData["Success"] = "Assignment removed successfully.";
            }
            else
            {
                TempData["Error"] = "Assignment not found.";
            }

            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {Id}", id);
            TempData["Error"] = "Error removing assignment. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
    }

    // POST: /QuestionAssignment/UpdateInstructions
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInstructions(int id, int campaignAssignmentId, string? instructions)
    {
        try
        {
            if (!await CanManageAssignmentsAsync(campaignAssignmentId))
            {
                return Forbid();
            }

            var success = await _assignmentService.UpdateAssignmentAsync(id, instructions, CurrentUserId);
            if (success)
            {
                TempData["Success"] = "Assignment instructions updated successfully.";
            }
            else
            {
                TempData["Error"] = "Assignment not found.";
            }

            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment instructions for {Id}", id);
            TempData["Error"] = "Error updating instructions. Please try again.";
            return RedirectToAction(nameof(Manage), new { id = campaignAssignmentId });
        }
    }

    // GET: /QuestionAssignment/Progress/5
    [HttpGet]
    public async Task<IActionResult> Progress(int id)
    {
        try
        {
            // Verify user has permission to view progress for this campaign assignment
            if (!await CanViewProgressAsync(id))
            {
                return Forbid();
            }

            var viewModel = await _assignmentService.GetAssignmentProgressAsync(id);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment progress for campaign assignment {Id}", id);
            TempData["Error"] = "Error loading assignment progress. Please try again.";
            return RedirectToAction("Details", "Campaign", new { id = id });
        }
    }

    // API endpoint for getting user assignments (used by Response controller)
    [HttpGet]
    public async Task<IActionResult> GetUserAssignments(int campaignAssignmentId, string? userId = null)
    {
        try
        {
            userId ??= CurrentUserId;
            if (userId == null)
            {
                return Forbid();
            }
            var assignedQuestionIds = await _assignmentService.GetAssignedQuestionIdsForUserAsync(userId, campaignAssignmentId);
            return Json(assignedQuestionIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user assignments for campaign assignment {Id}, user {UserId}", campaignAssignmentId, userId);
            return Json(new List<int>());
        }
    }

    #region Helper Methods

    private bool CanManageAssignments(CampaignAssignment campaignAssignment)
    {
        // Campaign creators and organization admins can manage assignments
        return campaignAssignment.Campaign.CreatedById == CurrentUserId ||
               IsOrgAdmin ||
               IsPlatformAdmin;
    }

    private async Task<bool> CanManageAssignmentsAsync(int campaignAssignmentId)
    {
        var campaignAssignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        return campaignAssignment != null && CanManageAssignments(campaignAssignment);
    }

    private async Task<bool> CanViewProgressAsync(int campaignAssignmentId)
    {
        var campaignAssignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        if (campaignAssignment == null)
            return false;

        // Campaign creators, organization admins, and lead responders can view progress
        return campaignAssignment.Campaign.CreatedById == CurrentUserId ||
               campaignAssignment.LeadResponderId == CurrentUserId ||
               IsOrgAdmin ||
               IsPlatformAdmin;
    }

    #endregion
} 