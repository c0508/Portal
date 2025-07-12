using ESGPlatform.Models.Entities;
using ESGPlatform.Services;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ESGPlatform.Models.ViewModels
{
    public class PdfAnalysisViewModel
    {
        public int QuestionnaireId { get; set; }
        public string QuestionnaireTitle { get; set; } = string.Empty;
        public List<Question> Questions { get; set; } = new();

        [Required]
        [Display(Name = "PDF File")]
        public IFormFile? PdfFile { get; set; }

        [Display(Name = "Analysis Notes")]
        public string? AnalysisNotes { get; set; }
    }

    public class PdfReviewViewModel
    {
        public int QuestionnaireId { get; set; }
        public PdfAnalysisResult AnalysisResult { get; set; } = new();
        public List<int> SelectedMatchIds { get; set; } = new();
    }

    public class PdfAnalysisResultViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int MatchedQuestions { get; set; }
        public double AverageConfidence { get; set; }
        public List<QuestionAnswerMatchViewModel> Matches { get; set; } = new();
    }

    public class QuestionAnswerMatchViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? ExtractedAnswer { get; set; }
        public double ConfidenceScore { get; set; }
        public string? SourceText { get; set; }
        public int? SourcePage { get; set; }
        public string? Reasoning { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsSelected { get; set; }
        
        // Display properties
        public string ConfidenceDisplay => $"{ConfidenceScore:P0}";
        public string ConfidenceColorClass => ConfidenceScore switch
        {
            >= 0.8 => "text-success",
            >= 0.6 => "text-warning",
            >= 0.4 => "text-info",
            _ => "text-danger"
        };
        
        public string StatusIconClass => IsValid switch
        {
            true when ConfidenceScore >= 0.8 => "bi-check-circle-fill text-success",
            true when ConfidenceScore >= 0.6 => "bi-check-circle text-warning",
            true when ConfidenceScore >= 0.4 => "bi-question-circle text-info",
            true => "bi-exclamation-circle text-danger",
            false => "bi-x-circle text-danger"
        };
    }
} 