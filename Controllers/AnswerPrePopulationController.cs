using ESGPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ESGPlatform.Controllers;

[Authorize]
public class AnswerPrePopulationController : BaseController
{
    private readonly IAnswerPrePopulationService _prePopulationService;
    private readonly ILogger<AnswerPrePopulationController> _logger;

    public AnswerPrePopulationController(
        IAnswerPrePopulationService prePopulationService,
        ILogger<AnswerPrePopulationController> logger)
    {
        _prePopulationService = prePopulationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPreviousCampaigns(int campaignAssignmentId)
    {
        try
        {
            var previousCampaigns = await _prePopulationService.GetAvailablePreviousCampaignsAsync(campaignAssignmentId);
            return Json(new { success = true, data = previousCampaigns });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous campaigns for assignment {AssignmentId}", campaignAssignmentId);
            return Json(new { success = false, message = "Failed to retrieve previous campaigns." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PreviewMatching(int currentAssignmentId, int previousAssignmentId)
    {
        try
        {
            var matchingResult = await _prePopulationService.PreviewQuestionMatchingAsync(currentAssignmentId, previousAssignmentId);
            return Json(new { success = true, data = matchingResult });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing question matching between assignments {CurrentId} and {PreviousId}", 
                currentAssignmentId, previousAssignmentId);
            return Json(new { success = false, message = "Failed to preview question matching." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PrePopulateAnswers(int currentAssignmentId, int previousAssignmentId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var result = await _prePopulationService.PrePopulateAnswersAsync(currentAssignmentId, previousAssignmentId, userId);
            
            var message = $"Successfully pre-populated {result.PrePopulatedCount} answers. " +
                         $"{result.UnmatchedCount} questions could not be matched. " +
                         $"{result.SkippedCount} questions already had responses.";

            return Json(new { 
                success = true, 
                message = message,
                data = result 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-populating answers between assignments {CurrentId} and {PreviousId}", 
                currentAssignmentId, previousAssignmentId);
            return Json(new { success = false, message = "Failed to pre-populate answers." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AcceptPrePopulatedAnswer(int responseId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var result = await _prePopulationService.AcceptPrePopulatedAnswerAsync(responseId, userId);
            
            if (result)
            {
                return Json(new { success = true, message = "Pre-populated answer accepted." });
            }
            else
            {
                return Json(new { success = false, message = "Answer not found or already accepted." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting pre-populated answer {ResponseId}", responseId);
            return Json(new { success = false, message = "Failed to accept pre-populated answer." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RejectPrePopulatedAnswer(int responseId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var result = await _prePopulationService.RejectPrePopulatedAnswerAsync(responseId, userId);
            
            if (result)
            {
                return Json(new { success = true, message = "Pre-populated answer rejected and removed." });
            }
            else
            {
                return Json(new { success = false, message = "Answer not found or not pre-populated." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting pre-populated answer {ResponseId}", responseId);
            return Json(new { success = false, message = "Failed to reject pre-populated answer." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AcceptAllPrePopulatedAnswers(int campaignAssignmentId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var count = await _prePopulationService.AcceptAllPrePopulatedAnswersAsync(campaignAssignmentId, userId);
            
            return Json(new { 
                success = true, 
                message = $"Accepted {count} pre-populated answers.",
                count = count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting all pre-populated answers for assignment {AssignmentId}", campaignAssignmentId);
            return Json(new { success = false, message = "Failed to accept all pre-populated answers." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RejectAllPrePopulatedAnswers(int campaignAssignmentId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var count = await _prePopulationService.RejectAllPrePopulatedAnswersAsync(campaignAssignmentId, userId);
            
            return Json(new { 
                success = true, 
                message = $"Rejected and removed {count} pre-populated answers.",
                count = count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting all pre-populated answers for assignment {AssignmentId}", campaignAssignmentId);
            return Json(new { success = false, message = "Failed to reject all pre-populated answers." });
        }
    }
} 