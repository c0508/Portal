using ESGPlatform.Models.Entities;

namespace ESGPlatform.Models.ViewModels;

// ===================================================================
// Portfolio Overview ViewModel (for Portfolio.cshtml)
// ===================================================================

public class PortfolioOverviewViewModel
{
    // Basic KPI properties
    public decimal OverallESGScore { get; set; }
    public decimal ESGScoreChange { get; set; }
    public decimal ClimatePolicyAdoption { get; set; }
    public int CompaniesWithClimatePolicy { get; set; }
    public int TotalCompanies { get; set; }
    public decimal AverageWomenLeadership { get; set; }
    public decimal WomenLeadershipImprovement { get; set; }
    public decimal TotalEmissionsReduction { get; set; }
    
    // Collections for different sections
    public List<ESGTopPerformerViewModel> TopPerformers { get; set; } = new();
    public List<ESGTopPerformerViewModel> BottomPerformers { get; set; } = new();
    public List<PolicyAdoptionRateViewModel> PolicyAdoptionRates { get; set; } = new();
    public List<CompanyPerformanceViewModel> CompanyPerformances { get; set; } = new();
    public List<TrendDataViewModel> TrendData { get; set; } = new();
    public List<SectorPerformanceViewModel> SectorPerformances { get; set; } = new();
}

public class PolicyAdoptionRateViewModel
{
    public string PolicyName { get; set; } = string.Empty;
    public decimal AdoptionRate { get; set; }
}

public class CompanyPerformanceViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal ESGScore { get; set; }
    public bool HasClimatePolicy { get; set; }
    public bool HasDiversityPolicy { get; set; }
    public decimal WomenLeadershipPercentage { get; set; }
    public decimal TotalEmissions { get; set; }
}

public class TrendDataViewModel
{
    public int Year { get; set; }
    public decimal AverageESGScore { get; set; }
}

public class SectorPerformanceViewModel
{
    public string Sector { get; set; } = string.Empty;
    public decimal AverageESGScore { get; set; }
}

// ===================================================================
// ESG Portfolio Overview ViewModels
// ===================================================================

public class ESGPortfolioOverviewViewModel
{
    public int ReportingYear { get; set; }
    public int TotalCompanies { get; set; }
    public decimal CompletionRate { get; set; }
    public Dictionary<string, decimal> PolicyAdoptionRates { get; set; } = new();
    public Dictionary<string, decimal> QuantitativeBenchmarks { get; set; } = new();
    public Dictionary<string, int> SectorBreakdown { get; set; } = new();
    public List<ESGTopPerformerViewModel> TopPerformers { get; set; } = new();
    public Dictionary<string, int> ESGMaturityDistribution { get; set; } = new();
}

public class ESGTopPerformerViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal ESGScore { get; set; }
    public string ESGScoreCategory => ESGScore switch
    {
        >= 80 => "High",
        >= 60 => "Medium",
        _ => "Low"
    };
}

// ===================================================================
// ESG Temporal Analysis ViewModels
// ===================================================================

public class ESGTemporalAnalysisViewModel
{
    public List<int> ReportingYears { get; set; } = new();
    public Dictionary<string, List<decimal>> PolicyAdoptionTrends { get; set; } = new();
    public Dictionary<string, List<decimal>> QuantitativeMetricTrends { get; set; } = new();
    public List<ESGCompanyProgressViewModel> CompanyProgressAnalysis { get; set; } = new();
    
    // Calculated properties
    public Dictionary<string, decimal> PolicyImprovementRates => CalculatePolicyImprovementRates();
    public Dictionary<string, decimal> MetricImprovementRates => CalculateMetricImprovementRates();
    
    private Dictionary<string, decimal> CalculatePolicyImprovementRates()
    {
        var improvements = new Dictionary<string, decimal>();
        
        foreach (var policy in PolicyAdoptionTrends)
        {
            if (policy.Value.Count >= 2)
            {
                var firstYear = policy.Value.First();
                var lastYear = policy.Value.Last();
                var improvement = lastYear - firstYear;
                improvements[policy.Key] = improvement;
            }
        }
        
        return improvements;
    }
    
