using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace ESGPlatform.Models.ViewModels
{
    public class QuestionnaireCreateViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Copy functionality
        [Display(Name = "Copy from existing questionnaire")]
        public bool CopyFromExisting { get; set; } = false;

        [Display(Name = "Source questionnaire")]
        public int? SourceQuestionnaireId { get; set; }

        // Excel import functionality
        [Display(Name = "Import questions from Excel")]
        public bool ImportFromExcel { get; set; } = false;

        [Display(Name = "Excel file")]
        public IFormFile? ExcelFile { get; set; }

        public List<Questionnaire> AvailableQuestionnaires { get; set; } = new();
        
        // For preview of imported questions
        public List<ExcelQuestionPreviewViewModel> ImportedQuestions { get; set; } = new();
        public bool HasImportedQuestions => ImportedQuestions.Any();
    }

    public class ExcelQuestionPreviewViewModel
    {
        public int RowNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? HelpText { get; set; }
        public string? Section { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? Options { get; set; }
        public bool IsPercentage { get; set; } = false;
        public string? Unit { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => !ValidationErrors.Any();
    }
} 