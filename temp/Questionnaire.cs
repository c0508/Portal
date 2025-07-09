using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Questionnaire
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int OrganizationId { get; set; }

    public string Category { get; set; } = null!;

    public string CreatedByUserId { get; set; } = null!;

    public bool IsTemplate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<QuestionnaireVersion> QuestionnaireVersions { get; set; } = new List<QuestionnaireVersion>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
