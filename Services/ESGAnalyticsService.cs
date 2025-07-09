using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ESGPlatform.Services;

public interface IESGAnalyticsService
{
    Task<ESGPortfolioOverviewViewModel> GetPortfolioOverviewAsync(int platformOrganizationId, int? reportingYear = null);
    Task<ESGTemporalAnalysisViewModel> GetTemporalAnalysisAsync(int platformOrganizationId, List<int> reportingYears);
    Task<ESGSectorBenchmarkingViewModel> GetSectorBenchmarkingAsync(int platformOrganizationId, int reportingYear);
    Task<ESGCompanyDeepDiveViewModel> GetCompanyDeepDiveAsync(int platformOrganizationId, int companyId, int reportingYear);
    Task<ESGQuestionLevelAnalyticsViewModel> GetQuestionLevelAnalyticsAsync(int platformOrganizationId, int questionId, int reportingYear);
    Task<List<int>> GetAvailableReportingYearsAsync(int platformOrganizationId);
}

public class ESGAnalyticsService : IESGAnalyticsService
{
    private readonly ApplicationDbContext _context;
    
    public ESGAnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ESGPortfolioOverviewViewModel> GetPortfolioOverviewAsync(int platformOrganizationId, int? reportingYear = null)
    {
        // Get latest year if not specified
        if (!reportingYear.HasValue)
        {
            var availableYears = await GetAvailableReportingYearsAsync(platformOrganizationId);
            reportingYear = availableYears.FirstOrDefault();
            if (reportingYear == 0) throw new InvalidOperationException("No reporting years available");
        }

        var campaigns = await GetCampaignsByReportingYearAsync(platformOrganizationId, reportingYear.Value);
        var assignments = await GetCompletedAssignmentsAsync(campaigns.Select(c => c.Id).ToList());
        
        Console.WriteLine($"DEBUG: Reporting year {reportingYear.Value}, Found {campaigns.Count} campaigns, {assignments.Count} assignments");
        
        var viewModel = new ESGPortfolioOverviewViewModel
        {
            ReportingYear = reportingYear.Value,
            TotalCompanies = assignments.Count,
            CompletionRate = CalculateCompletionRate(assignments),
            PolicyAdoptionRates = await CalculatePolicyAdoptionRatesAsync(assignments),
            QuantitativeBenchmarks = await CalculateQuantitativeBenchmarksAsync(assignments),
            SectorBreakdown = await CalculateSectorBreakdownAsync(assignments),
            TopPerformers = await IdentifyTopPerformersAsync(assignments, 3),
            ESGMaturityDistribution = await CalculateESGMaturityDistributionAsync(assignments)
        };

        return viewModel;
    }

    public async Task<ESGTemporalAnalysisViewModel> GetTemporalAnalysisAsync(int platformOrganizationId, List<int> reportingYears)
    {
        var viewModel = new ESGTemporalAnalysisViewModel
        {
            ReportingYears = reportingYears.OrderBy(y => y).ToList(),
            PolicyAdoptionTrends = new Dictionary<string, List<decimal>>(),
            QuantitativeMetricTrends = new Dictionary<string, List<decimal>>(),
            CompanyProgressAnalysis = new List<ESGCompanyProgressViewModel>()
        };

        foreach (var year in reportingYears)
        {
            var campaigns = await GetCampaignsByReportingYearAsync(platformOrganizationId, year);
            var assignments = await GetCompletedAssignmentsAsync(campaigns.Select(c => c.Id).ToList());
            
            // Policy adoption trends
            var policyRates = await CalculatePolicyAdoptionRatesAsync(assignments);
            foreach (var policy in policyRates)
            {
                if (!viewModel.PolicyAdoptionTrends.ContainsKey(policy.Key))
                    viewModel.PolicyAdoptionTrends[policy.Key] = new List<decimal>();
                viewModel.PolicyAdoptionTrends[policy.Key].Add(policy.Value);
            }

            // Quantitative metric trends
            var quantMetrics = await CalculateQuantitativeMetricAveragesAsync(assignments);
            foreach (var metric in quantMetrics)
            {
                if (!viewModel.QuantitativeMetricTrends.ContainsKey(metric.Key))
                    viewModel.QuantitativeMetricTrends[metric.Key] = new List<decimal>();
                viewModel.QuantitativeMetricTrends[metric.Key].Add(metric.Value);
            }
        }

        // Calculate company-level progress
        viewModel.CompanyProgressAnalysis = CalculateCompanyProgressAsync(platformOrganizationId, reportingYears);

        return viewModel;
    }