    private Dictionary<string, decimal> CalculateMetricImprovementRates()
    {
        var improvements = new Dictionary<string, decimal>();
        
        foreach (var metric in QuantitativeMetricTrends)
        {
            if (metric.Value.Count >= 2)
            {
                var firstYear = metric.Value.First();
                var lastYear = metric.Value.Last();
                
                // For emissions metrics, improvement is reduction (negative change is good)
                // For diversity metrics, improvement is increase (positive change is good)
                var improvement = metric.Key.ToLower().Contains("emission") || metric.Key.ToLower().Contains("waste")
                    ? firstYear - lastYear  // Reduction is improvement
                    : lastYear - firstYear; // Increase is improvement
                    
                improvements[metric.Key] = improvement;
            }
        }
        
        return improvements;
    }
}

public class ESGCompanyProgressViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public Dictionary<int, decimal> YearlyESGScores { get; set; } = new();
    public decimal OverallProgressRate { get; set; }
    public string ProgressCategory => OverallProgressRate switch
    {
        >= 10 => "Fast Improver",
        >= 5 => "Steady Improver",
        >= 0 => "Stable",
        _ => "Declining"
    };
}

// ===================================================================
// ESG Sector Benchmarking ViewModels
// ===================================================================

public class ESGSectorBenchmarkingViewModel
{
    public int ReportingYear { get; set; }
    public List<ESGSectorComparisonViewModel> SectorComparisons { get; set; } = new();
    
    // Portfolio-wide sector insights
    public string BestPerformingSector => SectorComparisons
        .OrderByDescending(s => s.AverageESGScore)
        .FirstOrDefault()?.SectorName ?? "N/A";
        
    public string MostImprovingSector { get; set; } = string.Empty; // Set by controller when comparing years
}

public class ESGSectorComparisonViewModel
{
    public string SectorName { get; set; } = string.Empty;
    public int CompanyCount { get; set; }
    public Dictionary<string, decimal> PolicyAdoptionRates { get; set; } = new();
    public Dictionary<string, decimal> QuantitativeBenchmarks { get; set; } = new();
    public decimal AverageESGScore { get; set; }
    public List<ESGTopPerformerViewModel> TopPerformingCompanies { get; set; } = new();
    
    // Sector-specific insights
    public List<string> SectorStrengths => PolicyAdoptionRates
        .Where(p => p.Value >= 80)
        .Select(p => p.Key)
        .ToList();
        
    public List<string> SectorGaps => PolicyAdoptionRates
        .Where(p => p.Value < 50)
        .Select(p => p.Key)
        .ToList();
}

// ===================================================================
// ESG Company Deep Dive ViewModels
// ===================================================================

public class ESGCompanyDeepDiveViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ReportingYear { get; set; }
    public string Sector { get; set; } = string.Empty;
    public decimal ESGScore { get; set; }
    
    public List<ESGResponseViewModel> CompanyResponses { get; set; } = new();
    public ESGPeerComparisonViewModel PeerComparison { get; set; } = new();
    public ESGPortfolioComparisonViewModel PortfolioComparison { get; set; } = new();
    public ESGHistoricalTrendsViewModel HistoricalTrends { get; set; } = new();
    public ESGStrengthsGapsViewModel StrengthsAndGaps { get; set; } = new();
    
    // Calculated insights
    public string ESGMaturityLevel => ESGScore switch
    {
        >= 80 => "High",
        >= 60 => "Medium",
        _ => "Low"
    };
    
    public int PeerRanking { get; set; } // Set by service
    public int PortfolioRanking { get; set; } // Set by service
}

public class ESGResponseViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? Section { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? SelectedValues { get; set; }
    public string? Unit { get; set; }
    public bool IsPercentage { get; set; }
    
    // Display helpers
    public string DisplayValue => QuestionType switch
    {
        QuestionType.YesNo => BooleanValue?.ToString() ?? "No Response",
        QuestionType.Number => NumericValue.HasValue 
            ? $"{NumericValue}{(IsPercentage ? "%" : Unit)}"
            : "No Response",
        QuestionType.Text => TextValue ?? "No Response",
        QuestionType.LongText => TextValue ?? "No Response",
        QuestionType.MultiSelect => SelectedValues ?? "No Selection",
        _ => "No Response"
    };
}

