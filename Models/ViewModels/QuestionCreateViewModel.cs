using System.ComponentModel.DataAnnotations;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels
{
    public class QuestionCreateViewModel
    {
        public int Id { get; set; }
        public int QuestionnaireId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Question Text")]
        public string QuestionText { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Help Text")]
        public string? HelpText { get; set; }

        [StringLength(100)]
        [Display(Name = "Section")]
        public string? Section { get; set; }

        [Required]
        [Display(Name = "Question Type")]
        public int QuestionTypeMasterId { get; set; }

        [Display(Name = "Required")]
        public bool IsRequired { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Options (one per line)")]
        public string? Options { get; set; }

        [Display(Name = "Validation Rules (JSON)")]
        public string? ValidationRules { get; set; }

        [Display(Name = "Is Percentage")]
        public bool IsPercentage { get; set; } = false;

        [StringLength(50)]
        [Display(Name = "Unit")]
        public string? Unit { get; set; }

        // Helper properties for UI
        public List<string> OptionsList => string.IsNullOrWhiteSpace(Options) 
            ? new List<string>() 
            : Options.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Check if the selected question type requires options
        public bool RequiresOptions => AvailableQuestionTypes
            .FirstOrDefault(qt => qt.Id == QuestionTypeMasterId)?.RequiresOptions ?? false;

        // For master table support
        public List<QuestionTypeMaster> AvailableQuestionTypes { get; set; } = new();

        public string QuestionnaireTitle { get; set; } = string.Empty;

        // Question Attributes
        [Display(Name = "Question Attributes")]
        public List<int> SelectedAttributeIds { get; set; } = new();
        
        public List<QuestionAttribute> AvailableAttributes { get; set; } = new();

        // Units for numeric questions
        public Dictionary<string, List<Unit>> AvailableUnits { get; set; } = new();

        // Helper method to get selected attributes grouped by category
        public Dictionary<string, List<QuestionAttribute>> SelectedAttributesByCategory => 
            AvailableAttributes
                .Where(a => SelectedAttributeIds.Contains(a.Id))
                .GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
    }
} 