using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class ResponseWorkflow
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public int CurrentStatus { get; set; }

    public DateTime? SubmittedForReviewAt { get; set; }

    public DateTime? ReviewStartedAt { get; set; }

    public DateTime? ReviewCompletedAt { get; set; }

    public string? CurrentReviewerId { get; set; }

    public int RevisionCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CurrentReviewer { get; set; }

    public virtual Response Response { get; set; } = null!;
}
