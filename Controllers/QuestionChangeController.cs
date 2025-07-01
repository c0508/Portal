using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Controllers;

[Authorize(Policy = "CampaignManagerOrHigher")] // CampaignManager+ can view change history
public class QuestionChangeController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionChangeController> _logger;

    public QuestionChangeController(ApplicationDbContext context, ILogger<QuestionChangeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /QuestionChange
    public async Task<IActionResult> Index(QuestionChangeFilterViewModel filter, int page = 1, int pageSize = 20)
    {
        // Start with base query
        var query = _context.QuestionChanges
            .Include(qc => qc.Question)
                .ThenInclude(q => q.Questionnaire)
            .Include(qc => qc.ChangedBy)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            query = query.Where(qc => qc.Question.QuestionText.Contains(filter.SearchText) ||
                                     qc.Question.Questionnaire.Title.Contains(filter.SearchText));
        }

        if (filter.QuestionnaireId.HasValue)
        {
            query = query.Where(qc => qc.Question.QuestionnaireId == filter.QuestionnaireId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Section))
        {
            query = query.Where(qc => qc.Question.Section == filter.Section);
        }

        if (!string.IsNullOrEmpty(filter.ChangedBy))
        {
            query = query.Where(qc => qc.ChangedById == filter.ChangedBy);
        }

        if (!string.IsNullOrEmpty(filter.FieldName))
        {
            query = query.Where(qc => qc.FieldName == filter.FieldName);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(qc => qc.ChangedAt.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(qc => qc.ChangedAt.Date <= filter.ToDate.Value.Date);
        }

        // Order by most recent first
        query = query.OrderByDescending(qc => qc.ChangedAt);

        // Calculate pagination
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (page - 1) * pageSize;

        // Get the data
        var changes = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(qc => new QuestionChangeViewModel
            {
                Id = qc.Id,
                FieldName = qc.FieldName ?? "",
                OldValue = qc.OldValue,
                NewValue = qc.NewValue,
                ChangeReason = qc.ChangeReason,
                ChangedAt = qc.ChangedAt,
                ChangedByName = $"{qc.ChangedBy.FirstName} {qc.ChangedBy.LastName}",
                ChangedByEmail = qc.ChangedBy.Email ?? ""
            })
            .ToListAsync();

        // Populate filter dropdowns
        await PopulateFilterDropdowns(filter);

        ViewBag.Filter = filter;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.PageSize = pageSize;

        return View(changes);
    }

    // GET: /QuestionChange/Question/5
    public async Task<IActionResult> Question(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Questionnaire)
            .Include(q => q.Changes)
                .ThenInclude(qc => qc.ChangedBy)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound();
        }

        var viewModel = new QuestionChangeHistoryViewModel
        {
            QuestionId = question.Id,
            QuestionText = question.QuestionText,
            QuestionnaireName = question.Questionnaire.Title,
            Section = question.Section,
            Changes = question.Changes
                .OrderByDescending(qc => qc.ChangedAt)
                .Select(qc => new QuestionChangeViewModel
                {
                    Id = qc.Id,
                    FieldName = qc.FieldName ?? "",
                    OldValue = qc.OldValue,
                    NewValue = qc.NewValue,
                    ChangeReason = qc.ChangeReason,
                    ChangedAt = qc.ChangedAt,
                    ChangedByName = $"{qc.ChangedBy.FirstName} {qc.ChangedBy.LastName}",
                    ChangedByEmail = qc.ChangedBy.Email ?? ""
                })
                .ToList()
        };

        return View(viewModel);
    }

    // GET: /QuestionChange/Summary
    public async Task<IActionResult> Summary()
    {
        var today = DateTime.Today;
        var weekAgo = today.AddDays(-7);

        var summary = new QuestionChangesSummaryViewModel
        {
            TotalChanges = await _context.QuestionChanges.CountAsync(),
            QuestionsModified = await _context.QuestionChanges
                .Select(qc => qc.QuestionId)
                .Distinct()
                .CountAsync(),
            ChangesToday = await _context.QuestionChanges
                .Where(qc => qc.ChangedAt.Date == today)
                .CountAsync(),
            ChangesThisWeek = await _context.QuestionChanges
                .Where(qc => qc.ChangedAt.Date >= weekAgo)
                .CountAsync(),
            TopChangers = await _context.QuestionChanges
                .GroupBy(qc => new { qc.ChangedById, qc.ChangedBy.FirstName, qc.ChangedBy.LastName, qc.ChangedBy.Email })
                .Select(g => new TopChangerViewModel
                {
                    UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                    UserEmail = g.Key.Email ?? "",
                    ChangeCount = g.Count()
                })
                .OrderByDescending(t => t.ChangeCount)
                .Take(10)
                .ToListAsync(),
            MostChangedQuestions = await _context.QuestionChanges
                .GroupBy(qc => new 
                { 
                    qc.QuestionId, 
                    qc.Question.QuestionText, 
                    qc.Question.Questionnaire.Title,
                    qc.Question.Section
                })
                .Select(g => new MostChangedQuestionViewModel
                {
                    QuestionId = g.Key.QuestionId,
                    QuestionText = g.Key.QuestionText,
                    QuestionnaireName = g.Key.Title,
                    Section = g.Key.Section,
                    ChangeCount = g.Count(),
                    LastChanged = g.Max(qc => qc.ChangedAt)
                })
                .OrderByDescending(m => m.ChangeCount)
                .Take(10)
                .ToListAsync()
        };

        return View(summary);
    }

    // GET: /QuestionChange/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var change = await _context.QuestionChanges
            .Include(qc => qc.Question)
                .ThenInclude(q => q.Questionnaire)
            .Include(qc => qc.ChangedBy)
            .FirstOrDefaultAsync(qc => qc.Id == id);

        if (change == null)
        {
            return NotFound();
        }

        var viewModel = new QuestionChangeViewModel
        {
            Id = change.Id,
            FieldName = change.FieldName ?? "",
            OldValue = change.OldValue,
            NewValue = change.NewValue,
            ChangeReason = change.ChangeReason,
            ChangedAt = change.ChangedAt,
            ChangedByName = $"{change.ChangedBy.FirstName} {change.ChangedBy.LastName}",
            ChangedByEmail = change.ChangedBy.Email ?? ""
        };

        ViewBag.Question = change.Question;
        ViewBag.Questionnaire = change.Question.Questionnaire;

        return View(viewModel);
    }

    private async Task PopulateFilterDropdowns(QuestionChangeFilterViewModel filter)
    {
        // Questionnaires
        filter.Questionnaires = await _context.Questionnaires
            .Where(q => q.IsActive)
            .OrderBy(q => q.Title)
            .Select(q => new SelectListItem
            {
                Value = q.Id.ToString(),
                Text = q.Title,
                Selected = q.Id == filter.QuestionnaireId
            })
            .ToListAsync();

        filter.Questionnaires.Insert(0, new SelectListItem { Value = "", Text = "All Questionnaires" });

        // Sections
        filter.Sections = await _context.Questions
            .Where(q => !string.IsNullOrEmpty(q.Section))
            .Select(q => q.Section!)
            .Distinct()
            .OrderBy(s => s)
            .Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == filter.Section
            })
            .ToListAsync();

        filter.Sections.Insert(0, new SelectListItem { Value = "", Text = "All Sections" });

        // Users who have made changes
        filter.Users = await _context.QuestionChanges
            .Include(qc => qc.ChangedBy)
            .Select(qc => new { qc.ChangedById, qc.ChangedBy.FirstName, qc.ChangedBy.LastName })
            .Distinct()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SelectListItem
            {
                Value = u.ChangedById,
                Text = $"{u.FirstName} {u.LastName}",
                Selected = u.ChangedById == filter.ChangedBy
            })
            .ToListAsync();

        filter.Users.Insert(0, new SelectListItem { Value = "", Text = "All Users" });

        // Field names
        var fieldNames = new[]
        {
            new SelectListItem { Value = "QuestionText", Text = "Question Text" },
            new SelectListItem { Value = "HelpText", Text = "Help Text" },
            new SelectListItem { Value = "Section", Text = "Section" },
            new SelectListItem { Value = "QuestionType", Text = "Question Type" },
            new SelectListItem { Value = "IsRequired", Text = "Required" },
            new SelectListItem { Value = "Options", Text = "Options" },
            new SelectListItem { Value = "ValidationRules", Text = "Validation Rules" },
            new SelectListItem { Value = "DisplayOrder", Text = "Display Order" }
        };

        foreach (var item in fieldNames)
        {
            item.Selected = item.Value == filter.FieldName;
        }

        filter.FieldNames = fieldNames.ToList();
        filter.FieldNames.Insert(0, new SelectListItem { Value = "", Text = "All Fields" });
    }
} 