using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class QuestionType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? InputType { get; set; }

    public bool RequiresOptions { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
