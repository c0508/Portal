using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum QuestionType
{
    Text,
    LongText,
    Number,
    Select,
    MultiSelect,
    Radio,
    Checkbox,
    YesNo,
    Date,
    FileUpload
}

public class QuestionTypeMaster
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty; // e.g., "Text", "LongText", etc.

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Text Input", "Long Text Area"

    [StringLength(500)]
    public string? Description { get; set; } // e.g., "Single line text input for short answers"

    [StringLength(50)]
    public string? InputType { get; set; } // e.g., "text", "textarea", "number", "date", "select", "radio", "checkbox", "file"

    public bool RequiresOptions { get; set; } = false; // True for Select, Radio, Checkbox, etc.

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0; // For ordering in dropdowns

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionnaireId { get; set; }

    [Required]
    [StringLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [StringLength(500)]
    public string? HelpText { get; set; }

    [StringLength(100)]
    [Display(Name = "Section")]
    public string? Section { get; set; }

    [Required]
    public QuestionType QuestionType { get; set; }

    // New foreign key to master table (nullable for migration compatibility)
    public int? QuestionTypeMasterId { get; set; }

    public int DisplayOrder { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    public bool IsRequired { get; set; } = false;

    // For select/multiselect questions - JSON array of options
    [StringLength(2000)]
    public string? Options { get; set; }

    // Validation rules in JSON format
    [StringLength(1000)]
    public string? ValidationRules { get; set; }

    // Numeric question enhancements
    public bool IsPercentage { get; set; } = false; // For percentage questions (0-100 validation)
    
    [StringLength(50)]
    public string? Unit { get; set; } // For numeric questions with units (e.g., "MWh", "kg", "km")

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(QuestionnaireId))]
    public virtual Questionnaire Questionnaire { get; set; } = null!;

    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(QuestionTypeMasterId))]
    public virtual QuestionTypeMaster? QuestionTypeMaster { get; set; }

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();
    public virtual ICollection<Delegation> Delegations { get; set; } = new List<Delegation>();
    public virtual ICollection<QuestionChange> Changes { get; set; } = new List<QuestionChange>();
    
    // Question Attributes - Many-to-Many relationship
    public virtual ICollection<QuestionQuestionAttribute> QuestionAttributes { get; set; } = new List<QuestionQuestionAttribute>();
    
    // Conditional Logic - Dependencies
    public virtual ICollection<QuestionDependency> Dependencies { get; set; } = new List<QuestionDependency>();
    public virtual ICollection<QuestionDependency> DependentQuestions { get; set; } = new List<QuestionDependency>();
}

// Master table for question attributes (e.g., GRI standards, SASB standards, etc.)
public class QuestionAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty; // e.g., "GRI305-1", "SASB-EM-RM-130a.1"

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty; // e.g., "Direct (Scope 1) GHG emissions"

    [StringLength(1000)]
    public string? Description { get; set; } // Detailed description of what this attribute represents

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // e.g., "GRI", "SASB", "TCFD", "IFRS", "Custom"

    [StringLength(100)]
    public string? Subcategory { get; set; } // e.g., "Environmental", "Social", "Governance"

    [StringLength(500)]
    public string? Tags { get; set; } // Comma-separated tags for additional categorization

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0; // For ordering in lists

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<QuestionQuestionAttribute> QuestionAttributes { get; set; } = new List<QuestionQuestionAttribute>();
}

// Master table for units (e.g., MWh, kg, km, etc.)
public class Unit
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty; // e.g., "MWh", "kg", "km"

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Megawatt Hours", "Kilograms", "Kilometers"

    [StringLength(500)]
    public string? Description { get; set; } // Detailed description

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // e.g., "Energy", "Weight", "Distance", "Volume", "Time"

    [StringLength(10)]
    public string? Symbol { get; set; } // e.g., "MWh", "kg", "km" for display

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0; // For ordering within category

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Junction table for Question-QuestionAttribute many-to-many relationship
public class QuestionQuestionAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [Required]
    public int QuestionAttributeId { get; set; }

    public bool IsPrimary { get; set; } = false; // Mark one attribute as primary for the question

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(QuestionAttributeId))]
    public virtual QuestionAttribute QuestionAttribute { get; set; } = null!;
}

public class QuestionChange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [Required]
    [StringLength(450)]
    public string ChangedById { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FieldName { get; set; } // Which field was changed (e.g., "QuestionText", "HelpText", "Section")

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValue { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValue { get; set; }

    [StringLength(500)]
    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(ChangedById))]
    public virtual User ChangedBy { get; set; } = null!;
} 