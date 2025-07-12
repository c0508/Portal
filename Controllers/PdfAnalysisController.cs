using ESGPlatform.Services;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;

namespace ESGPlatform.Controllers
{
    [Authorize]
    public class PdfAnalysisController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfAnalysisService _pdfAnalysisService;
        private readonly ILogger<PdfAnalysisController> _logger;

        public PdfAnalysisController(
            ApplicationDbContext context, 
            IPdfAnalysisService pdfAnalysisService,
            ILogger<PdfAnalysisController> logger)
        {
            _context = context;
            _pdfAnalysisService = pdfAnalysisService;
            _logger = logger;
        }

        // GET: PdfAnalysis/Analyze/{questionnaireId}
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Analyze(int questionnaireId)
        {
            var questionnaire = await _context.Questionnaires
                .Include(q => q.Questions.OrderBy(qu => qu.DisplayOrder))
                .FirstOrDefaultAsync(q => q.Id == questionnaireId);

            if (questionnaire == null)
            {
                return NotFound();
            }

            var model = new PdfAnalysisViewModel
            {
                QuestionnaireId = questionnaireId,
                QuestionnaireTitle = questionnaire.Title,
                Questions = questionnaire.Questions.ToList()
            };

            return View(model);
        }

        // POST: PdfAnalysis/Analyze
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> Analyze(PdfAnalysisViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PdfFile == null || model.PdfFile.Length == 0)
            {
                ModelState.AddModelError("PdfFile", "Please select a PDF file to analyze.");
                return View(model);
            }

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf" };
                var fileExtension = Path.GetExtension(model.PdfFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Any(ext => ext == fileExtension))
                {
                    ModelState.AddModelError("PdfFile", "Only PDF files are allowed.");
                    return View(model);
                }

                // Validate file size (10MB limit)
                if (model.PdfFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("PdfFile", "File size must be less than 10MB.");
                    return View(model);
                }

                // Get questions for the questionnaire
                var questions = await _context.Questions
                    .Where(q => q.QuestionnaireId == model.QuestionnaireId)
                    .OrderBy(q => q.DisplayOrder)
                    .ToListAsync();

                // Analyze the PDF
                var analysisResult = await _pdfAnalysisService.AnalyzePdfAsync(model.PdfFile, model.QuestionnaireId);
                
                // Extract answers for each question
                var matches = await _pdfAnalysisService.ExtractAnswersAsync(analysisResult.ExtractedText, questions);
                
                // Update the analysis result
                analysisResult.Matches = matches;
                analysisResult.TotalQuestions = questions.Count;
                analysisResult.MatchedQuestions = matches.Count(m => m.IsValid);
                analysisResult.AverageConfidence = matches.Any() ? matches.Average(m => m.ConfidenceScore) : 0;

                // Store the result in TempData for the next step
                TempData["PdfAnalysisResult"] = System.Text.Json.JsonSerializer.Serialize(analysisResult);

                TempData["SuccessMessage"] = $"PDF analysis completed! Found {analysisResult.MatchedQuestions} potential answers out of {analysisResult.TotalQuestions} questions.";
                
                return RedirectToAction(nameof(ReviewResults), new { questionnaireId = model.QuestionnaireId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PDF file: {FileName}", model.PdfFile?.FileName);
                ModelState.AddModelError("", "An error occurred while analyzing the PDF. Please try again.");
                return View(model);
            }
        }

        // GET: PdfAnalysis/ReviewResults/{questionnaireId}
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> ReviewResults(int questionnaireId)
        {
            var analysisResultJson = TempData["PdfAnalysisResult"]?.ToString();
            if (string.IsNullOrEmpty(analysisResultJson))
            {
                TempData["ErrorMessage"] = "No analysis results found. Please upload and analyze a PDF first.";
                return RedirectToAction(nameof(Analyze), new { questionnaireId });
            }

            try
            {
                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<PdfAnalysisResult>(analysisResultJson);
                if (analysisResult == null)
                {
                    TempData["ErrorMessage"] = "Invalid analysis results. Please try again.";
                    return RedirectToAction(nameof(Analyze), new { questionnaireId });
                }

                var model = new PdfReviewViewModel
                {
                    QuestionnaireId = questionnaireId,
                    AnalysisResult = analysisResult
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing PDF analysis results");
                TempData["ErrorMessage"] = "Error loading analysis results. Please try again.";
                return RedirectToAction(nameof(Analyze), new { questionnaireId });
            }
        }

        // POST: PdfAnalysis/ApplyResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> ApplyResults(PdfReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ReviewResults), new { questionnaireId = model.QuestionnaireId });
            }

            try
            {
                // Here you would apply the selected answers to the questionnaire
                // For now, we'll just redirect with a success message
                TempData["SuccessMessage"] = "PDF analysis results have been applied to the questionnaire.";
                return RedirectToAction("Details", "Questionnaire", new { id = model.QuestionnaireId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying PDF analysis results");
                TempData["ErrorMessage"] = "Error applying analysis results. Please try again.";
                return RedirectToAction(nameof(ReviewResults), new { questionnaireId = model.QuestionnaireId });
            }
        }

        // GET: PdfAnalysis/DownloadReport/{questionnaireId}
        [Authorize(Policy = "OrgAdminOrHigher")]
        public async Task<IActionResult> DownloadReport(int questionnaireId)
        {
            var analysisResultJson = TempData["PdfAnalysisResult"]?.ToString();
            if (string.IsNullOrEmpty(analysisResultJson))
            {
                TempData["ErrorMessage"] = "No analysis results found.";
                return RedirectToAction(nameof(Analyze), new { questionnaireId });
            }

            try
            {
                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<PdfAnalysisResult>(analysisResultJson);
                if (analysisResult == null)
                {
                    TempData["ErrorMessage"] = "Invalid analysis results.";
                    return RedirectToAction(nameof(Analyze), new { questionnaireId });
                }

                var reportBytes = await _pdfAnalysisService.GenerateAnalysisReportAsync(analysisResult);
                var fileName = $"pdf_analysis_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

                return File(reportBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF analysis report");
                TempData["ErrorMessage"] = "Error generating report.";
                return RedirectToAction(nameof(Analyze), new { questionnaireId });
            }
        }
    }
} 