    public async Task<ESGSectorBenchmarkingViewModel> GetSectorBenchmarkingAsync(int platformOrganizationId, int reportingYear)
    {
        var campaigns = await GetCampaignsByReportingYearAsync(platformOrganizationId, reportingYear);
        var assignments = await GetCompletedAssignmentsAsync(campaigns.Select(c => c.Id).ToList());

        var sectorGroups = assignments.GroupBy(a => GetCompanySector(a));
        
        var viewModel = new ESGSectorBenchmarkingViewModel
        {
            ReportingYear = reportingYear,
            SectorComparisons = new List<ESGSectorComparisonViewModel>()
        };

        foreach (var sectorGroup in sectorGroups)
        {
            var sectorAssignments = sectorGroup.ToList();
            var sectorComparison = new ESGSectorComparisonViewModel
            {
                SectorName = sectorGroup.Key,
                CompanyCount = sectorAssignments.Count,
                PolicyAdoptionRates = await CalculatePolicyAdoptionRatesAsync(sectorAssignments),
                QuantitativeBenchmarks = await CalculateQuantitativeBenchmarksAsync(sectorAssignments),
                AverageESGScore = await CalculateAverageESGScoreAsync(sectorAssignments),
                TopPerformingCompanies = await IdentifyTopPerformersAsync(sectorAssignments, 2)
            };
            
            viewModel.SectorComparisons.Add(sectorComparison);
        }

        return viewModel;
    }

    public async Task<ESGCompanyDeepDiveViewModel> GetCompanyDeepDiveAsync(int platformOrganizationId, int companyId, int reportingYear)
    {
        var campaigns = await GetCampaignsByReportingYearAsync(platformOrganizationId, reportingYear);
        var companyAssignment = await GetCompanyAssignmentAsync(campaigns.Select(c => c.Id).ToList(), companyId);
        
        if (companyAssignment == null)
            throw new InvalidOperationException($"No assignment found for company {companyId} in {reportingYear}");

        var allAssignments = await GetCompletedAssignmentsAsync(campaigns.Select(c => c.Id).ToList());
        var companySector = GetCompanySector(companyAssignment);
        var peerAssignments = allAssignments.Where(a => GetCompanySector(a) == companySector && a.Id != companyAssignment.Id).ToList();

        var viewModel = new ESGCompanyDeepDiveViewModel
        {
            CompanyId = companyId,
            CompanyName = companyAssignment.TargetOrganization.Name,
            ReportingYear = reportingYear,
            Sector = companySector,
            CompanyResponses = await GetCompanyResponsesAsync(companyAssignment.Id),
            PeerComparison = CalculatePeerComparisonAsync(companyAssignment, peerAssignments),
            PortfolioComparison = CalculatePortfolioComparisonAsync(companyAssignment, allAssignments),
            HistoricalTrends = CalculateCompanyHistoricalTrendsAsync(platformOrganizationId, companyId),
            ESGScore = await CalculateCompanyESGScoreAsync(companyAssignment),
            StrengthsAndGaps = IdentifyStrengthsAndGapsAsync(companyAssignment, peerAssignments)
        };

        return viewModel;
    }

    public async Task<ESGQuestionLevelAnalyticsViewModel> GetQuestionLevelAnalyticsAsync(int platformOrganizationId, int questionId, int reportingYear)
    {
        var campaigns = await GetCampaignsByReportingYearAsync(platformOrganizationId, reportingYear);
        var assignments = await GetCompletedAssignmentsAsync(campaigns.Select(c => c.Id).ToList());
        
        var question = await _context.Questions.FindAsync(questionId);
        if (question == null) throw new InvalidOperationException($"Question {questionId} not found");

        var responses = await GetQuestionResponsesAsync(assignments.Select(a => a.Id).ToList(), questionId);

        var viewModel = new ESGQuestionLevelAnalyticsViewModel
        {
            QuestionId = questionId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            ReportingYear = reportingYear,
            TotalResponses = responses.Count,
            ResponseRate = (decimal)responses.Count / assignments.Count * 100
        };

        // Analyze responses based on question type
        switch (question.QuestionType)
        {
            case QuestionType.YesNo:
                viewModel.BooleanAnalysis = AnalyzeBooleanResponses(responses);
                break;
            case QuestionType.Number:
                viewModel.NumericAnalysis = AnalyzeNumericResponses(responses, question.IsPercentage, question.Unit);
                break;
            case QuestionType.MultiSelect:
                viewModel.MultiSelectAnalysis = AnalyzeMultiSelectResponses(responses, question.Options);
                break;
            case QuestionType.Text:
            case QuestionType.LongText:
                viewModel.TextAnalysis = AnalyzeTextResponses(responses);
                break;
        }

        // Sector breakdown
        viewModel.SectorBreakdown = AnalyzeResponsesBySectorAsync(responses, assignments);

        return viewModel;
    }

