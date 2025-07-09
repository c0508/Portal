using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ESGPlatform.Services;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Models;
using ESGPlatform.Extensions;
using ESGPlatform.Models.Entities;
using System.Security.Claims;
using System.Text.Json;
using System.Diagnostics;
using ESGPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace ESGPlatform.Controllers;

[Authorize]
public class ESGAnalyticsController : Controller
{
    private readonly IESGAnalyticsService _esgAnalyticsService;
    private readonly ILogger<ESGAnalyticsController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IFlexibleAnalyticsService _flexibleAnalyticsService;

    public ESGAnalyticsController(
        IESGAnalyticsService esgAnalyticsService,
        ILogger<ESGAnalyticsController> logger,
        ApplicationDbContext context,
        IFlexibleAnalyticsService flexibleAnalyticsService)
    {
        _esgAnalyticsService = esgAnalyticsService;
        _logger = logger;
        _context = context;
        _flexibleAnalyticsService = flexibleAnalyticsService;
    }

    // ===================================================================
    // Portfolio Overview Dashboard
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> Index(int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            // Ensure user is from a platform organization
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("ESG Analytics is only available for Platform Organizations");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            
            if (!availableYears.Any())
            {
                ViewBag.NoDataMessage = "No ESG data available. Please ensure campaigns have been completed with responses.";
                return View("NoData");
            }

            var selectedYear = year ?? availableYears.First();
            var portfolioOverview = await _esgAnalyticsService.GetPortfolioOverviewAsync(platformOrgId, selectedYear);

            var viewModel = new ESGAnalyticsDashboardViewModel
            {
                PortfolioOverview = portfolioOverview,
                QuickInsights = GenerateQuickInsightsAsync(portfolioOverview),
                Alerts = GenerateAlertsAsync(platformOrgId, selectedYear),
                ProgressSummary = GenerateProgressSummaryAsync(platformOrgId, availableYears)
            };

            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ESG Analytics dashboard");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    // ===================================================================
    // Portfolio Overview Dashboard (Main Entry Point)
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> Portfolio(int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            // Ensure user is from a platform organization
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("ESG Analytics is only available for Platform Organizations");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            
            if (!availableYears.Any())
            {
                ViewBag.NoDataMessage = "No ESG data available. Please ensure campaigns have been completed with responses.";
                return View("NoData");
            }

            var selectedYear = year ?? availableYears.First();
            var serviceData = await _esgAnalyticsService.GetPortfolioOverviewAsync(platformOrgId, selectedYear);

            // Debug logging
            _logger.LogInformation($"Selected year: {selectedYear}, Total companies: {serviceData.TotalCompanies}, Top performers count: {serviceData.TopPerformers.Count}, Policy adoption rates count: {serviceData.PolicyAdoptionRates.Count}");

            // Map service data to the view model that the Portfolio view expects
            var viewModel = new PortfolioOverviewViewModel
            {
                TotalCompanies = serviceData.TotalCompanies,
                
                // Calculate overall ESG score from top performers
                OverallESGScore = serviceData.TopPerformers.Any() 
                    ? serviceData.TopPerformers.Average(p => p.ESGScore) 
                    : 0,
                
                // Calculate climate policy adoption
                ClimatePolicyAdoption = serviceData.PolicyAdoptionRates.ContainsKey("Does your company have a formal climate change policy?") 
                    ? serviceData.PolicyAdoptionRates["Does your company have a formal climate change policy?"] / 100 
                    : 0,
                
                CompaniesWithClimatePolicy = serviceData.PolicyAdoptionRates.ContainsKey("Does your company have a formal climate change policy?") 
                    ? (int)Math.Round(serviceData.PolicyAdoptionRates["Does your company have a formal climate change policy?"] / 100 * serviceData.TotalCompanies)
                    : 0,
                
                // Calculate women leadership average
                AverageWomenLeadership = serviceData.QuantitativeBenchmarks.ContainsKey("Percentage of women in leadership positions") 
                    ? serviceData.QuantitativeBenchmarks["Percentage of women in leadership positions"] 
                    : 0,
                
                // Default values for missing temporal data
                ESGScoreChange = 0,
                WomenLeadershipImprovement = 0,
                TotalEmissionsReduction = 0,
                
                // Map top performers directly
                TopPerformers = serviceData.TopPerformers,
                
                // Create bottom performers (last 3 in reverse order)
                BottomPerformers = serviceData.TopPerformers.OrderBy(p => p.ESGScore).Take(3).ToList(),
                
                // Transform policy adoption rates from dictionary to list
                PolicyAdoptionRates = serviceData.PolicyAdoptionRates.Select(kvp => new PolicyAdoptionRateViewModel
                {
                    PolicyName = kvp.Key,
                    AdoptionRate = kvp.Value / 100 // Convert percentage to decimal
                }).ToList(),
                
                // Create company performances from top performers data with real policy data
                CompanyPerformances = new List<CompanyPerformanceViewModel>(),
                
                // Create trend data with multiple years
                TrendData = new List<TrendDataViewModel>(),
                
                // Transform sector breakdown to sector performances
                SectorPerformances = serviceData.SectorBreakdown.Select(kvp => new SectorPerformanceViewModel
                {
                    Sector = kvp.Key,
                    AverageESGScore = serviceData.TopPerformers
                        .Where(p => p.Sector == kvp.Key)
                        .Any() ? serviceData.TopPerformers
                        .Where(p => p.Sector == kvp.Key)
                        .Average(p => p.ESGScore) : 0
                }).ToList()
            };

            // Populate trend data for all available years
            foreach (var trendYear in availableYears.OrderBy(y => y))
            {
                try
                {
                    var yearData = await _esgAnalyticsService.GetPortfolioOverviewAsync(platformOrgId, trendYear);
                    viewModel.TrendData.Add(new TrendDataViewModel
                    {
                        Year = trendYear,
                        AverageESGScore = yearData.TopPerformers.Any() 
                            ? yearData.TopPerformers.Average(p => p.ESGScore) 
                            : 0
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get trend data for year {Year}", trendYear);
                }
            }

            // Populate company performances with actual policy data from database
            await PopulateCompanyPerformancesAsync(viewModel, serviceData, platformOrgId, selectedYear);

            // Pass available years for the dropdown
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Portfolio Overview dashboard");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    // ===================================================================
    // Portfolio Overview API
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> PortfolioOverview(int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var overview = await _esgAnalyticsService.GetPortfolioOverviewAsync(platformOrgId, year);
            return Json(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio overview");
            return StatusCode(500, new { error = "Failed to load portfolio overview" });
        }
    }

    // ===================================================================
    // Temporal Analysis
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> TemporalAnalysis(string? years)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            
            List<int> selectedYears;
            if (!string.IsNullOrEmpty(years))
            {
                selectedYears = years.Split(',').Select(int.Parse).Where(y => availableYears.Contains(y)).ToList();
            }
            else
            {
                selectedYears = availableYears.Take(4).ToList(); // Default to last 4 years
            }

            if (selectedYears.Count < 2)
            {
                ViewBag.ErrorMessage = "Temporal analysis requires at least 2 years of data.";
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                });
            }

            var temporalAnalysis = await _esgAnalyticsService.GetTemporalAnalysisAsync(platformOrgId, selectedYears);
            
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYears = selectedYears;

            return View(temporalAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading temporal analysis");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTemporalData(string years)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var selectedYears = years.Split(',').Select(int.Parse).ToList();
            var temporalAnalysis = await _esgAnalyticsService.GetTemporalAnalysisAsync(platformOrgId, selectedYears);
            
            return Json(temporalAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting temporal data");
            return StatusCode(500, new { error = "Failed to load temporal data" });
        }
    }

    // ===================================================================
    // Sector Benchmarking
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> SectorBenchmarking(int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            var selectedYear = year ?? availableYears.FirstOrDefault();

            if (selectedYear == 0)
            {
                ViewBag.ErrorMessage = "No data available for sector benchmarking.";
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                });
            }

            var sectorBenchmarking = await _esgAnalyticsService.GetSectorBenchmarkingAsync(platformOrgId, selectedYear);
            
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;

            return View(sectorBenchmarking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sector benchmarking");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSectorData(int year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var sectorData = await _esgAnalyticsService.GetSectorBenchmarkingAsync(platformOrgId, year);
            return Json(sectorData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sector data");
            return StatusCode(500, new { error = "Failed to load sector data" });
        }
    }

    // ===================================================================
    // Company Deep Dive
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> CompanyDeepDive(int companyId, int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            var selectedYear = year ?? availableYears.FirstOrDefault();

            if (selectedYear == 0)
            {
                ViewBag.ErrorMessage = "No data available for this company.";
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                });
            }