public class ESGPeerComparisonViewModel
{
    public string CompanySector { get; set; } = string.Empty;
    public int TotalPeers { get; set; }
    public decimal CompanyESGScore { get; set; }
    public decimal PeerAverageESGScore { get; set; }
    public decimal ScoreDifferenceFromPeers => CompanyESGScore - PeerAverageESGScore;
    public List<ESGPeerMetricComparisonViewModel> MetricComparisons { get; set; } = new();
    public string PerformanceVsPeers => ScoreDifferenceFromPeers switch
    {
        >= 10 => "Well Above Peers",
        >= 5 => "Above Peers",
        >= -5 => "In Line with Peers",
        >= -10 => "Below Peers",
        _ => "Well Below Peers"
    };
}

public class ESGPeerMetricComparisonViewModel
{
    public string MetricName { get; set; } = string.Empty;
    public decimal CompanyValue { get; set; }
    public decimal PeerAverage { get; set; }
    public decimal Percentile { get; set; } // Company's percentile ranking among peers
    public string Unit { get; set; } = string.Empty;
    public bool IsPercentage { get; set; }
}

public class ESGPortfolioComparisonViewModel
{
    public int TotalPortfolioCompanies { get; set; }
    public decimal CompanyESGScore { get; set; }
    public decimal PortfolioAverageESGScore { get; set; }
    public decimal ScoreDifferenceFromPortfolio => CompanyESGScore - PortfolioAverageESGScore;
    public int PortfolioRanking { get; set; }
    public decimal PortfolioPercentile { get; set; }
    public List<ESGPeerMetricComparisonViewModel> MetricComparisons { get; set; } = new();
}

public class ESGHistoricalTrendsViewModel
{
    public Dictionary<int, decimal> YearlyESGScores { get; set; } = new();
    public Dictionary<string, Dictionary<int, decimal>> PolicyTrends { get; set; } = new();
    public Dictionary<string, Dictionary<int, decimal>> QuantitativeTrends { get; set; } = new();
    public decimal OverallTrendDirection { get; set; } // Positive = improving, negative = declining
    public List<ESGMilestoneViewModel> KeyMilestones { get; set; } = new();
}

public class ESGMilestoneViewModel
{
    public int Year { get; set; }
    public string Achievement { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Policy", "Environmental", "Social", "Governance"
}

public class ESGStrengthsGapsViewModel
{
    public List<ESGStrengthViewModel> Strengths { get; set; } = new();
    public List<ESGGapViewModel> Gaps { get; set; } = new();
    public List<ESGRecommendationViewModel> Recommendations { get; set; } = new();
}

public class ESGStrengthViewModel
{
    public string Area { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string BenchmarkComparison { get; set; } = string.Empty; // "Above peers", "Industry leading", etc.
}

public class ESGGapViewModel
{
    public string Area { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string ImpactLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
    public string BenchmarkComparison { get; set; } = string.Empty;
}

public class ESGRecommendationViewModel
{
    public string Area { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
    public string Timeline { get; set; } = string.Empty; // "Short-term", "Medium-term", "Long-term"
    public decimal PotentialImpact { get; set; } // Estimated score improvement
}

// ===================================================================
// ESG Question-Level Analytics ViewModels
// ===================================================================

public class ESGQuestionLevelAnalyticsViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int ReportingYear { get; set; }
    public int TotalResponses { get; set; }
    public decimal ResponseRate { get; set; }
    
    // Question type-specific analyses
    public ESGBooleanAnalysisViewModel? BooleanAnalysis { get; set; }
    public ESGNumericAnalysisViewModel? NumericAnalysis { get; set; }
    public ESGMultiSelectAnalysisViewModel? MultiSelectAnalysis { get; set; }
    public ESGTextAnalysisViewModel? TextAnalysis { get; set; }
    