    public async Task<List<int>> GetAvailableReportingYearsAsync(int platformOrganizationId)
    {
        return await _context.Campaigns
            .Where(c => c.OrganizationId == platformOrganizationId && c.ReportingPeriodStart.HasValue)
            .Select(c => c.ReportingPeriodStart!.Value.Year + 1) // Reporting year is data year + 1
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    // Private helper methods for data retrieval and calculations
    
    private async Task<List<Campaign>> GetCampaignsByReportingYearAsync(int platformOrganizationId, int reportingYear)
    {
        var dataYear = reportingYear - 1; // Convert reporting year back to data year
        
        return await _context.Campaigns
            .Where(c => c.OrganizationId == platformOrganizationId 
                       && c.ReportingPeriodStart.HasValue 
                       && c.ReportingPeriodStart.Value.Year == dataYear)
            .ToListAsync();
    }

    private async Task<List<CampaignAssignment>> GetCompletedAssignmentsAsync(List<int> campaignIds)
    {
        return await _context.CampaignAssignments
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
            .Include(ca => ca.OrganizationRelationship)
                .ThenInclude(or => or!.Attributes)
            .Where(ca => campaignIds.Contains(ca.CampaignId) 
                        && ca.Status == AssignmentStatus.Approved)
            .ToListAsync();
    }

    private async Task<CampaignAssignment?> GetCompanyAssignmentAsync(List<int> campaignIds, int companyId)
    {
        return await _context.CampaignAssignments
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
            .Include(ca => ca.OrganizationRelationship)
                .ThenInclude(or => or!.Attributes)
            .FirstOrDefaultAsync(ca => campaignIds.Contains(ca.CampaignId) 
                                     && ca.TargetOrganizationId == companyId);
    }

    private string GetCompanySector(CampaignAssignment assignment)
    {
        return assignment.OrganizationRelationship?.Attributes
            .FirstOrDefault(a => a.AttributeType == "SECTOR")?.AttributeValue ?? "Unknown";
    }

    private decimal CalculateCompletionRate(List<CampaignAssignment> assignments)
    {
        if (!assignments.Any()) return 0;
        var completed = assignments.Count(a => a.Status == AssignmentStatus.Approved);
        return (decimal)completed / assignments.Count * 100;
    }

    private async Task<Dictionary<string, decimal>> CalculatePolicyAdoptionRatesAsync(List<CampaignAssignment> assignments)
    {
        var assignmentIds = assignments.Select(a => a.Id).ToList();
        
        var policyResponses = await _context.Responses
            .Include(r => r.Question)
            .Where(r => assignmentIds.Contains(r.CampaignAssignmentId) 
                       && r.Question.QuestionType == QuestionType.YesNo 
                       && r.BooleanValue.HasValue)
            .ToListAsync();

        Console.WriteLine($"DEBUG: Policy calculation - Found {policyResponses.Count} boolean responses for {assignmentIds.Count} assignments");

        return policyResponses
            .GroupBy(r => r.Question.QuestionText)
            .ToDictionary(
                g => g.Key,
                g => g.Count(r => r.BooleanValue == true) / (decimal)g.Count() * 100
            );
    }

    private async Task<Dictionary<string, decimal>> CalculateQuantitativeBenchmarksAsync(List<CampaignAssignment> assignments)
    {
        var assignmentIds = assignments.Select(a => a.Id).ToList();
        
        var numericResponses = await _context.Responses
            .Include(r => r.Question)
            .Where(r => assignmentIds.Contains(r.CampaignAssignmentId) 
                       && r.Question.QuestionType == QuestionType.Number 
                       && r.NumericValue.HasValue)
            .ToListAsync();

        return numericResponses
            .GroupBy(r => r.Question.QuestionText)
            .ToDictionary(
                g => g.Key,
                g => g.Average(r => r.NumericValue!.Value)
            );
    }

    private async Task<Dictionary<string, decimal>> CalculateQuantitativeMetricAveragesAsync(List<CampaignAssignment> assignments)
    {
        return await CalculateQuantitativeBenchmarksAsync(assignments);
    }

    private async Task<Dictionary<string, int>> CalculateSectorBreakdownAsync(List<CampaignAssignment> assignments)
    {
        return assignments
            .GroupBy(a => GetCompanySector(a))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<List<ESGTopPerformerViewModel>> IdentifyTopPerformersAsync(List<CampaignAssignment> assignments, int count)
    {
        var performers = new List<ESGTopPerformerViewModel>();
        
        foreach (var assignment in assignments.Take(count))
        {
            performers.Add(new ESGTopPerformerViewModel
            {
                CompanyId = assignment.TargetOrganizationId,
                CompanyName = assignment.TargetOrganization.Name,
                Sector = GetCompanySector(assignment),
                ESGScore = await CalculateCompanyESGScoreAsync(assignment)
            });
        }

        return performers.OrderByDescending(p => p.ESGScore).Take(count).ToList();
    }

    private async Task<Dictionary<string, int>> CalculateESGMaturityDistributionAsync(List<CampaignAssignment> assignments)
    {
        var distribution = new Dictionary<string, int>
        {
            ["High (80-100)"] = 0,
            ["Medium (60-79)"] = 0,
            ["Low (0-59)"] = 0
        };

        foreach (var assignment in assignments)
        {
            var score = await CalculateCompanyESGScoreAsync(assignment);
            
            if (score >= 80) distribution["High (80-100)"]++;
            else if (score >= 60) distribution["Medium (60-79)"]++;
            else distribution["Low (0-59)"]++;
        }

        return distribution;
    }

    private async Task<decimal> CalculateCompanyESGScoreAsync(CampaignAssignment assignment)
    {
        // Simplified ESG scoring algorithm
        var responses = await _context.Responses
            .Include(r => r.Question)
            .Where(r => r.CampaignAssignmentId == assignment.Id)
            .ToListAsync();

        if (!responses.Any()) return 0;

        var policyScore = CalculatePolicyScore(responses);
        var quantitativeScore = CalculateQuantitativeScore(responses);
        var completenessScore = CalculateCompletenessScore(responses, assignment);

        return (policyScore * 0.4m + quantitativeScore * 0.4m + completenessScore * 0.2m);
    }

    private decimal CalculatePolicyScore(List<Response> responses)
    {
        var policyResponses = responses.Where(r => r.Question.QuestionType == QuestionType.YesNo && r.BooleanValue.HasValue).ToList();
        if (!policyResponses.Any()) return 0;
        
        var positiveResponses = policyResponses.Count(r => r.BooleanValue == true);
        return (decimal)positiveResponses / policyResponses.Count * 100;
    }

    private decimal CalculateQuantitativeScore(List<Response> responses)
    {
        // Simplified quantitative scoring - normalize against industry benchmarks
        var numericResponses = responses.Where(r => r.Question.QuestionType == QuestionType.Number && r.NumericValue.HasValue).ToList();
        if (!numericResponses.Any()) return 0;

        // For now, return a score based on data completeness and some basic metrics
        return Math.Min(100, numericResponses.Count * 10); // Simplified scoring
    }

    private decimal CalculateCompletenessScore(List<Response> responses, CampaignAssignment assignment)
    {
        var totalQuestions = _context.Questions
            .Count(q => q.QuestionnaireId == assignment.QuestionnaireVersion.QuestionnaireId);
        
        if (totalQuestions == 0) return 100;
        
        return (decimal)responses.Count / totalQuestions * 100;
    }

    private List<ESGCompanyProgressViewModel> CalculateCompanyProgressAsync(int platformOrganizationId, List<int> reportingYears)
    {
        // Implementation for company progress analysis across years
        return new List<ESGCompanyProgressViewModel>(); // Placeholder
    }

    private async Task<decimal> CalculateAverageESGScoreAsync(List<CampaignAssignment> assignments)
    {
        var scores = new List<decimal>();
        foreach (var assignment in assignments)
        {
            scores.Add(await CalculateCompanyESGScoreAsync(assignment));
        }
        return scores.Any() ? scores.Average() : 0;
    }

    private async Task<List<Response>> GetQuestionResponsesAsync(List<int> assignmentIds, int questionId)
    {
        return await _context.Responses
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Where(r => assignmentIds.Contains(r.CampaignAssignmentId) && r.QuestionId == questionId)
            .ToListAsync();
    }

    private async Task<List<ESGResponseViewModel>> GetCompanyResponsesAsync(int assignmentId)
    {
        var responses = await _context.Responses
            .Include(r => r.Question)
            .Where(r => r.CampaignAssignmentId == assignmentId)
            .ToListAsync();

        return responses.Select(r => new ESGResponseViewModel
        {
            QuestionId = r.QuestionId,
            QuestionText = r.Question.QuestionText,
            QuestionType = r.Question.QuestionType,
            Section = r.Question.Section,
            TextValue = r.TextValue,
            NumericValue = r.NumericValue,
            BooleanValue = r.BooleanValue,
            SelectedValues = r.SelectedValues,
            Unit = r.Question.Unit,
            IsPercentage = r.Question.IsPercentage
        }).ToList();
    }

    // Analysis methods for different question types
    private ESGBooleanAnalysisViewModel AnalyzeBooleanResponses(List<Response> responses)
    {
        var yesCount = responses.Count(r => r.BooleanValue == true);
        var totalCount = responses.Count;

        return new ESGBooleanAnalysisViewModel
        {
            YesCount = yesCount,
            NoCount = totalCount - yesCount,
            YesPercentage = totalCount > 0 ? (decimal)yesCount / totalCount * 100 : 0
        };
    }

    private ESGNumericAnalysisViewModel AnalyzeNumericResponses(List<Response> responses, bool isPercentage, string? unit)
    {
        var values = responses.Where(r => r.NumericValue.HasValue).Select(r => r.NumericValue!.Value).ToList();
        
        if (!values.Any())
            return new ESGNumericAnalysisViewModel { HasData = false };

        return new ESGNumericAnalysisViewModel
        {
            HasData = true,
            Count = values.Count,
            Average = values.Average(),
            Median = CalculateMedian(values),
            Min = values.Min(),
            Max = values.Max(),
            Unit = unit,
            IsPercentage = isPercentage,
            Distribution = CalculateDistribution(values)
        };
    }

    private decimal CalculateMedian(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        else
            return sorted[count / 2];
    }

    private Dictionary<string, int> CalculateDistribution(List<decimal> values)
    {
        // Create simple distribution buckets
        var distribution = new Dictionary<string, int>();
        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        var bucketSize = range / 5; // 5 buckets

        for (int i = 0; i < 5; i++)
        {
            var bucketMin = min + i * bucketSize;
            var bucketMax = min + (i + 1) * bucketSize;
            var label = $"{bucketMin:F1}-{bucketMax:F1}";
            var count = values.Count(v => v >= bucketMin && (i == 4 ? v <= bucketMax : v < bucketMax));
            distribution[label] = count;
        }

        return distribution;
    }

    private ESGMultiSelectAnalysisViewModel AnalyzeMultiSelectResponses(List<Response> responses, string? optionsJson)
    {
        var analysis = new ESGMultiSelectAnalysisViewModel
        {
            OptionCounts = new Dictionary<string, int>()
        };

        if (string.IsNullOrEmpty(optionsJson)) return analysis;

        try
        {
            var availableOptions = JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
            
            foreach (var option in availableOptions)
            {
                analysis.OptionCounts[option] = 0;
            }

            foreach (var response in responses.Where(r => !string.IsNullOrEmpty(r.SelectedValues)))
            {
                var selectedOptions = JsonSerializer.Deserialize<List<string>>(response.SelectedValues!) ?? new List<string>();
                foreach (var selected in selectedOptions)
                {
                    if (analysis.OptionCounts.ContainsKey(selected))
                        analysis.OptionCounts[selected]++;
                }
            }

            analysis.TotalResponses = responses.Count;
            analysis.MostPopularOption = analysis.OptionCounts.Any() 
                ? analysis.OptionCounts.OrderByDescending(kvp => kvp.Value).First().Key 
                : null;
        }
        catch (JsonException)
        {
            // Handle JSON parsing errors gracefully
        }

        return analysis;
    }

    private ESGTextAnalysisViewModel AnalyzeTextResponses(List<Response> responses)
    {
        var textResponses = responses
            .Where(r => !string.IsNullOrWhiteSpace(r.TextValue))
            .Select(r => r.TextValue!)
            .ToList();

        return new ESGTextAnalysisViewModel
        {
            ResponseCount = textResponses.Count,
            AverageLength = textResponses.Any() ? (int)textResponses.Average(t => t.Length) : 0,
            CommonKeywords = ExtractCommonKeywords(textResponses),
            SampleResponses = textResponses.Take(3).ToList()
        };
    }

    private List<string> ExtractCommonKeywords(List<string> textResponses)
    {
        // Simple keyword extraction - split by words and find most common
        var words = textResponses
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 3) // Filter short words
            .GroupBy(w => w.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    // Placeholder methods for complex analyses
    private ESGPeerComparisonViewModel CalculatePeerComparisonAsync(CampaignAssignment companyAssignment, List<CampaignAssignment> peerAssignments)
    {
        return new ESGPeerComparisonViewModel(); // Placeholder
    }

    private ESGPortfolioComparisonViewModel CalculatePortfolioComparisonAsync(CampaignAssignment companyAssignment, List<CampaignAssignment> allAssignments)
    {
        return new ESGPortfolioComparisonViewModel(); // Placeholder
    }

    private ESGHistoricalTrendsViewModel CalculateCompanyHistoricalTrendsAsync(int platformOrganizationId, int companyId)
    {
        return new ESGHistoricalTrendsViewModel(); // Placeholder
    }

    private ESGStrengthsGapsViewModel IdentifyStrengthsAndGapsAsync(CampaignAssignment companyAssignment, List<CampaignAssignment> peerAssignments)
    {
        return new ESGStrengthsGapsViewModel(); // Placeholder
    }

    private Dictionary<string, ESGSectorResponseBreakdownViewModel> AnalyzeResponsesBySectorAsync(List<Response> responses, List<CampaignAssignment> assignments)
    {
        return new Dictionary<string, ESGSectorResponseBreakdownViewModel>(); // Placeholder
    }

    private List<ESGQuickInsightViewModel> GenerateQuickInsights(ESGPortfolioOverviewViewModel portfolioOverview)
    {
        var insights = new List<ESGQuickInsightViewModel>();

        // Policy adoption insight
        if (portfolioOverview.PolicyAdoptionRates.Any())
        {
            var avgPolicyAdoption = portfolioOverview.PolicyAdoptionRates.Values.Average();
            insights.Add(new ESGQuickInsightViewModel
            {
                Title = "Average Policy Adoption",
                Value = $"{avgPolicyAdoption:F1}%",
                Trend = avgPolicyAdoption >= 70 ? "up" : avgPolicyAdoption >= 50 ? "stable" : "down",
                Description = "Portfolio-wide policy implementation rate",
                Category = "Governance"
            });
        }

        return insights;
    }

    private List<ESGAlertViewModel> GenerateAlerts(int platformOrgId, int year)
    {
        var alerts = new List<ESGAlertViewModel>();

        // TODO: Implement alert logic based on thresholds and benchmarks
        // Examples:
        // - Companies with declining ESG scores
        // - Low response rates
        // - Missing critical policy implementations
        // - Outlier performance metrics

        return alerts;
    }

    private ESGProgressSummaryViewModel GenerateProgressSummary(int platformOrgId, List<int> availableYears)
    {
        var progressSummary = new ESGProgressSummaryViewModel();

        if (availableYears.Count >= 2)
        {
            // TODO: Implement progress calculation logic
            // Compare latest year with previous year across portfolio
            progressSummary.CompaniesImproving = 6; // Placeholder
            progressSummary.CompaniesStable = 3;    // Placeholder
            progressSummary.CompaniesDeclining = 1; // Placeholder
            progressSummary.OverallPortfolioProgress = 12.5m; // Placeholder
        }

        return progressSummary;
    }

    private ESGCompanyDeepDiveViewModel GenerateCompanyDeepDive(int platformOrgId, int companyId, int year)
    {
        var deepDive = new ESGCompanyDeepDiveViewModel();

        // TODO: Implement company deep dive logic
        // - Company-specific ESG metrics
        // - Comparison with portfolio average
        // - Trend analysis
        // - Detailed response analysis

        return deepDive;
    }


} 