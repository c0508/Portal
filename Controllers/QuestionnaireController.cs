using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Services;

namespace ESGPlatform.Controllers
{
    [Authorize]
    public class QuestionnaireController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuestionTypeService _questionTypeService;
        private readonly IQuestionChangeTrackingService _changeTrackingService;
        private readonly IExcelImportService _excelImportService;
        private readonly IUnitService _unitService;

        public QuestionnaireController(ApplicationDbContext context, IQuestionTypeService questionTypeService, IQuestionChangeTrackingService changeTrackingService, IExcelImportService excelImportService, IUnitService unitService)
        {
            _context = context;
            _questionTypeService = questionTypeService;
            _changeTrackingService = changeTrackingService;
            _excelImportService = excelImportService;
            _unitService = unitService;
        }

        // GET: Questionnaire
        public async Task<IActionResult> Index()
        {
            var questionnaires = await _context.Questionnaires
                .Include(q => q.Questions)
                .Include(q => q.Versions)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(questionnaires);
        }

        // GET: Questionnaire/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var questionnaire = await _context.Questionnaires
                .Include(q => q.Questions.OrderBy(qu => qu.DisplayOrder))
                .Include(q => q.Versions)
                .Include(q => q.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questionnaire == null) return NotFound();

            return View(questionnaire);
        }

        // GET: Questionnaire/Create
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Create()
        {
            var model = new QuestionnaireCreateViewModel();
            
            // Load available questionnaires for copying
            model.AvailableQuestionnaires = await _context.Questionnaires
                .Where(q => q.OrganizationId == CurrentOrganizationId)
                .Select(q => new Questionnaire 
                { 
                    Id = q.Id, 
                    Title = q.Title, 
                    Category = q.Category,
                    CreatedAt = q.CreatedAt
                })
                .OrderBy(q => q.Title)
                .ToListAsync();
            
            return View(model);
        }

        // POST: Questionnaire/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Create(QuestionnaireCreateViewModel model, string action = "create")
        {
            // Handle Excel import preview
            if (action == "preview" && model.ImportFromExcel && model.ExcelFile != null)
            {
                try
                {
                    model.ImportedQuestions = await _excelImportService.ParseExcelFileAsync(model.ExcelFile);
                    TempData["InfoMessage"] = $"Successfully parsed {model.ImportedQuestions.Count} questions from Excel file.";
                    
                    // Check for validation errors
                    var errorCount = model.ImportedQuestions.Count(q => !q.IsValid);
                    if (errorCount > 0)
                    {
                        TempData["WarningMessage"] = $"{errorCount} questions have validation errors. Please fix them before creating the questionnaire.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error parsing Excel file: {ex.Message}";
                    model.ImportedQuestions = new List<ExcelQuestionPreviewViewModel>();
                }

                // Reload questionnaires for the view
                model.AvailableQuestionnaires = await _context.Questionnaires
                    .Where(q => q.OrganizationId == CurrentOrganizationId)
                    .Select(q => new Questionnaire { Id = q.Id, Title = q.Title, Category = q.Category })
                    .OrderBy(q => q.Title)
                    .ToListAsync();

                return View(model);
            }

            if (ModelState.IsValid)
            {
                Questionnaire questionnaire;

                if (model.CopyFromExisting && model.SourceQuestionnaireId.HasValue)
                {
                    // Copy from existing questionnaire
                    questionnaire = await CopyQuestionnaireAsync(model.SourceQuestionnaireId.Value, model);
                }
                else if (model.ImportFromExcel && model.ExcelFile != null)
                {
                    // Parse Excel file for creation (re-process the file)
                    try
                    {
                        model.ImportedQuestions = await _excelImportService.ParseExcelFileAsync(model.ExcelFile);
                        var validQuestions = model.ImportedQuestions.Where(q => q.IsValid).ToList();
                        
                        if (!validQuestions.Any())
                        {
                            TempData["ErrorMessage"] = "No valid questions found in Excel file. Please fix validation errors and try again.";
                            // Reload questionnaires for the view
                            model.AvailableQuestionnaires = await _context.Questionnaires
                                .Where(q => q.OrganizationId == CurrentOrganizationId)
                                .Select(q => new Questionnaire { Id = q.Id, Title = q.Title, Category = q.Category })
                                .OrderBy(q => q.Title)
                                .ToListAsync();
                            return View(model);
                        }
                        
                        // Create questionnaire with imported questions
                        questionnaire = await CreateQuestionnaireFromExcelAsync(model);
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Error processing Excel file: {ex.Message}";
                        // Reload questionnaires for the view
                        model.AvailableQuestionnaires = await _context.Questionnaires
                            .Where(q => q.OrganizationId == CurrentOrganizationId)
                            .Select(q => new Questionnaire { Id = q.Id, Title = q.Title, Category = q.Category })
                            .OrderBy(q => q.Title)
                            .ToListAsync();
                        return View(model);
                    }
                }
                else
                {
                    // Create new questionnaire
                    questionnaire = new Questionnaire
                    {
                        Title = model.Title,
                        Description = model.Description,
                        Category = model.Category,
                        IsActive = model.IsActive,
                        OrganizationId = CurrentOrganizationId ?? 0,
                        CreatedByUserId = CurrentUserId ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Add(questionnaire);
                    await _context.SaveChangesAsync();

                    // Create initial version
                    var version = new QuestionnaireVersion
                    {
                        QuestionnaireId = questionnaire.Id,
                        VersionNumber = "1.0",
                        ChangeDescription = "Initial version",
                        CreatedByUserId = CurrentUserId ?? string.Empty,
                        IsActive = true,
                        IsLocked = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Add(version);
                    await _context.SaveChangesAsync();
                }

                var importedCount = model.ImportFromExcel ? model.ImportedQuestions.Count(q => q.IsValid) : 0;
                TempData["SuccessMessage"] = model.CopyFromExisting 
                    ? "Questionnaire copied successfully!" 
                    : model.ImportFromExcel 
                        ? $"Questionnaire created successfully with {importedCount} imported questions!"
                        : "Questionnaire created successfully!";
                return RedirectToAction(nameof(Details), new { id = questionnaire.Id });
            }

            // Reload questionnaires for the view if validation fails
            model.AvailableQuestionnaires = await _context.Questionnaires
                .Where(q => q.OrganizationId == CurrentOrganizationId)
                .Select(q => new Questionnaire { Id = q.Id, Title = q.Title, Category = q.Category })
                .OrderBy(q => q.Title)
                .ToListAsync();

            return View(model);
        }

        // GET: Questionnaire/Edit/5
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var questionnaire = await _context.Questionnaires.FindAsync(id);
            if (questionnaire == null) return NotFound();

            var model = new QuestionnaireCreateViewModel
            {
                Id = questionnaire.Id,
                Title = questionnaire.Title,
                Description = questionnaire.Description,
                Category = questionnaire.Category,
                IsActive = questionnaire.IsActive
            };

            return View(model);
        }

        // POST: Questionnaire/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Edit(int id, QuestionnaireCreateViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var questionnaire = await _context.Questionnaires.FindAsync(id);
                    if (questionnaire == null) return NotFound();

                    questionnaire.Title = model.Title;
                    questionnaire.Description = model.Description;
                    questionnaire.Category = model.Category;
                    questionnaire.IsActive = model.IsActive;
                    questionnaire.UpdatedAt = DateTime.UtcNow;

                    _context.Update(questionnaire);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Questionnaire updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = questionnaire.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionnaireExists(model.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(model);
        }

        // GET: Questionnaire/Delete/5
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var questionnaire = await _context.Questionnaires
                .Include(q => q.Questions)
                .Include(q => q.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questionnaire == null) return NotFound();

            return View(questionnaire);
        }

        // POST: Questionnaire/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var questionnaire = await _context.Questionnaires.FindAsync(id);
            if (questionnaire != null)
            {
                _context.Questionnaires.Remove(questionnaire);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Questionnaire deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Questionnaire/ManageQuestions/5
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> ManageQuestions(int? id)
        {
            if (id == null) return NotFound();

            var questionnaire = await _context.Questionnaires
                .Include(q => q.Questions.OrderBy(qu => qu.DisplayOrder))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questionnaire == null) return NotFound();

            return View(questionnaire);
        }

        // GET: Questionnaire/AddQuestion/5
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> AddQuestion(int? id)
        {
            if (id == null) return NotFound();

            var questionnaire = await _context.Questionnaires.FindAsync(id);
            if (questionnaire == null) return NotFound();

            var maxOrder = await _context.Questions
                .Where(q => q.QuestionnaireId == id)
                .MaxAsync(q => (int?)q.DisplayOrder) ?? 0;

            var model = new QuestionCreateViewModel
            {
                QuestionnaireId = questionnaire.Id,
                QuestionnaireTitle = questionnaire.Title,
                DisplayOrder = maxOrder + 1,
                AvailableQuestionTypes = await _questionTypeService.GetActiveQuestionTypesAsync(),
                AvailableAttributes = await _context.QuestionAttributes
                    .Where(qa => qa.IsActive)
                    .OrderBy(qa => qa.Category)
                    .ThenBy(qa => qa.DisplayOrder)
                    .ToListAsync(),
                AvailableUnits = await _unitService.GetUnitsGroupedByCategoryAsync()
            };

            return View(model);
        }

        // POST: Questionnaire/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> AddQuestion(QuestionCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Get the selected question type master to map to the enum
                var questionTypeMaster = await _context.QuestionTypes.FindAsync(model.QuestionTypeMasterId);
                if (questionTypeMaster == null)
                {
                    ModelState.AddModelError("QuestionTypeMasterId", "Invalid question type selected.");
                    // Reload form data
                    var reloadQuestionnaire = await _context.Questionnaires.FindAsync(model.QuestionnaireId);
                    model.QuestionnaireTitle = reloadQuestionnaire?.Title ?? "";
                    model.AvailableQuestionTypes = await _questionTypeService.GetActiveQuestionTypesAsync();
                    model.AvailableAttributes = await _context.QuestionAttributes
                        .Where(qa => qa.IsActive)
                        .OrderBy(qa => qa.Category)
                        .ThenBy(qa => qa.DisplayOrder)
                        .ToListAsync();
                    model.AvailableUnits = await _unitService.GetUnitsGroupedByCategoryAsync();
                    return View(model);
                }

                // Map the question type code to the enum
                Enum.TryParse<QuestionType>(questionTypeMaster.Code, out var questionTypeEnum);

                var question = new Question
                {
                    QuestionnaireId = model.QuestionnaireId,
                    QuestionText = model.QuestionText,
                    QuestionType = questionTypeEnum,
                    QuestionTypeMasterId = model.QuestionTypeMasterId,
                    IsRequired = model.IsRequired,
                    DisplayOrder = model.DisplayOrder,
                    Options = model.Options,
                    HelpText = model.HelpText,
                    Section = model.Section,
                    ValidationRules = model.ValidationRules,
                    IsPercentage = model.IsPercentage,
                    Unit = model.Unit,
                    OrganizationId = CurrentOrganizationId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Add(question);
                await _context.SaveChangesAsync();

                // Add question attributes
                if (model.SelectedAttributeIds?.Any() == true)
                {
                    var questionAttributes = model.SelectedAttributeIds.Select((attributeId, index) => new QuestionQuestionAttribute
                    {
                        QuestionId = question.Id,
                        QuestionAttributeId = attributeId,
                        IsPrimary = index == 0, // Mark first attribute as primary
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.AddRange(questionAttributes);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Question added successfully!";
                return RedirectToAction(nameof(ManageQuestions), new { id = model.QuestionnaireId });
            }

            // Reload questionnaire title for the view
            var questionnaire = await _context.Questionnaires.FindAsync(model.QuestionnaireId);
            model.QuestionnaireTitle = questionnaire?.Title ?? "";
            model.AvailableQuestionTypes = await _questionTypeService.GetActiveQuestionTypesAsync();
            model.AvailableAttributes = await _context.QuestionAttributes
                .Where(qa => qa.IsActive)
                .OrderBy(qa => qa.Category)
                .ThenBy(qa => qa.DisplayOrder)
                .ToListAsync();
            model.AvailableUnits = await _unitService.GetUnitsGroupedByCategoryAsync();

            return View(model);
        }

        // GET: Questionnaire/EditQuestion/5
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> EditQuestion(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Questionnaire)
                .Include(q => q.QuestionAttributes)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();

            var model = new QuestionCreateViewModel
            {
                Id = question.Id,
                QuestionnaireId = question.QuestionnaireId,
                QuestionnaireTitle = question.Questionnaire.Title,
                QuestionText = question.QuestionText,
                QuestionTypeMasterId = question.QuestionTypeMasterId ?? 0,
                IsRequired = question.IsRequired,
                DisplayOrder = question.DisplayOrder,
                Options = question.Options,
                HelpText = question.HelpText,
                Section = question.Section,
                ValidationRules = question.ValidationRules,
                IsPercentage = question.IsPercentage,
                Unit = question.Unit,
                SelectedAttributeIds = question.QuestionAttributes.Select(qa => qa.QuestionAttributeId).ToList(),
                AvailableQuestionTypes = await _questionTypeService.GetActiveQuestionTypesAsync(),
                AvailableAttributes = await _context.QuestionAttributes
                    .Where(qa => qa.IsActive)
                    .OrderBy(qa => qa.Category)
                    .ThenBy(qa => qa.DisplayOrder)
                    .ToListAsync(),
                AvailableUnits = await _unitService.GetUnitsGroupedByCategoryAsync()
            };

            return View(model);
        }

        // POST: Questionnaire/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> EditQuestion(int id, QuestionCreateViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var question = await _context.Questions.FindAsync(id);
                    if (question == null) return NotFound();

                    // Create a copy of the original question for change tracking
                    var originalQuestion = new Question
                    {
                        Id = question.Id,
                        QuestionText = question.QuestionText,
                        HelpText = question.HelpText,
                        Section = question.Section,
                        QuestionType = question.QuestionType,
                        IsRequired = question.IsRequired,
                        Options = question.Options,
                        ValidationRules = question.ValidationRules,
                        IsPercentage = question.IsPercentage,
                        Unit = question.Unit,
                        DisplayOrder = question.DisplayOrder
                    };

                    // Get the selected question type master to map to the enum
                    var questionTypeMaster = await _context.QuestionTypes.FindAsync(model.QuestionTypeMasterId);
                    if (questionTypeMaster != null)
                    {
                        Enum.TryParse<QuestionType>(questionTypeMaster.Code, out var questionTypeEnum);
                        question.QuestionType = questionTypeEnum;
                        question.QuestionTypeMasterId = model.QuestionTypeMasterId;
                    }

                    question.QuestionText = model.QuestionText;
                    question.IsRequired = model.IsRequired;
                    question.DisplayOrder = model.DisplayOrder;
                    question.Options = model.Options;
                    question.HelpText = model.HelpText;
                    question.Section = model.Section;
                    question.ValidationRules = model.ValidationRules;
                    question.IsPercentage = model.IsPercentage;
                    question.Unit = model.Unit;
                    question.UpdatedAt = DateTime.UtcNow;

                    // Track changes before saving
                    await _changeTrackingService.TrackQuestionChangesAsync(
                        originalQuestion, 
                        question, 
                        CurrentUserId ?? string.Empty, 
                        "Question updated via edit form"
                    );

                    _context.Update(question);

                    // Update question attributes
                    // Remove existing attributes
                    var existingAttributes = await _context.QuestionQuestionAttributes
                        .Where(qa => qa.QuestionId == question.Id)
                        .ToListAsync();
                    _context.RemoveRange(existingAttributes);

                    // Add new attributes
                    if (model.SelectedAttributeIds?.Any() == true)
                    {
                        var newAttributes = model.SelectedAttributeIds.Select((attributeId, index) => new QuestionQuestionAttribute
                        {
                            QuestionId = question.Id,
                            QuestionAttributeId = attributeId,
                            IsPrimary = index == 0, // Mark first attribute as primary
                            CreatedAt = DateTime.UtcNow
                        }).ToList();

                        _context.AddRange(newAttributes);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Question updated successfully!";
                    return RedirectToAction(nameof(ManageQuestions), new { id = model.QuestionnaireId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(model.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            // Reload questionnaire title for the view
            var questionnaire = await _context.Questionnaires.FindAsync(model.QuestionnaireId);
            model.QuestionnaireTitle = questionnaire?.Title ?? "";
            model.AvailableQuestionTypes = await _questionTypeService.GetActiveQuestionTypesAsync();
            model.AvailableAttributes = await _context.QuestionAttributes
                .Where(qa => qa.IsActive)
                .OrderBy(qa => qa.Category)
                .ThenBy(qa => qa.DisplayOrder)
                .ToListAsync();
            model.AvailableUnits = await _unitService.GetUnitsGroupedByCategoryAsync();

            return View(model);
        }

        // POST: Questionnaire/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> DeleteQuestion(int id, int questionnaireId)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Question deleted successfully!";
            }

            return RedirectToAction(nameof(ManageQuestions), new { id = questionnaireId });
        }

        // POST: Questionnaire/ReorderQuestions
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> ReorderQuestions(int questionnaireId, int[] questionIds)
        {
            for (int i = 0; i < questionIds.Length; i++)
            {
                var question = await _context.Questions.FindAsync(questionIds[i]);
                if (question != null)
                {
                    question.DisplayOrder = i + 1;
                    question.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Question order updated successfully!";

            return RedirectToAction(nameof(ManageQuestions), new { id = questionnaireId });
        }

        // GET: Questionnaire/DownloadTemplate
        [Authorize(Policy = "OrgAdminOrHigher")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templateBytes = _excelImportService.GenerateQuestionnaireTemplate();
                var fileName = $"Questionnaire_Template_{DateTime.Now:yyyyMMdd}.xlsx";
                
                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating template: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        private async Task<Questionnaire> CreateQuestionnaireFromExcelAsync(QuestionnaireCreateViewModel model)
        {
            // Create new questionnaire
            var questionnaire = new Questionnaire
            {
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                IsActive = model.IsActive,
                OrganizationId = CurrentOrganizationId ?? 0,
                CreatedByUserId = CurrentUserId ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(questionnaire);
            await _context.SaveChangesAsync();

            // Create initial version
            var version = new QuestionnaireVersion
            {
                QuestionnaireId = questionnaire.Id,
                VersionNumber = "1.0",
                ChangeDescription = $"Initial version with {model.ImportedQuestions.Count} imported questions",
                CreatedByUserId = CurrentUserId ?? string.Empty,
                IsActive = true,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(version);
            await _context.SaveChangesAsync();

            // Create questions from imported data
            var displayOrder = 1;
            foreach (var importedQuestion in model.ImportedQuestions.Where(q => q.IsValid))
            {
                // Get question type master by code
                var questionTypeMaster = await _context.QuestionTypes
                    .FirstOrDefaultAsync(qt => qt.Code == importedQuestion.QuestionType);

                if (questionTypeMaster == null) continue;

                // Map question type string to enum
                Enum.TryParse<QuestionType>(importedQuestion.QuestionType, out var questionTypeEnum);

                var question = new Question
                {
                    QuestionnaireId = questionnaire.Id,
                    QuestionText = importedQuestion.QuestionText,
                    HelpText = importedQuestion.HelpText,
                    Section = string.IsNullOrWhiteSpace(importedQuestion.Section) ? null : importedQuestion.Section,
                    QuestionType = questionTypeEnum,
                    QuestionTypeMasterId = questionTypeMaster.Id,
                    IsRequired = importedQuestion.IsRequired,
                    DisplayOrder = displayOrder++,
                    Options = string.IsNullOrWhiteSpace(importedQuestion.Options) ? null : importedQuestion.Options,
                    IsPercentage = importedQuestion.IsPercentage,
                    Unit = string.IsNullOrWhiteSpace(importedQuestion.Unit) ? null : importedQuestion.Unit,
                    OrganizationId = CurrentOrganizationId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Add(question);
            }

            await _context.SaveChangesAsync();
            return questionnaire;
        }

        private async Task<Questionnaire> CopyQuestionnaireAsync(int sourceQuestionnaireId, QuestionnaireCreateViewModel model)
        {
            // Get source questionnaire with questions and their attributes
            var sourceQuestionnaire = await _context.Questionnaires
                .Include(q => q.Questions)
                    .ThenInclude(q => q.QuestionAttributes)
                        .ThenInclude(qa => qa.QuestionAttribute)
                .FirstOrDefaultAsync(q => q.Id == sourceQuestionnaireId && q.OrganizationId == CurrentOrganizationId);

            if (sourceQuestionnaire == null)
                throw new ArgumentException("Source questionnaire not found or access denied");

            // Create new questionnaire
            var newQuestionnaire = new Questionnaire
            {
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                IsActive = model.IsActive,
                OrganizationId = CurrentOrganizationId ?? 0,
                CreatedByUserId = CurrentUserId ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(newQuestionnaire);
            await _context.SaveChangesAsync();

            // Create initial version
            var version = new QuestionnaireVersion
            {
                QuestionnaireId = newQuestionnaire.Id,
                VersionNumber = "1.0",
                ChangeDescription = $"Copied from '{sourceQuestionnaire.Title}'",
                CreatedByUserId = CurrentUserId ?? string.Empty,
                IsActive = true,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(version);
            await _context.SaveChangesAsync();

            // Copy questions
            foreach (var sourceQuestion in sourceQuestionnaire.Questions.OrderBy(q => q.DisplayOrder))
            {
                var newQuestion = new Question
                {
                    QuestionnaireId = newQuestionnaire.Id,
                    QuestionText = sourceQuestion.QuestionText,
                    HelpText = sourceQuestion.HelpText,
                    QuestionType = sourceQuestion.QuestionType,
                    QuestionTypeMasterId = sourceQuestion.QuestionTypeMasterId,
                    IsRequired = sourceQuestion.IsRequired,
                    DisplayOrder = sourceQuestion.DisplayOrder,
                    Options = sourceQuestion.Options,
                    ValidationRules = sourceQuestion.ValidationRules,
                    OrganizationId = CurrentOrganizationId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Add(newQuestion);
                await _context.SaveChangesAsync();

                // Copy question attributes
                foreach (var sourceAttribute in sourceQuestion.QuestionAttributes)
                {
                    var newQuestionAttribute = new QuestionQuestionAttribute
                    {
                        QuestionId = newQuestion.Id,
                        QuestionAttributeId = sourceAttribute.QuestionAttributeId,
                        IsPrimary = sourceAttribute.IsPrimary,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Add(newQuestionAttribute);
                }
            }

            await _context.SaveChangesAsync();
            return newQuestionnaire;
        }

        private bool QuestionnaireExists(int id)
        {
            return _context.Questionnaires.Any(e => e.Id == id);
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }
    }
} 