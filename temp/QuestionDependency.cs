using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionDependency
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int DependsOnQuestionId { get; set; }

    public int ConditionType { get; set; }

    public string? ConditionValue { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedById { get; set; } = null!;

    public virtual User CreatedBy { get; set; } = null!;

    public virtual Question DependsOnQuestion { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