            var companyDeepDive = await _esgAnalyticsService.GetCompanyDeepDiveAsync(platformOrgId, companyId, selectedYear);
            
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;

            return View(companyDeepDive);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No assignment found"))
        {
            ViewBag.ErrorMessage = $"No ESG data found for this company in {year}.";
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading company deep dive for company {CompanyId}", companyId);
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    // ===================================================================
    // Question-Level Analytics
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> QuestionAnalysis(int questionId, int? year)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var availableYears = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            var selectedYear = year ?? availableYears.FirstOrDefault();

            if (selectedYear == 0)
            {
                ViewBag.ErrorMessage = "No data available for question analysis.";
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                });
            }

            var questionAnalysis = await _esgAnalyticsService.GetQuestionLevelAnalyticsAsync(platformOrgId, questionId, selectedYear);
            
            ViewBag.AvailableYears = availableYears;
            ViewBag.SelectedYear = selectedYear;

            return View(questionAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading question analysis for question {QuestionId}", questionId);
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    // ===================================================================
    // Flexible Analytics Dashboard
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> FlexibleAnalytics(
        [FromQuery] List<int> years,
        [FromQuery] List<string> sections,
        [FromQuery] List<int> questionIds,
        [FromQuery] List<int> companyIds,
        [FromQuery] List<string> sectors,
        [FromQuery] string? searchTerm,
        [FromQuery] string? selectedAttribute,
        [FromQuery] string? aggregationMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "CompanyName",
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            // Debug logging for incoming parameters
            _logger.LogInformation($"FlexibleAnalytics called with selectedAttribute: '{selectedAttribute}', aggregationMethod: '{aggregationMethod}'");
            _logger.LogInformation($"QuestionIds: [{string.Join(", ", questionIds)}]");

            // Parse aggregation method
            var parsedAggregationMethod = NumericAggregationMethod.Sum;
            if (!string.IsNullOrEmpty(aggregationMethod) && Enum.TryParse<NumericAggregationMethod>(aggregationMethod, out var method))
            {
                parsedAggregationMethod = method;
            }
            
            // Handle selectedAttribute - only default to "Sector" if null/empty
            var actualSelectedAttribute = selectedAttribute ?? "Sector";
            _logger.LogInformation($"Selected attribute before: '{selectedAttribute}', after: '{actualSelectedAttribute}'");
            
            var filters = new FlexibleAnalyticsFilters
            {
                Years = years,
                Sections = sections,
                QuestionIds = questionIds,
                CompanyIds = companyIds,
                Sectors = sectors,
                SearchTerm = searchTerm,
                SelectedAttribute = actualSelectedAttribute,
                AggregationMethod = parsedAggregationMethod,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var viewModel = await _flexibleAnalyticsService.GetAnalyticsAsync(platformOrgId, filters);

            // Debug logging for the result
            _logger.LogInformation($"Controller result: SelectedAttribute='{viewModel.Filters.SelectedAttribute}', AggregationMethod='{viewModel.Filters.AggregationMethod}'");
            _logger.LogInformation($"Controller: ChartData - Primary count: {viewModel.ChartData.PrimaryData.Count}, Secondary count: {viewModel.ChartData.SecondaryData.Count}");
            _logger.LogInformation($"Controller: Chart types - Primary: {viewModel.ChartData.PrimaryChartType}, Secondary: {viewModel.ChartData.SecondaryChartType}");
            _logger.LogInformation($"Controller: Summary - Total responses: {viewModel.ChartData.Summary.TotalResponses}");

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Flexible Analytics dashboard");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportFlexibleAnalytics([FromForm] FlexibleAnalyticsFilters filters)
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var excelData = await _flexibleAnalyticsService.ExportToExcelAsync(platformOrgId, filters);
            
            var fileName = $"ESG_Analytics_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting flexible analytics data");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(
        [FromQuery] List<int> years,
        [FromQuery] List<string> sections,
        [FromQuery] List<int> questionIds,
        [FromQuery] List<int> companyIds,
        [FromQuery] List<string> sectors,
        [FromQuery] string? searchTerm,
        [FromQuery] string? selectedAttribute,
        [FromQuery] string? aggregationMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "CompanyName",
        [FromQuery] bool sortDescending = false)
          {
          try
          {
              var platformOrganizationId = HttpContext.GetCurrentUserOrganizationId();
              
              if (!IsFromPlatformOrganizationAsync(platformOrganizationId))
              {
                  return Forbid("Access denied");
              }

              // Create filters object from query parameters
              var filters = new FlexibleAnalyticsFilters
              {
                  Years = years ?? new List<int>(),
                  Sections = sections ?? new List<string>(),
                  QuestionIds = questionIds ?? new List<int>(),
                  CompanyIds = companyIds ?? new List<int>(),
                  Sectors = sectors ?? new List<string>(),
                  SearchTerm = searchTerm ?? "",
                  SelectedAttribute = selectedAttribute ?? "Industry",
                  AggregationMethod = Enum.TryParse<NumericAggregationMethod>(aggregationMethod, out var method) ? method : NumericAggregationMethod.Sum,
                  Page = page,
                  PageSize = pageSize,
                  SortBy = sortBy,
                  SortDescending = sortDescending
              };

              _logger.LogInformation($"GetChartData called with filters: Years={string.Join(",", filters.Years)}, Sections={string.Join(",", filters.Sections)}, QuestionIds={string.Join(",", filters.QuestionIds)}, Attribute={filters.SelectedAttribute}, Aggregation={filters.AggregationMethod}");

              // Log the incoming filter details for debugging
              _logger.LogInformation($"Received AggregationMethod: {filters.AggregationMethod}");
              _logger.LogInformation($"Question IDs count: {filters.QuestionIds?.Count ?? 0}");
              
              var baseData = await _flexibleAnalyticsService.GetAnalyticsAsync(platformOrganizationId, filters);
            
            _logger.LogInformation($"Primary chart type: {baseData.ChartData.PrimaryChartType}");
            _logger.LogInformation($"Primary chart title: {baseData.ChartData.PrimaryTitle}");
            _logger.LogInformation($"Primary chart data count: {baseData.ChartData.PrimaryData?.Count ?? 0}");
            _logger.LogInformation($"Secondary chart type: {baseData.ChartData.SecondaryChartType}");
            _logger.LogInformation($"Secondary is stacked: {baseData.ChartData.SecondaryIsStacked}");

            return Json(new { 
                success = true, 
                chartData = new {
                    primaryTitle = baseData.ChartData.PrimaryTitle,
                    secondaryTitle = baseData.ChartData.SecondaryTitle,
                    primaryData = baseData.ChartData.PrimaryData,
                    secondaryData = baseData.ChartData.SecondaryData,
                    primaryChartType = baseData.ChartData.PrimaryChartType,
                    secondaryChartType = baseData.ChartData.SecondaryChartType,
                    secondaryIsStacked = baseData.ChartData.SecondaryIsStacked,
                    secondaryHideAttributeFilter = baseData.ChartData.SecondaryHideAttributeFilter
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetChartData");
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ===================================================================
    // Data Export Endpoints
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> ExportPortfolioData(int year, string format = "json")
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            
            if (!IsFromPlatformOrganizationAsync(platformOrgId))
            {
                return Forbid("Access denied");
            }

            var portfolioData = await _esgAnalyticsService.GetPortfolioOverviewAsync(platformOrgId, year);

            switch (format.ToLower())
            {
                case "json":
                    var jsonContent = JsonSerializer.Serialize(portfolioData, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    return File(System.Text.Encoding.UTF8.GetBytes(jsonContent), 
                              "application/json", 
                              $"esg-portfolio-{year}.json");

                case "csv":
                    // TODO: Implement CSV export
                    return BadRequest("CSV export not yet implemented");

                default:
                    return BadRequest("Unsupported format. Use 'json' or 'csv'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting portfolio data");
            return StatusCode(500, "Export failed");
        }
    }

    // ===================================================================
    // API Endpoints for Charts and Widgets
    // ===================================================================

    [HttpGet]
    public async Task<IActionResult> GetAvailableYears()
    {
        try
        {
            var platformOrgId = HttpContext.GetCurrentUserOrganizationId();
            var years = await _esgAnalyticsService.GetAvailableReportingYearsAsync(platformOrgId);
            return Json(years);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available years");
            return StatusCode(500, new { error = "Failed to load available years" });
        }
    }

    // ===================================================================
    // Helper Methods
    // ===================================================================

    private bool IsFromPlatformOrganizationAsync(int organizationId)
    {
        // TODO: Add logic to check if organization is a platform organization
        // For now, assume all organizations have access (will be refined)
        return true;
    }

    private List<ESGQuickInsightViewModel> GenerateQuickInsightsAsync(ESGPortfolioOverviewViewModel portfolioOverview)
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

        // Completion rate insight
        insights.Add(new ESGQuickInsightViewModel
        {
            Title = "Data Completion",
            Value = $"{portfolioOverview.CompletionRate:F1}%",
            Trend = portfolioOverview.CompletionRate >= 90 ? "up" : portfolioOverview.CompletionRate >= 70 ? "stable" : "down",
            Description = "Portfolio response completion rate",
            Category = "Data Quality"
        });

        // ESG maturity insight
        if (portfolioOverview.ESGMaturityDistribution.ContainsKey("High (80-100)"))
        {
            var highMaturityCount = portfolioOverview.ESGMaturityDistribution["High (80-100)"];
            var highMaturityPercent = (decimal)highMaturityCount / portfolioOverview.TotalCompanies * 100;
            
            insights.Add(new ESGQuickInsightViewModel
            {
                Title = "High ESG Maturity",
                Value = $"{highMaturityCount} companies",
                Trend = highMaturityPercent >= 30 ? "up" : highMaturityPercent >= 20 ? "stable" : "down",
                Description = $"{highMaturityPercent:F0}% of portfolio",
                Category = "Portfolio"
            });
        }

        return insights;
    }

    private List<ESGAlertViewModel> GenerateAlertsAsync(int platformOrgId, int year)
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

    private ESGProgressSummaryViewModel GenerateProgressSummaryAsync(int platformOrgId, List<int> availableYears)
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

    private async Task PopulateCompanyPerformancesAsync(PortfolioOverviewViewModel viewModel, ESGPortfolioOverviewViewModel serviceData, int platformOrgId, int selectedYear)
    {
        // Get campaigns for the selected year using the same logic as the service
        var campaigns = await _context.Campaigns
            .Where(c => c.OrganizationId == platformOrgId && 
                       c.ReportingPeriodStart != null &&
                       c.ReportingPeriodStart.Value.Year + 1 == selectedYear)
            .Select(c => c.Id)
            .ToListAsync();

        if (!campaigns.Any()) return;

        // Get policy responses for all companies
        var climateQuestion = "Does your company have a formal climate change policy?";
        var diversityQuestion = "Does your company have a diversity and inclusion policy?";
        var womenLeadershipQuestion = "Percentage of women in leadership positions";
        var emissionsQuestion = "Total scope 1 emissions (tCO2e)";

        var policyResponses = await _context.Responses
            .Include(r => r.Question)
            .Include(r => r.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Where(r => campaigns.Contains(r.CampaignAssignment.CampaignId) &&
                       (r.Question.QuestionText == climateQuestion ||
                        r.Question.QuestionText == diversityQuestion ||
                        r.Question.QuestionText == womenLeadershipQuestion ||
                        r.Question.QuestionText == emissionsQuestion))
            .ToListAsync();

        // Group responses by company
        var companiesByResponses = policyResponses
            .GroupBy(r => new { 
                CompanyId = r.CampaignAssignment.TargetOrganizationId,
                CompanyName = r.CampaignAssignment.TargetOrganization.Name
            })
            .ToList();

        // Create company performance records
        foreach (var companyGroup in companiesByResponses)
        {
            var responses = companyGroup.ToList();
            
            // Find the ESG score for this company from service data
            var topPerformer = serviceData.TopPerformers.FirstOrDefault(p => p.CompanyId == companyGroup.Key.CompanyId);
            if (topPerformer == null) continue;

            // Extract policy values
            var climateResponse = responses.FirstOrDefault(r => r.Question.QuestionText == climateQuestion);
            var diversityResponse = responses.FirstOrDefault(r => r.Question.QuestionText == diversityQuestion);
            var womenResponse = responses.FirstOrDefault(r => r.Question.QuestionText == womenLeadershipQuestion);
            var emissionsResponse = responses.FirstOrDefault(r => r.Question.QuestionText == emissionsQuestion);

            var companyPerformance = new CompanyPerformanceViewModel
            {
                CompanyId = companyGroup.Key.CompanyId,
                CompanyName = companyGroup.Key.CompanyName,
                Sector = topPerformer.Sector,
                ESGScore = topPerformer.ESGScore,
                HasClimatePolicy = climateResponse?.BooleanValue ?? false,
                HasDiversityPolicy = diversityResponse?.BooleanValue ?? false,
                WomenLeadershipPercentage = womenResponse?.NumericValue ?? 0,
                TotalEmissions = emissionsResponse?.NumericValue ?? 0
            };

            viewModel.CompanyPerformances.Add(companyPerformance);
        }

        // Sort by ESG score descending
        viewModel.CompanyPerformances = viewModel.CompanyPerformances
            .OrderByDescending(c => c.ESGScore)
            .ToList();
    }
} 