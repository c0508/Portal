using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

public class QuestionChangeHistoryViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string? Section { get; set; }
    public List<QuestionChangeViewModel> Changes { get; set; } = new List<QuestionChangeViewModel>();
}

public class QuestionChangeViewModel
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public string ChangedByEmail { get; set; } = string.Empty;
    
    // Helper properties for display
    public string FormattedFieldName => FormatFieldName(FieldName);
    public string FormattedOldValue => TruncateValue(OldValue);
    public string FormattedNewValue => TruncateValue(NewValue);
    public string ChangeType => string.IsNullOrEmpty(OldValue) ? "Added" : 
                               string.IsNullOrEmpty(NewValue) ? "Removed" : "Modified";
    
    private static string FormatFieldName(string fieldName)
    {
        return fieldName switch
        {
            "QuestionText" => "Question Text",
            "HelpText" => "Help Text",
            "Section" => "Section",
            "QuestionType" => "Question Type",
            "IsRequired" => "Required",
            "Options" => "Options",
            "ValidationRules" => "Validation Rules",
            "DisplayOrder" => "Display Order",
            _ => fieldName
        };
    }
    
    private static string TruncateValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        return value.Length > 100 ? value[..97] + "..." : value;
    }
}

public class QuestionChangeFilterViewModel
{
    [Display(Name = "Search Questions")]
    public string? SearchText { get; set; }
    
    [Display(Name = "Questionnaire")]
    public int? QuestionnaireId { get; set; }
    
    [Display(Name = "Section")]
    public string? Section { get; set; }
    
    [Display(Name = "Changed By")]
    public string? ChangedBy { get; set; }
    
    [Display(Name = "Field Name")]
    public string? FieldName { get; set; }
    
    [Display(Name = "From Date")]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }
    
    [Display(Name = "To Date")]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }
    
    // For dropdown population
    public List<SelectListItem> Questionnaires { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> Sections { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> Users { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FieldNames { get; set; } = new List<SelectListItem>();
}

public class QuestionChangesSummaryViewModel
{
    public int TotalChanges { get; set; }
    public int QuestionsModified { get; set; }
    public int ChangesToday { get; set; }
    public int ChangesThisWeek { get; set; }
    public List<TopChangerViewModel> TopChangers { get; set; } = new List<TopChangerViewModel>();
    public List<MostChangedQuestionViewModel> MostChangedQuestions { get; set; } = new List<MostChangedQuestionViewModel>();
}

public class TopChangerViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int ChangeCount { get; set; }
}

public class MostChangedQuestionViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string? Section { get; set; }
    public int ChangeCount { get; set; }
    public DateTime LastChanged { get; set; }
} 