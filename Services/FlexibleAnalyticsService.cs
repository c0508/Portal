using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models.Entities;
using System.Text.Json;
using System.Globalization;

namespace ESGPlatform.Services;

public interface IFlexibleAnalyticsService
{
    Task<FlexibleAnalyticsViewModel> GetAnalyticsAsync(int platformOrganizationId, FlexibleAnalyticsFilters filters);
    Task<List<int>> GetAvailableYearsAsync(int platformOrganizationId);
    Task<List<string>> GetAvailableSectionsAsync(int platformOrganizationId, List<int> years);
    Task<List<QuestionFilterOption>> GetAvailableQuestionsAsync(int platformOrganizationId, List<int> years, List<string> sections);
    Task<List<CompanyFilterOption>> GetAvailableCompaniesAsync(int platformOrganizationId, List<int> years, List<string> sectors, string? selectedAttribute);
    Task<List<SectorFilterOption>> GetAvailableSectorsAsync(int platformOrganizationId, List<int> years);
    Task<List<AttributeOption>> GetAvailableAttributesAsync(int platformOrganizationId, List<int> years);
    Task<byte[]> ExportToExcelAsync(int platformOrganizationId, FlexibleAnalyticsFilters filters);
}

public class FlexibleAnalyticsService : IFlexibleAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FlexibleAnalyticsService> _logger;

    public FlexibleAnalyticsService(ApplicationDbContext context, ILogger<FlexibleAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FlexibleAnalyticsViewModel> GetAnalyticsAsync(int platformOrganizationId, FlexibleAnalyticsFilters filters)
    {
        // Auto-set SelectedAttribute if not specified or doesn't exist
        if (string.IsNullOrEmpty(filters.SelectedAttribute) || filters.SelectedAttribute == "Sector")
        {
            var availableAttributes = await GetAvailableAttributesAsync(platformOrganizationId, filters.Years);
            if (availableAttributes.Any())
            {
                filters.SelectedAttribute = availableAttributes.OrderByDescending(a => a.CompanyCount).First().Name;
                _logger.LogInformation($"Auto-selected attribute: {filters.SelectedAttribute}");
            }
        }

        // Auto-set AggregationMethod for percentage questions
        if (filters.QuestionIds.Count == 1)
        {
            var questionId = filters.QuestionIds.First();
            var question = await _context.Questions.FindAsync(questionId);
            if (question != null && question.QuestionType == QuestionType.Number)
            {
                var shouldUseAverage = IsPercentageQuestion(question);
                if (shouldUseAverage && filters.AggregationMethod == NumericAggregationMethod.Sum)
                {
                    filters.AggregationMethod = NumericAggregationMethod.Average;
                    _logger.LogInformation($"Auto-set aggregation method to Average for percentage question: {question.QuestionText}");
                }
            }
        }

        var viewModel = new FlexibleAnalyticsViewModel
        {
            Filters = filters,
            AvailableYears = await GetAvailableYearsAsync(platformOrganizationId),
            AvailableSections = await GetAvailableSectionsAsync(platformOrganizationId, filters.Years),
            AvailableQuestions = await GetAvailableQuestionsAsync(platformOrganizationId, filters.Years, filters.Sections),
            AvailableCompanies = await GetAvailableCompaniesAsync(platformOrganizationId, filters.Years, filters.Sectors, filters.SelectedAttribute),
            AvailableSectors = await GetAvailableSectorsAsync(platformOrganizationId, filters.Years),
            AvailableAttributes = await GetAvailableAttributesAsync(platformOrganizationId, filters.Years)
        };

        // Set dynamic attribute labels
        if (viewModel.AvailableAttributes.Any())
        {
            var primaryAttribute = viewModel.AvailableAttributes.OrderByDescending(a => a.CompanyCount).First();
            viewModel.PrimaryAttributeType = primaryAttribute.Name;
            viewModel.PrimaryAttributeDisplayName = primaryAttribute.DisplayName;
            _logger.LogInformation($"Set primary attribute display: {viewModel.PrimaryAttributeDisplayName}");
        }

        // Get the underlying data
        var baseQuery = await GetBaseDataQueryAsync(platformOrganizationId, filters);
        
        // Generate charts based on filters
        viewModel.ChartData = await GenerateChartDataAsync(baseQuery, filters);
        
        // Generate data table
        viewModel.DataTable = await GenerateDataTableAsync(baseQuery, filters);

        return viewModel;
    }

    public async Task<List<int>> GetAvailableYearsAsync(int platformOrganizationId)
    {
        return await _context.Campaigns
            .Where(c => c.OrganizationId == platformOrganizationId && c.ReportingPeriodStart.HasValue)
            .Select(c => c.ReportingPeriodStart!.Value.Year + 1)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    public async Task<List<string>> GetAvailableSectionsAsync(int platformOrganizationId, List<int> years)
    {
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, years);
        
        return await _context.Responses
            .Include(r => r.Question)
            .Include(r => r.CampaignAssignment)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId))
            .Select(r => r.Question.Section ?? "Other")
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<List<QuestionFilterOption>> GetAvailableQuestionsAsync(int platformOrganizationId, List<int> years, List<string> sections)
    {
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, years);
        
        var query = _context.Responses
            .Include(r => r.Question)
            .Include(r => r.CampaignAssignment)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId));

        if (sections.Any())
        {
            query = query.Where(r => sections.Contains(r.Question.Section ?? "Other"));
        }

        return await query
            .GroupBy(r => new { r.Question.Id, r.Question.QuestionText, r.Question.Section, r.Question.QuestionType })
            .Select(g => new QuestionFilterOption
            {
                Id = g.Key.Id,
                Text = g.Key.QuestionText,
                Section = g.Key.Section ?? "Other",
                Type = g.Key.QuestionType.ToString(),
                ResponseCount = g.Count()
            })
            .OrderBy(q => q.Section)
            .ThenBy(q => q.Text)
            .ToListAsync();
    }

    public async Task<List<CompanyFilterOption>> GetAvailableCompaniesAsync(int platformOrganizationId, List<int> years, List<string> sectors, string? selectedAttribute)
    {
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, years);
        
        var query = _context.Responses
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.OrganizationRelationship)
                    .ThenInclude(or => or!.Attributes)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId));

        if (sectors.Any() && !string.IsNullOrEmpty(selectedAttribute))
        {
            _logger.LogInformation($"Filtering available companies by attribute '{selectedAttribute}' with values: {string.Join(", ", sectors)}");
            query = query.Where(r => r.CampaignAssignment.OrganizationRelationship!.Attributes!
                .Any(a => a.AttributeType == selectedAttribute && sectors.Contains(a.AttributeValue)));
        }

        var responses = await query.ToListAsync();

        // Get the most common attribute type to use for sector information
        var allAttributes = responses
            .SelectMany(r => r.CampaignAssignment?.OrganizationRelationship?.Attributes ?? new List<OrganizationRelationshipAttribute>())
            .ToList();
            
        var primaryAttributeType = allAttributes.Any() ? 
            allAttributes.GroupBy(a => a.AttributeType).OrderByDescending(g => g.Count()).First().Key : 
            "Unknown";

        return responses
            .Where(r => r.CampaignAssignment?.TargetOrganization != null)
            .GroupBy(r => new { 
                Id = r.CampaignAssignment.TargetOrganization.Id, 
                Name = r.CampaignAssignment.TargetOrganization.Name
            })
            .Select(g => 
            {
                var firstResponse = g.First();
                var sector = firstResponse.CampaignAssignment?.OrganizationRelationship?.Attributes
                    ?.FirstOrDefault(a => a.AttributeType == primaryAttributeType)?.AttributeValue ?? "Unknown";

                return new CompanyFilterOption
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Sector = sector,
                    ResponseCount = g.Select(r => r.Id).Distinct().Count()
                };
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<List<SectorFilterOption>> GetAvailableSectorsAsync(int platformOrganizationId, List<int> years)
    {
        // Instead of hardcoding "Sector", use the first available attribute type as the primary grouping
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, years);
        
        var responses = await _context.Responses
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.OrganizationRelationship)
                    .ThenInclude(or => or!.Attributes)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId))
            .ToListAsync();

        // Get the most common attribute type to use as the primary filter
        var allAttributes = responses
            .SelectMany(r => r.CampaignAssignment?.OrganizationRelationship?.Attributes ?? new List<OrganizationRelationshipAttribute>())
            .ToList();
            
        if (!allAttributes.Any())
        {
            return new List<SectorFilterOption>
            {
                new SectorFilterOption { Name = "Unknown", CompanyCount = 0, ResponseCount = 0 }
            };
        }

        var primaryAttributeType = allAttributes
            .GroupBy(a => a.AttributeType)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        _logger.LogInformation($"Using '{primaryAttributeType}' as primary grouping attribute for sectors filter");

        return responses
            .GroupBy(r => r.CampaignAssignment?.OrganizationRelationship?.Attributes
                ?.FirstOrDefault(a => a.AttributeType == primaryAttributeType)?.AttributeValue ?? "Unknown")
            .Select(g => new SectorFilterOption
            {
                Name = g.Key,
                CompanyCount = g.Select(r => r.CampaignAssignment?.TargetOrganizationId).Where(id => id != null).Distinct().Count(),
                ResponseCount = g.Count()
            })
            .OrderBy(s => s.Name)
            .ToList();
    }

    public async Task<List<AttributeOption>> GetAvailableAttributesAsync(int platformOrganizationId, List<int> years)
    {
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, years);
        
        var responses = await _context.Responses
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.OrganizationRelationship)
                    .ThenInclude(or => or!.Attributes)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId))
            .ToListAsync();

        // Get all attributes from all organizations
        var allAttributes = responses
            .SelectMany(r =>
                r.CampaignAssignment?.OrganizationRelationship?.Attributes ?? new List<OrganizationRelationshipAttribute>()
            )
            .ToList();

        // Group by attribute type and collect values
        return allAttributes
            .GroupBy(a => a.AttributeType)
            .Select(g => new AttributeOption
            {
                Name = g.Key,
                DisplayName = ConvertAttributeTypeToDisplayName(g.Key),
                CompanyCount = g.Select(a => a.OrganizationRelationshipId).Distinct().Count(),
                Values = g.Select(a => a.AttributeValue).Distinct().OrderBy(v => v).ToList()
            })
            .OrderBy(a => a.Name)
            .ToList();
    }

    private string ConvertAttributeTypeToDisplayName(string attributeType)
    {
        return attributeType switch
        {
            "Company" => "Companies",
            "INDUSTRY" => "Industries",
            "SECTOR" => "Sectors", 
            "REGION" => "Regions",
            "SIZE_CATEGORY" => "Company Sizes",
            "ABC_SEGMENTATION" => "ABC Classifications",
            "SUPPLIER_CLASSIFICATION" => "Supplier Classifications",
            "BUSINESS_TYPE" => "Business Types",
            "COUNTRY" => "Countries",
            "DIVISION" => "Divisions",
            "DEPARTMENT" => "Departments",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(attributeType.Replace("_", " ").ToLower())
        };
    }

    public async Task<byte[]> ExportToExcelAsync(int platformOrganizationId, FlexibleAnalyticsFilters filters)
    {
        // Get all data without pagination for export
        var exportFilters = new FlexibleAnalyticsFilters
        {
            Years = filters.Years,
            Sections = filters.Sections,
            QuestionIds = filters.QuestionIds,
            CompanyIds = filters.CompanyIds,
            Sectors = filters.Sectors,
            SearchTerm = filters.SearchTerm,
            Page = 1,
            PageSize = 10000, // Large number to get all data
            SortBy = filters.SortBy,
            SortDescending = filters.SortDescending
        };

        var baseQuery = await GetBaseDataQueryAsync(platformOrganizationId, exportFilters);
        var dataTable = await GenerateDataTableAsync(baseQuery, exportFilters);

        // TODO: Implement Excel export using EPPlus or similar
        // For now, return empty byte array
        return new byte[0];
    }

    // Private helper methods

    private async Task<List<int>> GetCampaignIdsAsync(int platformOrganizationId, List<int> years)
    {
        var query = _context.Campaigns
            .Where(c => c.OrganizationId == platformOrganizationId && c.ReportingPeriodStart.HasValue);

        if (years.Any())
        {
            query = query.Where(c => years.Contains(c.ReportingPeriodStart!.Value.Year + 1));
        }

        return await query.Select(c => c.Id).ToListAsync();
    }

    private async Task<List<Response>> GetBaseDataQueryAsync(int platformOrganizationId, FlexibleAnalyticsFilters filters)
    {
        var campaignIds = await GetCampaignIdsAsync(platformOrganizationId, filters.Years);
        
        var query = _context.Responses
            .Include(r => r.Question)
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.OrganizationRelationship)
                    .ThenInclude(or => or!.Attributes)
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Where(r => campaignIds.Contains(r.CampaignAssignment.CampaignId));

        // Get all responses first to avoid LINQ expression tree issues
        var allResponses = await query.ToListAsync();

        // Apply filters using LINQ to Objects (safer with null checks)
        var filteredResponses = allResponses.AsEnumerable();

        if (filters.Sections.Any())
        {
            filteredResponses = filteredResponses.Where(r => filters.Sections.Contains(r.Question.Section ?? "Other"));
        }

        if (filters.QuestionIds.Any())
        {
            filteredResponses = filteredResponses.Where(r => filters.QuestionIds.Contains(r.Question.Id));
        }

        if (filters.CompanyIds.Any())
        {
            filteredResponses = filteredResponses.Where(r => filters.CompanyIds.Contains(r.CampaignAssignment.TargetOrganizationId));
        }

        if (filters.Sectors.Any())
        {
            // Get the primary attribute type dynamically
            var sampleResponse = filteredResponses.FirstOrDefault();
            var primaryAttributeType = "Unknown";
            
            if (sampleResponse?.CampaignAssignment.OrganizationRelationship?.Attributes?.Any() == true)
            {
                var allAttributes = allResponses
                    .SelectMany(r => r.CampaignAssignment.OrganizationRelationship!.Attributes ?? new List<OrganizationRelationshipAttribute>())
                    .ToList();
                    
                if (allAttributes.Any())
                {
                    primaryAttributeType = allAttributes
                        .GroupBy(a => a.AttributeType)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }
            }
            
            filteredResponses = filteredResponses.Where(r => filters.Sectors.Contains(
                r.CampaignAssignment.OrganizationRelationship?.Attributes
                    ?.FirstOrDefault(a => a.AttributeType == primaryAttributeType)?.AttributeValue ?? "Unknown"));
        }

        if (!string.IsNullOrEmpty(filters.SearchTerm))
        {
            filteredResponses = filteredResponses.Where(r => 
                r.Question.QuestionText.Contains(filters.SearchTerm) ||
                r.CampaignAssignment.TargetOrganization.Name.Contains(filters.SearchTerm) ||
                (r.TextValue != null && r.TextValue.Contains(filters.SearchTerm)));
        }

        return filteredResponses.ToList();
    }

    private async Task<FlexibleAnalyticsChartData> GenerateChartDataAsync(List<Response> baseData, FlexibleAnalyticsFilters filters)
    {
        var chartData = new FlexibleAnalyticsChartData();
        
        // Debug logging
        _logger.LogInformation($"GenerateChartDataAsync: Base data count: {baseData.Count}, Filter question count: {filters.QuestionIds.Count}");
        
        // If single question selected, generate question-specific charts
        if (filters.QuestionIds.Count == 1)
        {
            var questionId = filters.QuestionIds.First();
            var question = await _context.Questions.FindAsync(questionId);
            
            if (question != null)
            {
                chartData = GenerateQuestionSpecificChartsAsync(baseData, question, filters);
                
                // Debug logging after chart generation
                _logger.LogInformation($"Generated charts for question {questionId}: Primary data count: {chartData.PrimaryData.Count}, Secondary data count: {chartData.SecondaryData.Count}");
            }
            else
            {
                _logger.LogWarning($"Question {questionId} not found in database");
            }
        }
        // If multiple questions or no specific question, generate overview charts
        else
        {
            chartData = await GenerateOverviewChartsAsync(baseData, filters);
            _logger.LogInformation($"Generated overview charts: Primary data count: {chartData.PrimaryData.Count}, Secondary data count: {chartData.SecondaryData.Count}");
        }

        return chartData;
    }

    private FlexibleAnalyticsChartData GenerateQuestionSpecificChartsAsync(List<Response> baseData, Question question, FlexibleAnalyticsFilters filters)
    {
        var chartData = new FlexibleAnalyticsChartData
        {
            Title = question.QuestionText,
            Subtitle = $"Section: {question.Section ?? "Other"}"
        };

        var responses = baseData.Where(r => r.QuestionId == question.Id).ToList();
        
        // Debug logging
        _logger.LogInformation($"Question {question.Id} ({question.QuestionType}): Found {responses.Count} responses out of {baseData.Count} total responses");
        _logger.LogInformation($"Selected attribute: {filters.SelectedAttribute}");
        
        // Check if selected attribute exists in the data
        var availableAttributes = responses
            .SelectMany(r => r.CampaignAssignment.OrganizationRelationship?.Attributes ?? new List<OrganizationRelationshipAttribute>())
            .Select(a => a.AttributeType)
            .Distinct()
            .ToList();
            
        _logger.LogInformation($"Available attributes for this question: {string.Join(", ", availableAttributes)}");
        
        // Use the selected attribute if it exists, or if it's "Company" (special case), otherwise use the first available
        var attributeToUse = (availableAttributes.Contains(filters.SelectedAttribute) || filters.SelectedAttribute == "Company") ? 
            filters.SelectedAttribute : 
            availableAttributes.FirstOrDefault() ?? "Unknown";
            
        if (attributeToUse != filters.SelectedAttribute)
        {
            _logger.LogInformation($"Switching from '{filters.SelectedAttribute}' to '{attributeToUse}' for this question");
            // Don't modify the original filters object - use local variable instead
        }
        
        // Check attribute values for debugging
        var attributeValues = responses.Select(r => GetAttributeValueFromResponse(r, attributeToUse)).Distinct().ToList();
        _logger.LogInformation($"Attribute values found for '{attributeToUse}': {string.Join(", ", attributeValues)}");
        
        // Debug: Check if responses have the expected data
        if (responses.Any())
        {
            var firstResponse = responses.First();
            _logger.LogInformation($"First response - ID: {firstResponse.Id}, QuestionId: {firstResponse.QuestionId}, BooleanValue: {firstResponse.BooleanValue}, TextValue: {firstResponse.TextValue}, NumericValue: {firstResponse.NumericValue}");
        }
        
        switch (question.QuestionType)
        {
            case QuestionType.YesNo:
                chartData = GenerateBooleanChartsAsync(responses, chartData, filters, attributeToUse);
                break;
            case QuestionType.Number:
                chartData = GenerateNumericChartsAsync(responses, chartData, filters, question, attributeToUse);
                break;
            case QuestionType.MultiSelect:
                chartData = GenerateMultiSelectChartsAsync(responses, chartData, filters, question, attributeToUse);
                break;
            case QuestionType.Text:
            case QuestionType.LongText:
                chartData = GenerateTextChartsAsync(responses, chartData, filters, question, attributeToUse);
                break;
            default:
                chartData = GenerateNumericChartsAsync(responses, chartData, filters, question, attributeToUse);
                break;
        }

        return chartData;
    }

    private FlexibleAnalyticsChartData GenerateBooleanChartsAsync(List<Response> responses, FlexibleAnalyticsChartData chartData, FlexibleAnalyticsFilters filters, string attributeToUse)
    {
        var booleanResponses = responses.Where(r => r.BooleanValue.HasValue).ToList();
        
        if (!booleanResponses.Any()) return chartData;

        // Group responses by year
        var yearlyData = booleanResponses
            .GroupBy(r => r.CampaignAssignment.Campaign.ReportingPeriodStart!.Value.Year + 1)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (yearlyData.Count > 1)
        {
            // Multiple years: Show stacked bars of Yes/No counts over time
            var timeSeriesData = new List<ChartDataPoint>();
            
            foreach (var year in yearlyData)
            {
                var yearYesCount = year.Value.Count(r => r.BooleanValue == true);
                var yearNoCount = year.Value.Count(r => r.BooleanValue == false);
                
                timeSeriesData.Add(new ChartDataPoint
                {
                    Label = year.Key.ToString(),
                    Value = yearYesCount,
                    Category = "Yes",
                    Color = "#28a745"
                });
                
                timeSeriesData.Add(new ChartDataPoint
                {
                    Label = year.Key.ToString(),
                    Value = yearNoCount,
                    Category = "No",
                    Color = "#dc3545"
                });
            }

            chartData.SecondaryData = timeSeriesData;
            chartData.SecondaryChartType = "stacked-bar";
            chartData.SecondaryTitle = "Yes/No Responses Over Time";
            chartData.SecondaryIsStacked = true;
            chartData.SecondaryHideAttributeFilter = true; // Hide attribute filter for boolean time evolution
        }
        else
        {
            // Single year: Show pie chart of Yes/No distribution
            var yesCount = booleanResponses.Count(r => r.BooleanValue == true);
            var noCount = booleanResponses.Count(r => r.BooleanValue == false);
            
            chartData.SecondaryData = new List<ChartDataPoint>
            {
                new ChartDataPoint { Label = "Yes", Value = yesCount, Color = "#28a745" },
                new ChartDataPoint { Label = "No", Value = noCount, Color = "#dc3545" }
            };
            chartData.SecondaryChartType = "pie";
            chartData.SecondaryTitle = "Yes/No Distribution";
            chartData.SecondaryIsStacked = false;
            chartData.SecondaryHideAttributeFilter = false;
        }

        return chartData;
    }

    private FlexibleAnalyticsChartData GenerateNumericChartsAsync(List<Response> responses, FlexibleAnalyticsChartData chartData, FlexibleAnalyticsFilters filters, Question question, string attributeToUse)
    {
        var validResponses = responses.Where(r => r.NumericValue.HasValue).ToList();
        
        if (!validResponses.Any()) return chartData;

        var values = validResponses.Select(r => r.NumericValue!.Value).ToList();
        var aggregationDisplayName = GetAggregationMethodDisplayName(filters.AggregationMethod);

        // Initialize chart data
        chartData.PrimaryChartType = "bar";
        chartData.PrimaryTitle = $"{aggregationDisplayName} by {ConvertAttributeTypeToDisplayName(attributeToUse)}";
        chartData.PrimaryData = new List<ChartDataPoint>();

        // Group data by the selected attribute
        var groupedData = validResponses
            .GroupBy(r => GetAttributeValueFromResponse(r, attributeToUse))
            .ToList();

        // Generate colors for each group
        var colorPalette = GetColorPalette();
        var colorIndex = 0;

        foreach (var group in groupedData)
        {
            var groupValues = group.Select(r => r.NumericValue!.Value).ToList();
            var aggregatedValue = ApplyAggregationMethod(groupValues, filters.AggregationMethod);
            
            chartData.PrimaryData.Add(new ChartDataPoint
            {
                Label = group.Key,
                Value = aggregatedValue,
                Color = colorPalette[colorIndex % colorPalette.Count]
            });
            
            colorIndex++;
        }

        // Secondary chart: Show numeric values over time if multiple years
        var years = validResponses.Select(r => r.CampaignAssignment.Campaign.ReportingPeriodStart!.Value.Year).Distinct().OrderBy(y => y).ToList();
        var attributeGroups = validResponses.GroupBy(r => GetAttributeValueFromResponse(r, attributeToUse)).ToList();

        if (years.Count > 1)
        {
            // Multiple years: Show time series for each attribute value
            var multiLineData = new List<ChartDataPoint>();
            colorIndex = 0;
            foreach (var group in attributeGroups)
            {
                var attrLabel = group.Key;
                var color = colorPalette[colorIndex % colorPalette.Count];
                foreach (var year in years)
                {
                    var yearGroup = group.Where(r => r.CampaignAssignment.Campaign.ReportingPeriodStart!.Value.Year == year).ToList();
                    var yearValues = yearGroup.Select(r => r.NumericValue!.Value).ToList();
                    var aggregatedValue = yearValues.Any() ? ApplyAggregationMethod(yearValues, filters.AggregationMethod) : 0;
                    multiLineData.Add(new ChartDataPoint
                    {
                        Label = year.ToString(),
                        Value = aggregatedValue,
                        Category = attrLabel,
                        Color = color
                    });
                }
                colorIndex++;
            }
            chartData.SecondaryData = multiLineData;
            chartData.SecondaryChartType = "line";
            chartData.SecondaryTitle = $"{aggregationDisplayName} Over Time";
            chartData.SecondaryIsStacked = false;
            chartData.SecondaryHideAttributeFilter = false;
        }
        else
        {
            // Single year: Show distribution chart
            var distributionData = CreateDistributionBins(values);
            chartData.SecondaryData = distributionData;
            chartData.SecondaryChartType = "bar";
            chartData.SecondaryTitle = "Value Distribution";
            chartData.SecondaryIsStacked = false;
            chartData.SecondaryHideAttributeFilter = false;
        }

        return chartData;
    }

    private FlexibleAnalyticsChartData GenerateMultiSelectChartsAsync(List<Response> responses, FlexibleAnalyticsChartData chartData, FlexibleAnalyticsFilters filters, Question question, string attributeToUse)
    {
        var multiSelectResponses = responses.Where(r => !string.IsNullOrEmpty(r.SelectedValues)).ToList();
        
        if (!multiSelectResponses.Any()) return chartData;

        // Parse multi-select values (assuming comma-separated)
        var allSelections = multiSelectResponses
            .SelectMany(r => r.SelectedValues!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => s.Trim())
            .ToList();

        var colorPalette = GetColorPalette();
        var colorIndex = 0;

        // Primary chart: Show selection frequency
        var selectionCounts = allSelections
            .GroupBy(s => s)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Color = colorPalette[colorIndex++ % colorPalette.Count]
            })
            .OrderByDescending(p => p.Value)
            .ToList();

        chartData.PrimaryData = selectionCounts;
        chartData.PrimaryChartType = "bar";
        chartData.PrimaryTitle = "Selection Frequency";

        // Secondary chart: Show selections by attribute
        colorIndex = 0;
        var attributeSelections = multiSelectResponses
            .GroupBy(r => GetAttributeValueFromResponse(r, attributeToUse))
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.SelectMany(r => r.SelectedValues!.Split(',', StringSplitOptions.RemoveEmptyEntries)).Count(),
                Color = colorPalette[colorIndex++ % colorPalette.Count]
            })
            .OrderByDescending(p => p.Value)
            .ToList();

        chartData.SecondaryData = attributeSelections;
        chartData.SecondaryChartType = "bar";
        chartData.SecondaryTitle = $"Selections by {ConvertAttributeTypeToDisplayName(attributeToUse)}";
        chartData.SecondaryIsStacked = false;
        chartData.SecondaryHideAttributeFilter = false;

        return chartData;
    }

    private FlexibleAnalyticsChartData GenerateTextChartsAsync(List<Response> responses, FlexibleAnalyticsChartData chartData, FlexibleAnalyticsFilters filters, Question question, string attributeToUse)
    {
        var textResponses = responses.Where(r => !string.IsNullOrEmpty(r.TextValue)).ToList();
        
        if (!textResponses.Any()) return chartData;

        var colorPalette = GetColorPalette();
        var colorIndex = 0;

        // Primary chart: Show response count by attribute
        var attributeResponseCounts = textResponses
            .GroupBy(r => GetAttributeValueFromResponse(r, attributeToUse))
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Color = colorPalette[colorIndex++ % colorPalette.Count]
            })
            .OrderByDescending(p => p.Value)
            .ToList();

        chartData.PrimaryData = attributeResponseCounts;
        chartData.PrimaryChartType = "bar";
        chartData.PrimaryTitle = $"Response Count by {ConvertAttributeTypeToDisplayName(attributeToUse)}";

        // Secondary chart: Show response length distribution
        colorIndex = 0;
        var lengthDistribution = textResponses
            .Select(r => r.TextValue!.Length)
            .GroupBy(l => l switch
            {
                <= 50 => "Short (≤50 chars)",
                <= 200 => "Medium (51-200 chars)",
                <= 500 => "Long (201-500 chars)",
                _ => "Very Long (>500 chars)"
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Color = colorPalette[colorIndex++ % colorPalette.Count]
            })
            .ToList();

        chartData.SecondaryData = lengthDistribution;
        chartData.SecondaryChartType = "pie";
        chartData.SecondaryTitle = "Response Length Distribution";
        chartData.SecondaryIsStacked = false;
        chartData.SecondaryHideAttributeFilter = false;

        return chartData;
    }

    private async Task<FlexibleAnalyticsChartData> GenerateOverviewChartsAsync(List<Response> baseData, FlexibleAnalyticsFilters filters)
    {
        var chartData = new FlexibleAnalyticsChartData
        {
            Title = "Overview",
            Subtitle = "Multi-question analysis"
        };

        // Primary chart: Response rate by section
        chartData.PrimaryChartType = "bar";
        chartData.PrimaryTitle = "Response Rate by Section";
        
        var sectionData = baseData
            .GroupBy(r => r.Question.Section ?? "Other")
            .Select(g => new
            {
                Section = g.Key,
                Count = g.Count(),
                Companies = g.Select(r => r.CampaignAssignment.TargetOrganizationId).Distinct().Count()
            })
            .ToList();

        chartData.PrimaryData = sectionData
            .Select(s => new ChartDataPoint
            {
                Label = s.Section,
                Value = s.Count,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Companies"] = s.Companies
                }
            })
            .OrderByDescending(d => d.Value)
            .ToList();

        // Secondary chart: Company completion rates
        chartData.SecondaryChartType = "bar";
        chartData.SecondaryTitle = "Company Completion Rates";
        
        // Get all campaign assignments to calculate proper completion rates
        var campaignIds = baseData.Select(r => r.CampaignAssignment.CampaignId).Distinct().ToList();
        var allAssignments = await _context.CampaignAssignments
            .Include(ca => ca.TargetOrganization)
            .Where(ca => campaignIds.Contains(ca.CampaignId))
            .ToListAsync();
            
        var totalQuestionsByCompany = allAssignments
            .GroupBy(ca => ca.TargetOrganization.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        var companyData = baseData
            .GroupBy(r => r.CampaignAssignment.TargetOrganization.Name)
            .Select(g => new
            {
                Company = g.Key,
                AnsweredQuestions = g.Select(r => r.Question.Id).Distinct().Count(),
                TotalQuestions = totalQuestionsByCompany.GetValueOrDefault(g.Key, 1), // Avoid division by zero
                Responses = g.Count()
            })
            .Where(c => c.TotalQuestions > 0) // Filter out invalid data
            .ToList();

        chartData.SecondaryData = companyData
            .Select(c => new ChartDataPoint
            {
                Label = c.Company,
                Value = Math.Round((decimal)c.AnsweredQuestions / c.TotalQuestions * 100, 1), // Completion percentage
                AdditionalData = new Dictionary<string, object>
                {
                    ["AnsweredQuestions"] = c.AnsweredQuestions,
                    ["TotalQuestions"] = c.TotalQuestions,
                    ["Responses"] = c.Responses
                }
            })
            .OrderByDescending(d => d.Value)
            .Take(10)
            .ToList();

        return chartData;
    }

    private Task<FlexibleAnalyticsDataTable> GenerateDataTableAsync(List<Response> baseData, FlexibleAnalyticsFilters filters)
    {
        // Get total count for pagination
        var totalCount = baseData.Count;
        
        // Apply sorting and pagination
        var sortedResponses = ApplySorting(baseData.AsQueryable(), filters.SortBy, filters.SortDescending).ToList();
        
        // Apply pagination
        var responses = sortedResponses
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToList();

        // Convert to data rows
        var rows = responses.Select(r => new FlexibleAnalyticsDataRow
        {
            CompanyId = r.CampaignAssignment.TargetOrganizationId,
            CompanyName = r.CampaignAssignment.TargetOrganization.Name,
            Sector = GetSectorFromResponse(r),
            Industry = GetAttributeFromResponse(r, "Industry"),
            Region = GetAttributeFromResponse(r, "Region"),
            Size = GetAttributeFromResponse(r, "Size"),
            Year = r.CampaignAssignment.Campaign.ReportingPeriodStart!.Value.Year + 1,
            QuestionId = r.Question.Id,
            QuestionText = r.Question.QuestionText,
            Section = r.Question.Section ?? "Other",
            QuestionType = r.Question.QuestionType.ToString(),
            TextValue = r.TextValue,
            NumericValue = r.NumericValue,
            BooleanValue = r.BooleanValue,
            SelectedValues = r.SelectedValues,
            Unit = r.Question.Unit,
            IsPercentage = r.Question.IsPercentage,
            FormattedValue = FormatResponseValue(r),
            OrganizationAttributes = GetAllAttributesFromResponse(r)
        }).ToList();

        return Task.FromResult(new FlexibleAnalyticsDataTable
        {
            Rows = rows,
            TotalRows = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize),
            Columns = GetTableColumns()
        });
    }

    // Helper methods

    private string GetSectorFromResponse(Response response)
    {
        var attributes = response.CampaignAssignment.OrganizationRelationship?.Attributes;
        if (attributes == null || !attributes.Any())
        {
            return "Unknown";
        }
        
        // Use the first available attribute as the "sector" for display purposes
        var firstAttribute = attributes.OrderBy(a => a.AttributeType).First();
        return firstAttribute.AttributeValue ?? "Unknown";
    }

    private string GetAttributeValueFromResponse(Response response, string attributeType)
    {
        // Handle Company attribute specially
        if (attributeType == "Company")
        {
            return response.CampaignAssignment.TargetOrganization.Name;
        }

        var relationship = response.CampaignAssignment.OrganizationRelationship;
        if (relationship == null)
        {
            _logger.LogWarning($"No organization relationship found for response ID {response.Id}");
            return "Unknown";
        }

        var attributes = relationship.Attributes;
        if (attributes == null || !attributes.Any())
        {
            _logger.LogWarning($"No attributes found for organization relationship ID {relationship.Id}");
            return "Unknown";
        }

        var attribute = attributes.FirstOrDefault(a => a.AttributeType == attributeType);
        if (attribute == null)
        {
            _logger.LogWarning($"No attribute of type '{attributeType}' found. Available types: {string.Join(", ", attributes.Select(a => a.AttributeType))}");
            return "Unknown";
        }

        return attribute.AttributeValue ?? "Unknown";
    }

    private string? GetAttributeFromResponse(Response response, string attributeName)
    {
        return response.CampaignAssignment.OrganizationRelationship?.Attributes
            .FirstOrDefault(a => a.AttributeType == attributeName)?.AttributeValue;
    }

    private Dictionary<string, string> GetAllAttributesFromResponse(Response response)
    {
        return response.CampaignAssignment.OrganizationRelationship?.Attributes
            .ToDictionary(a => a.AttributeType, a => a.AttributeValue) ?? new Dictionary<string, string>();
    }

    private string FormatResponseValue(Response response)
    {
        if (response.BooleanValue.HasValue)
            return response.BooleanValue.Value ? "Yes" : "No";
        
        if (response.NumericValue.HasValue)
        {
            var value = response.NumericValue.Value;
            if (response.Question.IsPercentage)
                return $"{value:F1}%";
            if (!string.IsNullOrEmpty(response.Question.Unit))
                return $"{value:N2} {response.Question.Unit}";
            return value.ToString("N2");
        }
        
        if (!string.IsNullOrEmpty(response.SelectedValues))
            return response.SelectedValues;
        
        return response.TextValue ?? "";
    }

    private List<string> GetTableColumns()
    {
        return new List<string>
        {
            "CompanyName", "Sector", "Industry", "Region", "Size", "Year",
            "Section", "QuestionText", "QuestionType", "FormattedValue"
        };
    }

    private IQueryable<Response> ApplySorting(IQueryable<Response> query, string sortBy, bool descending)
    {
        return sortBy switch
        {
            "CompanyName" => descending 
                ? query.OrderByDescending(r => r.CampaignAssignment.TargetOrganization.Name)
                : query.OrderBy(r => r.CampaignAssignment.TargetOrganization.Name),
            "Year" => descending 
                ? query.OrderByDescending(r => r.CampaignAssignment.Campaign.ReportingPeriodStart)
                : query.OrderBy(r => r.CampaignAssignment.Campaign.ReportingPeriodStart),
            "Section" => descending 
                ? query.OrderByDescending(r => r.Question.Section)
                : query.OrderBy(r => r.Question.Section),
            "QuestionText" => descending 
                ? query.OrderByDescending(r => r.Question.QuestionText)
                : query.OrderBy(r => r.Question.QuestionText),
            _ => query.OrderBy(r => r.CampaignAssignment.TargetOrganization.Name)
        };
    }

    private decimal CalculateMedian(List<decimal> values)
    {
        if (!values.Any()) return 0;
        
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    private List<ChartDataPoint> CreateDistributionBins(List<decimal> values)
    {
        if (!values.Any()) return new List<ChartDataPoint>();

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        if (range == 0)
        {
            return new List<ChartDataPoint>
            {
                new ChartDataPoint 
                { 
                    Label = min.ToString("F2"), 
                    Value = values.Count, 
                    Color = "#007bff" 
                }
            };
        }

        var binCount = Math.Min(10, values.Count);
        var binSize = range / binCount;

        var bins = new List<ChartDataPoint>();
        for (int i = 0; i < binCount; i++)
        {
            var binStart = min + i * binSize;
            var binEnd = i == binCount - 1 ? max : min + (i + 1) * binSize;
            
            var count = values.Count(v => v >= binStart && v < binEnd);
            if (i == binCount - 1) count = values.Count(v => v >= binStart && v <= binEnd);

            bins.Add(new ChartDataPoint
            {
                Label = $"{binStart:F1}-{binEnd:F1}",
                Value = count,
                Color = "#007bff"
            });
        }

        return bins;
    }

    private List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        
        var commonWords = new HashSet<string> { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "a", "an", "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can", "cannot", "we", "our", "us", "you", "your", "they", "their", "them", "it", "its", "this", "that", "these", "those" };
        
        return text
            .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3 && !commonWords.Contains(word.ToLower()))
            .Select(word => word.ToLower())
            .ToList();
    }

    private bool IsPercentageQuestion(Question question)
    {
        // Check the IsPercentage flag first
        if (question.IsPercentage)
        {
            return true;
        }
        
        // Check for percentage indicators in question text
        var questionText = question.QuestionText?.ToLower() ?? "";
        var percentageKeywords = new[] { "percentage", "percent", "%", "rate", "proportion", "share" };
        
        foreach (var keyword in percentageKeywords)
        {
            if (questionText.Contains(keyword))
            {
                return true;
            }
        }
        
        // Check unit field
        var unit = question.Unit?.ToLower() ?? "";
        if (unit.Contains("percent") || unit.Contains("%"))
        {
            return true;
        }
        
        return false;
    }

    private string GetAggregationMethodDisplayName(NumericAggregationMethod method)
    {
        return method switch
        {
            NumericAggregationMethod.Sum => "Total (Sum)",
            NumericAggregationMethod.Average => "Average",
            NumericAggregationMethod.Count => "Count of Responses",
            NumericAggregationMethod.Min => "Minimum Value",
            NumericAggregationMethod.Max => "Maximum Value",
            _ => method.ToString()
        };
    }

    private decimal ApplyAggregationMethod(List<decimal> values, NumericAggregationMethod method)
    {
        if (!values.Any()) return 0;

        return method switch
        {
            NumericAggregationMethod.Sum => values.Sum(),
            NumericAggregationMethod.Average => values.Average(),
            NumericAggregationMethod.Count => values.Count,
            NumericAggregationMethod.Min => values.Min(),
            NumericAggregationMethod.Max => values.Max(),
            _ => values.Sum()
        };
    }

    private List<string> GetColorPalette()
    {
        return new List<string>
        {
            "#0d6efd", // Blue
            "#20c997", // Teal
            "#fd7e14", // Orange
            "#6f42c1", // Purple
            "#dc3545", // Red
            "#198754", // Green
            "#0dcaf0", // Light Blue
            "#ffc107", // Yellow
            "#e83e8c", // Pink
            "#6c757d", // Gray
            "#17a2b8", // Info Blue
            "#28a745", // Success Green
            "#ffc107", // Warning Yellow
            "#dc3545", // Danger Red
            "#6610f2", // Indigo
            "#e83e8c", // Pink
            "#fd7e14", // Orange
            "#20c997", // Teal
            "#6f42c1", // Purple
            "#0dcaf0"  // Light Blue
        };
    }
} 