    // Cross-cutting analysis
    public Dictionary<string, ESGSectorResponseBreakdownViewModel> SectorBreakdown { get; set; } = new();
}

public class ESGBooleanAnalysisViewModel
{
    public int YesCount { get; set; }
    public int NoCount { get; set; }
    public decimal YesPercentage { get; set; }
    public decimal NoPercentage => 100 - YesPercentage;
    
    public string AdoptionLevel => YesPercentage switch
    {
        >= 80 => "High Adoption",
        >= 60 => "Moderate Adoption",
        >= 40 => "Low Adoption",
        _ => "Very Low Adoption"
    };
}

public class ESGNumericAnalysisViewModel
{
    public bool HasData { get; set; }
    public int Count { get; set; }
    public decimal Average { get; set; }
    public decimal Median { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public string? Unit { get; set; }
    public bool IsPercentage { get; set; }
    public Dictionary<string, int> Distribution { get; set; } = new();
    
    public decimal Range => Max - Min;
    public string VariabilityLevel => Range switch
    {
        <= 10 => "Low Variability",
        <= 50 => "Moderate Variability",
        _ => "High Variability"
    };
}

public class ESGMultiSelectAnalysisViewModel
{
    public Dictionary<string, int> OptionCounts { get; set; } = new();
    public int TotalResponses { get; set; }
    public string? MostPopularOption { get; set; }
    
    public Dictionary<string, decimal> OptionPercentages => OptionCounts.ToDictionary(
        kvp => kvp.Key,
        kvp => TotalResponses > 0 ? (decimal)kvp.Value / TotalResponses * 100 : 0
    );
    
    public decimal AverageSelectionsPerResponse => TotalResponses > 0 
        ? (decimal)OptionCounts.Values.Sum() / TotalResponses 
        : 0;
}

public class ESGTextAnalysisViewModel
{
    public int ResponseCount { get; set; }
    public int AverageLength { get; set; }
    public List<string> CommonKeywords { get; set; } = new();
    public List<string> SampleResponses { get; set; } = new();
    
    public string ResponseQuality => AverageLength switch
    {
        >= 200 => "Detailed Responses",
        >= 100 => "Moderate Detail",
        >= 50 => "Brief Responses",
        _ => "Very Brief Responses"
    };
}

public class ESGSectorResponseBreakdownViewModel
{
    public string SectorName { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public decimal ResponseRate { get; set; }
    public object? SectorAverage { get; set; } // Flexible type for different question types
    public string SectorTrend { get; set; } = string.Empty; // "Improving", "Stable", "Declining"
}

// ===================================================================
// Dashboard Summary ViewModels
// ===================================================================

public class ESGAnalyticsDashboardViewModel
{
    public ESGPortfolioOverviewViewModel PortfolioOverview { get; set; } = new();
    public List<ESGQuickInsightViewModel> QuickInsights { get; set; } = new();
    public List<ESGAlertViewModel> Alerts { get; set; } = new();
    public ESGProgressSummaryViewModel ProgressSummary { get; set; } = new();
}

public class ESGQuickInsightViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Environmental", "Social", "Governance"
}

public class ESGAlertViewModel
{
    public string Type { get; set; } = string.Empty; // "warning", "info", "success"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public int? CompanyId { get; set; }
    public string ActionRequired { get; set; } = string.Empty;
}

public class ESGProgressSummaryViewModel
{
    public int CompaniesImproving { get; set; }
    public int CompaniesStable { get; set; }
    public int CompaniesDeclining { get; set; }
    public decimal OverallPortfolioProgress { get; set; }
    public List<string> KeyAchievements { get; set; } = new();
    public List<string> AreasNeedingAttention { get; set; } = new();
}

// ===================================================================
// Flexible Analytics View Models
// ===================================================================

public enum NumericAggregationMethod
{
    Sum,
    Average,
    Count,
    Min,
    Max
}

public class FlexibleAnalyticsViewModel
{
    public List<int> AvailableYears { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();
    public List<QuestionFilterOption> AvailableQuestions { get; set; } = new();
    public List<CompanyFilterOption> AvailableCompanies { get; set; } = new();
    public List<SectorFilterOption> AvailableSectors { get; set; } = new();
    public List<AttributeOption> AvailableAttributes { get; set; } = new();
    
    // Filters and results
    public FlexibleAnalyticsFilters Filters { get; set; } = new();
    
    // Chart data
    public FlexibleAnalyticsChartData ChartData { get; set; } = new();
    
    // Data table
    public FlexibleAnalyticsDataTable DataTable { get; set; } = new();
    
    // Dynamic attribute labels
    public string PrimaryAttributeType { get; set; } = "Attributes";
    public string PrimaryAttributeDisplayName { get; set; } = "Attributes";
}

public class FlexibleAnalyticsFilters
{
    public List<int> Years { get; set; } = new();
    public List<string> Sections { get; set; } = new();
    public List<int> QuestionIds { get; set; } = new();
    public List<int> CompanyIds { get; set; } = new();
    public List<string> Sectors { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string SelectedAttribute { get; set; } = "Sector"; // Used for dynamic chart updates
    public NumericAggregationMethod AggregationMethod { get; set; } = NumericAggregationMethod.Sum; // New property for smart aggregation
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "CompanyName";
    public bool SortDescending { get; set; } = false;
}

public class FlexibleAnalyticsChartData
{
    public string ChartType { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }

    // Primary chart (e.g., bar chart)
    public List<ChartDataPoint> PrimaryData { get; set; } = new();
    public string PrimaryChartType { get; set; }
    public string PrimaryTitle { get; set; }

    // Secondary chart (e.g., time series line chart)
    public List<ChartDataPoint> SecondaryData { get; set; } = new();
    public string SecondaryChartType { get; set; }
    public string SecondaryTitle { get; set; }
    public bool SecondaryIsStacked { get; set; } = false;
    public bool SecondaryHideAttributeFilter { get; set; } = false; // Hide attribute filter for boolean time evolution

    // Reference data (e.g., benchmarks)
    public List<ReferenceDataPoint> ReferenceData { get; set; } = new();

    // Summary statistics
    public AnalyticsSummary Summary { get; set; } = new();

    public FlexibleAnalyticsChartData()
    {
        ChartType = "none";
        Title = string.Empty;
        Subtitle = string.Empty;
        PrimaryChartType = string.Empty;
        PrimaryTitle = string.Empty;
        SecondaryChartType = string.Empty;
        SecondaryTitle = string.Empty;
    }
}

public class ChartDataPoint
{
    public string Label { get; set; }
    public decimal Value { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    public ChartDataPoint()
    {
        Label = string.Empty;
    }
}

public class ReferenceDataPoint
{
    public string Label { get; set; }
    public decimal Value { get; set; }
    public string Type { get; set; } // "sector", "total", "peer"
    public string? Color { get; set; }

    public ReferenceDataPoint()
    {
        Label = string.Empty;
        Type = string.Empty;
    }
}

public class AnalyticsSummary
{
    public int TotalResponses { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalSectors { get; set; }
    public decimal ResponseRate { get; set; }
    public string? MostCommonValue { get; set; }
    public decimal? Average { get; set; }
    public decimal? Median { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

public class FlexibleAnalyticsDataTable
{
    public List<FlexibleAnalyticsDataRow> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<string> Columns { get; set; } = new();
}

public class FlexibleAnalyticsDataRow
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public string? Size { get; set; }
    public int Year { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public string Section { get; set; } = "";
    public string QuestionType { get; set; } = "";
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? SelectedValues { get; set; }
    public string? Unit { get; set; }
    public bool IsPercentage { get; set; }
    public string FormattedValue { get; set; } = "";
    public Dictionary<string, string> OrganizationAttributes { get; set; } = new();
}

public class QuestionFilterOption
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string Section { get; set; } = "";
    public string Type { get; set; } = "";
    public int ResponseCount { get; set; }
}

public class CompanyFilterOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public int ResponseCount { get; set; }
}

public class SectorFilterOption
{
    public string Name { get; set; } = "";
    public int CompanyCount { get; set; }
    public int ResponseCount { get; set; }
}

public class AttributeOption
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int CompanyCount { get; set; }
    public List<string> Values { get; set; } = new();
} 