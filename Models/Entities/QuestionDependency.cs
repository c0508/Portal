using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public enum DependencyConditionType
{
    Equals,
    NotEquals,
    IsAnswered,
    IsNotAnswered
}

public class QuestionDependency
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; } // The question that will be shown/hidden

    [Required]
    public int DependsOnQuestionId { get; set; } // The question this depends on

    [Required]
    public DependencyConditionType ConditionType { get; set; } = DependencyConditionType.Equals;

    [StringLength(500)]
    public string? ConditionValue { get; set; } // The value that triggers showing the question

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(450)]
    public string CreatedById { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(QuestionId))]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey(nameof(DependsOnQuestionId))]
    public virtual Question DependsOnQuestion { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;
} 