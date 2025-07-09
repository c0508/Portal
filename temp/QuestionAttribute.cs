using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionAttribute
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public string? Subcategory { get; set; }

    public string? Tags { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<QuestionQuestionAttribute> QuestionQuestionAttributes { get; set; } = new List<QuestionQuestionAttribute>();
}
