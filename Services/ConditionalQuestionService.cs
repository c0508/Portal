using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Services;

public interface IConditionalQuestionService
{
    Task<List<QuestionDependency>> GetQuestionDependenciesAsync(int questionnaireId);
    Task<bool> ShouldShowQuestionAsync(int questionId, int campaignAssignmentId);
    Task<Dictionary<int, bool>> GetQuestionVisibilityAsync(List<int> questionIds, int campaignAssignmentId);
}

public class ConditionalQuestionService : IConditionalQuestionService
{
    private readonly ApplicationDbContext _context;

    public ConditionalQuestionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestionDependency>> GetQuestionDependenciesAsync(int questionnaireId)
    {
        return await _context.QuestionDependencies
            .Include(qd => qd.Question)
            .Include(qd => qd.DependsOnQuestion)
            .Where(qd => qd.IsActive && qd.Question.QuestionnaireId == questionnaireId)
            .ToListAsync();
    }

    public async Task<bool> ShouldShowQuestionAsync(int questionId, int campaignAssignmentId)
    {
        var dependencies = await _context.QuestionDependencies
            .Include(qd => qd.DependsOnQuestion)
            .Where(qd => qd.QuestionId == questionId && qd.IsActive)
            .ToListAsync();

        if (!dependencies.Any())
            return true; // No dependencies = always show

        // Check all dependencies (all must be satisfied)
        foreach (var dependency in dependencies)
        {
            var dependsOnResponse = await _context.Responses
                .FirstOrDefaultAsync(r => r.CampaignAssignmentId == campaignAssignmentId && 
                                        r.QuestionId == dependency.DependsOnQuestionId);

            var isSatisfied = IsDependencySatisfied(dependency, dependsOnResponse);
            
            // Debug logging
            var responseValue = dependsOnResponse != null ? GetResponseValue(dependsOnResponse) : "NULL";
            Console.WriteLine($"Question {questionId} depends on Q{dependency.DependsOnQuestionId}. " +
                            $"Condition: {dependency.ConditionType} '{dependency.ConditionValue}'. " +
                            $"Response: '{responseValue}'. Satisfied: {isSatisfied}");

            if (!isSatisfied)
                return false;
        }

        return true;
    }

    public async Task<Dictionary<int, bool>> GetQuestionVisibilityAsync(List<int> questionIds, int campaignAssignmentId)
    {
        var visibility = new Dictionary<int, bool>();
        
        foreach (var questionId in questionIds)
        {
            visibility[questionId] = await ShouldShowQuestionAsync(questionId, campaignAssignmentId);
        }

        return visibility;
    }

    private bool IsDependencySatisfied(QuestionDependency dependency, Response? response)
    {
        switch (dependency.ConditionType)
        {
            case DependencyConditionType.Equals:
                if (response == null) return false;
                
                // Check various response value types
                var responseValue = GetResponseValue(response);
                return !string.IsNullOrEmpty(responseValue) && 
                       responseValue.Equals(dependency.ConditionValue, StringComparison.OrdinalIgnoreCase);

            case DependencyConditionType.NotEquals:
                if (response == null) return true;
                
                var responseValueNotEquals = GetResponseValue(response);
                return string.IsNullOrEmpty(responseValueNotEquals) || 
                       !responseValueNotEquals.Equals(dependency.ConditionValue, StringComparison.OrdinalIgnoreCase);

            case DependencyConditionType.IsAnswered:
                return response != null && !string.IsNullOrEmpty(GetResponseValue(response));

            case DependencyConditionType.IsNotAnswered:
                return response == null || string.IsNullOrEmpty(GetResponseValue(response));

            default:
                return true;
        }
    }

    private string? GetResponseValue(Response response)
    {
        // Handle different response types
        if (!string.IsNullOrEmpty(response.TextValue))
            return response.TextValue;
        
        if (response.BooleanValue.HasValue)
            return response.BooleanValue.Value ? "Yes" : "No";
            
        if (response.NumericValue.HasValue)
            return response.NumericValue.Value.ToString();
            
        if (response.DateValue.HasValue)
            return response.DateValue.Value.ToString("yyyy-MM-dd");
            
        if (!string.IsNullOrEmpty(response.SelectedValues))
            return response.SelectedValues; // For multi-select, this is JSON
            
        return null;
    }
} 