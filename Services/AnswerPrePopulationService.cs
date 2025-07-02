using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ESGPlatform.Services;

public interface IAnswerPrePopulationService
{
    Task<List<PreviousCampaignSummary>> GetAvailablePreviousCampaignsAsync(int campaignAssignmentId);
    Task<QuestionMatchingResult> PreviewQuestionMatchingAsync(int currentCampaignAssignmentId, int previousCampaignAssignmentId);
    Task<PrePopulationResult> PrePopulateAnswersAsync(int currentCampaignAssignmentId, int previousCampaignAssignmentId, string userId);
    Task<bool> AcceptPrePopulatedAnswerAsync(int responseId, string userId);
    Task<bool> RejectPrePopulatedAnswerAsync(int responseId, string userId);
    Task<int> AcceptAllPrePopulatedAnswersAsync(int campaignAssignmentId, string userId);
    Task<int> RejectAllPrePopulatedAnswersAsync(int campaignAssignmentId, string userId);
}

public class AnswerPrePopulationService : IAnswerPrePopulationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnswerPrePopulationService> _logger;
    private readonly IResponseWorkflowService _responseWorkflowService;

    public AnswerPrePopulationService(ApplicationDbContext context, ILogger<AnswerPrePopulationService> logger, IResponseWorkflowService responseWorkflowService)
    {
        _context = context;
        _logger = logger;
        _responseWorkflowService = responseWorkflowService;
    }

    public async Task<List<PreviousCampaignSummary>> GetAvailablePreviousCampaignsAsync(int campaignAssignmentId)
    {
        var currentAssignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        if (currentAssignment == null)
            throw new ArgumentException($"Campaign assignment {campaignAssignmentId} not found");

        // Find previous campaigns for the same target organization
        var previousCampaigns = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
            .Include(ca => ca.Responses)
            .Where(ca => ca.TargetOrganizationId == currentAssignment.TargetOrganizationId &&
                        ca.Id != campaignAssignmentId &&
                        ca.Status == AssignmentStatus.Submitted || ca.Status == AssignmentStatus.Approved)
            .Where(ca => ca.Campaign.ReportingPeriodEnd < currentAssignment.Campaign.ReportingPeriodStart)
            .OrderByDescending(ca => ca.Campaign.ReportingPeriodEnd)
            .ToListAsync();

        return previousCampaigns.Select(ca => new PreviousCampaignSummary
        {
            CampaignAssignmentId = ca.Id,
            CampaignName = ca.Campaign.Name,
            QuestionnaireName = ca.QuestionnaireVersion.Questionnaire.Title,
            ReportingPeriodStart = ca.Campaign.ReportingPeriodStart,
            ReportingPeriodEnd = ca.Campaign.ReportingPeriodEnd,
            ResponseCount = ca.Responses.Count,
            SubmittedAt = ca.SubmittedAt
        }).ToList();
    }

    public async Task<QuestionMatchingResult> PreviewQuestionMatchingAsync(int currentCampaignAssignmentId, int previousCampaignAssignmentId)
    {
        var currentQuestions = await GetQuestionsForAssignmentAsync(currentCampaignAssignmentId);
        var previousQuestions = await GetQuestionsWithResponsesAsync(previousCampaignAssignmentId);

        var matchingResult = new QuestionMatchingResult
        {
            TotalCurrentQuestions = currentQuestions.Count,
            TotalPreviousQuestions = previousQuestions.Count
        };

        foreach (var currentQuestion in currentQuestions)
        {
            var match = FindBestMatch(currentQuestion, previousQuestions);
            
            if (match != null)
            {
                matchingResult.Matches.Add(new QuestionMatch
                {
                    CurrentQuestionId = currentQuestion.Id,
                    CurrentQuestionText = currentQuestion.QuestionText,
                    CurrentSection = currentQuestion.Section,
                    PreviousQuestionId = match.Question.Id,
                    PreviousQuestionText = match.Question.QuestionText,
                    PreviousSection = match.Question.Section,
                    MatchType = DetermineMatchType(currentQuestion, match.Question),
                    ConfidenceScore = CalculateConfidenceScore(currentQuestion, match.Question),
                    HasPreviousResponse = match.Response != null,
                    PreviousResponseValue = GetResponseDisplayValue(match.Response)
                });
            }
            else
            {
                matchingResult.UnmatchedQuestions.Add(new UnmatchedQuestion
                {
                    QuestionId = currentQuestion.Id,
                    QuestionText = currentQuestion.QuestionText,
                    Section = currentQuestion.Section,
                    QuestionType = currentQuestion.QuestionType.ToString()
                });
            }
        }

        return matchingResult;
    }

    public async Task<PrePopulationResult> PrePopulateAnswersAsync(int currentCampaignAssignmentId, int previousCampaignAssignmentId, string userId)
    {
        var currentQuestions = await GetQuestionsForAssignmentAsync(currentCampaignAssignmentId);
        var previousQuestions = await GetQuestionsWithResponsesAsync(previousCampaignAssignmentId);

        var result = new PrePopulationResult();
        var responsesToCreate = new List<Response>();

        foreach (var currentQuestion in currentQuestions)
        {
            var match = FindBestMatch(currentQuestion, previousQuestions);
            
            if (match?.Response != null && CalculateConfidenceScore(currentQuestion, match.Question) >= 0.7) // Only high-confidence matches
            {
                // Check if response already exists
                var existingResponse = await _context.Responses
                    .FirstOrDefaultAsync(r => r.QuestionId == currentQuestion.Id && 
                                            r.CampaignAssignmentId == currentCampaignAssignmentId);

                if (existingResponse == null)
                {
                    var newResponse = new Response
                    {
                        QuestionId = currentQuestion.Id,
                        CampaignAssignmentId = currentCampaignAssignmentId,
                        ResponderId = userId,
                        TextValue = match.Response.TextValue,
                        NumericValue = match.Response.NumericValue,
                        DateValue = match.Response.DateValue,
                        BooleanValue = match.Response.BooleanValue,
                        SelectedValues = match.Response.SelectedValues,
                        IsPrePopulated = true,
                        SourceResponseId = match.Response.Id,
                        IsPrePopulatedAccepted = false
                    };

                    responsesToCreate.Add(newResponse);
                    result.PrePopulatedCount++;
                }
                else
                {
                    result.SkippedCount++; // Question already has a response
                }
            }
            else
            {
                result.UnmatchedCount++;
            }
        }

        if (responsesToCreate.Any())
        {
            _context.Responses.AddRange(responsesToCreate);
            await _context.SaveChangesAsync();

            // Update status for all pre-populated responses
            foreach (var response in responsesToCreate)
            {
                await _responseWorkflowService.UpdateStatusForPrePopulationAsync(response.Id, userId);
            }
        }

        _logger.LogInformation($"Pre-populated {result.PrePopulatedCount} answers for assignment {currentCampaignAssignmentId} from assignment {previousCampaignAssignmentId}");

        return result;
    }

    public async Task<bool> AcceptPrePopulatedAnswerAsync(int responseId, string userId)
    {
        var response = await _context.Responses.FindAsync(responseId);
        if (response?.IsPrePopulated == true && !response.IsPrePopulatedAccepted)
        {
            response.IsPrePopulatedAccepted = true;
            response.UpdatedAt = DateTime.UtcNow;
            
            // Log the acceptance
            var change = new ResponseChange
            {
                ResponseId = responseId,
                ChangedById = userId,
                OldValue = "Pre-populated (pending)",
                NewValue = "Pre-populated (accepted)",
                ChangeReason = "User accepted pre-populated answer"
            };
            _context.ResponseChanges.Add(change);
            
            await _context.SaveChangesAsync();

            // Update status to Answered since the pre-populated answer is now accepted
            await _responseWorkflowService.UpdateStatusForAnswerAsync(responseId, userId);
            
            return true;
        }
        return false;
    }

    public async Task<bool> RejectPrePopulatedAnswerAsync(int responseId, string userId)
    {
        var response = await _context.Responses.FindAsync(responseId);
        if (response?.IsPrePopulated == true)
        {
            _context.Responses.Remove(response);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Rejected pre-populated response {responseId} by user {userId}");
            return true;
        }
        return false;
    }

    public async Task<int> AcceptAllPrePopulatedAnswersAsync(int campaignAssignmentId, string userId)
    {
        var prePopulatedResponses = await _context.Responses
            .Where(r => r.CampaignAssignmentId == campaignAssignmentId && 
                       r.IsPrePopulated && 
                       !r.IsPrePopulatedAccepted)
            .ToListAsync();

        foreach (var response in prePopulatedResponses)
        {
            response.IsPrePopulatedAccepted = true;
            response.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return prePopulatedResponses.Count;
    }

    public async Task<int> RejectAllPrePopulatedAnswersAsync(int campaignAssignmentId, string userId)
    {
        var prePopulatedResponses = await _context.Responses
            .Where(r => r.CampaignAssignmentId == campaignAssignmentId && 
                       r.IsPrePopulated && 
                       !r.IsPrePopulatedAccepted)
            .ToListAsync();

        _context.Responses.RemoveRange(prePopulatedResponses);
        await _context.SaveChangesAsync();
        
        return prePopulatedResponses.Count;
    }

    // Private helper methods
    private async Task<List<Question>> GetQuestionsForAssignmentAsync(int campaignAssignmentId)
    {
        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        return assignment?.QuestionnaireVersion.Questionnaire.Questions.ToList() ?? new List<Question>();
    }

    private async Task<List<QuestionWithResponse>> GetQuestionsWithResponsesAsync(int campaignAssignmentId)
    {
        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .Include(ca => ca.Responses)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        if (assignment == null) return new List<QuestionWithResponse>();

        return assignment.QuestionnaireVersion.Questionnaire.Questions
            .Select(q => new QuestionWithResponse
            {
                Question = q,
                Response = assignment.Responses.FirstOrDefault(r => r.QuestionId == q.Id)
            })
            .ToList();
    }

    private QuestionWithResponse? FindBestMatch(Question currentQuestion, List<QuestionWithResponse> previousQuestions)
    {
        var candidates = previousQuestions
            .Where(pq => pq.Question.QuestionType == currentQuestion.QuestionType)
            .Select(pq => new
            {
                QuestionWithResponse = pq,
                Score = CalculateConfidenceScore(currentQuestion, pq.Question)
            })
            .Where(x => x.Score >= 0.5) // Minimum threshold
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return candidates?.QuestionWithResponse;
    }

    private double CalculateConfidenceScore(Question current, Question previous)
    {
        double score = 0.0;

        // Question type match (required)
        if (current.QuestionType != previous.QuestionType)
            return 0.0;

        score += 0.3; // Base score for matching type

        // Text similarity
        var textSimilarity = CalculateTextSimilarity(current.QuestionText, previous.QuestionText);
        score += textSimilarity * 0.5;

        // Section match
        if (!string.IsNullOrEmpty(current.Section) && !string.IsNullOrEmpty(previous.Section))
        {
            if (current.Section.Equals(previous.Section, StringComparison.OrdinalIgnoreCase))
                score += 0.2;
        }

        // Options similarity for select/radio questions
        if (HasOptions(current.QuestionType) && !string.IsNullOrEmpty(current.Options) && !string.IsNullOrEmpty(previous.Options))
        {
            var optionsSimilarity = CalculateOptionsSimilarity(current.Options, previous.Options);
            score += optionsSimilarity * 0.1;
        }

        return Math.Min(1.0, score);
    }

    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        // Simple similarity based on common words
        var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private double CalculateOptionsSimilarity(string options1, string options2)
    {
        try
        {
            var opts1 = JsonSerializer.Deserialize<string[]>(options1) ?? Array.Empty<string>();
            var opts2 = JsonSerializer.Deserialize<string[]>(options2) ?? Array.Empty<string>();

            var intersection = opts1.Intersect(opts2, StringComparer.OrdinalIgnoreCase).Count();
            var union = opts1.Union(opts2, StringComparer.OrdinalIgnoreCase).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private bool HasOptions(QuestionType questionType)
    {
        return questionType == QuestionType.Select || 
               questionType == QuestionType.MultiSelect || 
               questionType == QuestionType.Radio;
    }

    private MatchType DetermineMatchType(Question current, Question previous)
    {
        if (current.QuestionText.Equals(previous.QuestionText, StringComparison.OrdinalIgnoreCase))
            return MatchType.Exact;
        
        var similarity = CalculateTextSimilarity(current.QuestionText, previous.QuestionText);
        return similarity >= 0.8 ? MatchType.High : MatchType.Partial;
    }

    private string? GetResponseDisplayValue(Response? response)
    {
        if (response == null) return null;

        if (!string.IsNullOrEmpty(response.TextValue))
            return response.TextValue;
        
        if (response.BooleanValue.HasValue)
            return response.BooleanValue.Value ? "Yes" : "No";
        
        if (response.NumericValue.HasValue)
            return response.NumericValue.Value.ToString();
        
        if (response.DateValue.HasValue)
            return response.DateValue.Value.ToString("yyyy-MM-dd");
        
        if (!string.IsNullOrEmpty(response.SelectedValues))
            return response.SelectedValues;

        return null;
    }
}

// Supporting classes
public class PreviousCampaignSummary
{
    public int CampaignAssignmentId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public DateTime? ReportingPeriodStart { get; set; }
    public DateTime? ReportingPeriodEnd { get; set; }
    public int ResponseCount { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class QuestionMatchingResult
{
    public int TotalCurrentQuestions { get; set; }
    public int TotalPreviousQuestions { get; set; }
    public List<QuestionMatch> Matches { get; set; } = new();
    public List<UnmatchedQuestion> UnmatchedQuestions { get; set; } = new();
}

public class QuestionMatch
{
    public int CurrentQuestionId { get; set; }
    public string CurrentQuestionText { get; set; } = string.Empty;
    public string? CurrentSection { get; set; }
    public int PreviousQuestionId { get; set; }
    public string PreviousQuestionText { get; set; } = string.Empty;
    public string? PreviousSection { get; set; }
    public MatchType MatchType { get; set; }
    public double ConfidenceScore { get; set; }
    public bool HasPreviousResponse { get; set; }
    public string? PreviousResponseValue { get; set; }
}

public class UnmatchedQuestion
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string QuestionType { get; set; } = string.Empty;
}

public class PrePopulationResult
{
    public int PrePopulatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int UnmatchedCount { get; set; }
}

public class QuestionWithResponse
{
    public Question Question { get; set; } = null!;
    public Response? Response { get; set; }
}

public enum MatchType
{
    Exact,
    High,
    Partial
} 