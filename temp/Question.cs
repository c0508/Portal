using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class Question
{
    public int Id { get; set; }

    public int QuestionnaireId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string? HelpText { get; set; }

    public int QuestionType { get; set; }

    public int DisplayOrder { get; set; }

    public int OrganizationId { get; set; }

    public bool IsRequired { get; set; }

    public string? Options { get; set; }

    public string? ValidationRules { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? QuestionTypeMasterId { get; set; }

    public string? Section { get; set; }

    public bool IsPercentage { get; set; }

    public string? Unit { get; set; }

    public virtual ICollection<Delegation> Delegations { get; set; } = new List<Delegation>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<QuestionAssignmentChange> QuestionAssignmentChanges { get; set; } = new List<QuestionAssignmentChange>();

    public virtual ICollection<QuestionAssignment> QuestionAssignments { get; set; } = new List<QuestionAssignment>();

    public virtual ICollection<QuestionChange> QuestionChanges { get; set; } = new List<QuestionChange>();

    public virtual ICollection<QuestionDependency> QuestionDependencyDependsOnQuestions { get; set; } = new List<QuestionDependency>();

    public virtual ICollection<QuestionDependency> QuestionDependencyQuestions { get; set; } = new List<QuestionDependency>();

    public virtual ICollection<QuestionQuestionAttribute> QuestionQuestionAttributes { get; set; } = new List<QuestionQuestionAttribute>();

    public virtual QuestionType? QuestionTypeMaster { get; set; }

    public virtual Questionnaire Questionnaire { get; set; } = null!;

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();

    public virtual ICollection<ReviewAssignment> ReviewAssignments { get; set; } = new List<ReviewAssignment>();

    public virtual ICollection<ReviewAuditLog> ReviewAuditLogs { get; set; } = new List<ReviewAuditLog>();
}
