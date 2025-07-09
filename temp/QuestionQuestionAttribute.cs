using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionQuestionAttribute
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int QuestionAttributeId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual QuestionAttribute QuestionAttribute { get; set; } = null!;
}
