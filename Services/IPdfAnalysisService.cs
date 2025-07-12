using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Services
{
    public interface IPdfAnalysisService
    {
        Task<PdfAnalysisResult> AnalyzePdfAsync(IFormFile pdfFile, int questionnaireId);
        Task<List<QuestionAnswerMatch>> ExtractAnswersAsync(string pdfText, List<Question> questions);
        Task<byte[]> GenerateAnalysisReportAsync(PdfAnalysisResult result);
    }

    public class PdfAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public List<QuestionAnswerMatch> Matches { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public int TotalQuestions { get; set; }
        public int MatchedQuestions { get; set; }
        public double AverageConfidence { get; set; }
    }

    public class QuestionAnswerMatch
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? ExtractedAnswer { get; set; }
        public double ConfidenceScore { get; set; }
        public string? SourceText { get; set; }
        public int? SourcePage { get; set; }
        public string? Reasoning { get; set; }
        public bool IsValid { get; set; } = true;
        public List<string> ValidationErrors { get; set; } = new();
    }
} 