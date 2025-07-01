using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Services;
using Microsoft.AspNetCore.Identity;

namespace ESGPlatform.Controllers;

[Authorize(Policy = "CampaignManagerOrHigher")]
public class QuestionDependencyController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IConditionalQuestionService _conditionalService;

    public QuestionDependencyController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IConditionalQuestionService conditionalService)
    {
        _context = context;
        _userManager = userManager;
        _conditionalService = conditionalService;
    }

    // GET: QuestionDependency/Manage/5 (questionnaireId)
    public async Task<IActionResult> Manage(int id)
    {
        var questionnaire = await _context.Questionnaires
            .Include(q => q.Questions)
                .ThenInclude(q => q.Dependencies)
                    .ThenInclude(d => d.DependsOnQuestion)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (questionnaire == null)
        {
            return NotFound();
        }

        ViewBag.Questionnaire = questionnaire;
        ViewBag.Questions = questionnaire.Questions.OrderBy(q => q.DisplayOrder).ToList();
        
        return View();
    }

    // POST: QuestionDependency/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] int questionId, [FromForm] int dependsOnQuestionId, 
        [FromForm] DependencyConditionType conditionType, [FromForm] string? conditionValue)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        // Validation
        if (questionId == dependsOnQuestionId)
        {
            TempData["Error"] = "A question cannot depend on itself.";
            return RedirectToAction(nameof(Manage), new { id = await GetQuestionnaireIdAsync(questionId) });
        }

        // Check if dependency already exists
        var existingDependency = await _context.QuestionDependencies
            .FirstOrDefaultAsync(qd => qd.QuestionId == questionId && 
                                      qd.DependsOnQuestionId == dependsOnQuestionId &&
                                      qd.IsActive);

        if (existingDependency != null)
        {
            TempData["Error"] = "This dependency already exists.";
            return RedirectToAction(nameof(Manage), new { id = await GetQuestionnaireIdAsync(questionId) });
        }

        // Create dependency
        var dependency = new QuestionDependency
        {
            QuestionId = questionId,
            DependsOnQuestionId = dependsOnQuestionId,
            ConditionType = conditionType,
            ConditionValue = conditionValue,
            CreatedById = user.Id,
            IsActive = true
        };

        _context.QuestionDependencies.Add(dependency);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Question dependency created successfully.";
        return RedirectToAction(nameof(Manage), new { id = await GetQuestionnaireIdAsync(questionId) });
    }

    // POST: QuestionDependency/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var dependency = await _context.QuestionDependencies.FindAsync(id);
        if (dependency == null)
        {
            return NotFound();
        }

        var questionnaireId = await GetQuestionnaireIdAsync(dependency.QuestionId);
        
        _context.QuestionDependencies.Remove(dependency);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Question dependency deleted successfully.";
        return RedirectToAction(nameof(Manage), new { id = questionnaireId });
    }

    // POST: QuestionDependency/Toggle/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var dependency = await _context.QuestionDependencies.FindAsync(id);
        if (dependency == null)
        {
            return NotFound();
        }

        var questionnaireId = await GetQuestionnaireIdAsync(dependency.QuestionId);
        
        dependency.IsActive = !dependency.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Question dependency {(dependency.IsActive ? "activated" : "deactivated")} successfully.";
        return RedirectToAction(nameof(Manage), new { id = questionnaireId });
    }

    private async Task<int> GetQuestionnaireIdAsync(int questionId)
    {
        var question = await _context.Questions.FindAsync(questionId);
        return question?.QuestionnaireId ?? 0;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }
} 