using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ESGPlatform.Models;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using ESGPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<User> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var viewModel = new HomeViewModel();

        // Determine what to show based on user role
        viewModel.ShowCampaigns = IsPlatformAdmin || IsCampaignManager;

        // Build the dashboard data
        await BuildDashboardSummary(viewModel, user);
        await BuildMyAssignments(viewModel, user);
        await BuildMyReviews(viewModel, user);
        
        if (viewModel.ShowCampaigns)
        {
            await BuildMyCampaigns(viewModel, user);
        }

        // Set flags for conditional display
        viewModel.ShowReviews = viewModel.MyReviews.Any();

        return View(viewModel);
    }

    private async Task BuildDashboardSummary(HomeViewModel viewModel, User user)
    {
        var summary = viewModel.Summary;

        // Get user's assignments
        var assignmentsQuery = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Where(ca => ca.LeadResponderId == user.Id);

        // Add organization filter for supplier users
        if (!IsPlatformAdmin)
        {
            assignmentsQuery = assignmentsQuery.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
        }

        var assignments = await assignmentsQuery.ToListAsync();

        summary.TotalAssignments = assignments.Count;
        summary.CompletedAssignments = assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved);
        summary.InProgressAssignments = assignments.Count(a => a.Status == AssignmentStatus.InProgress);
        summary.OverdueAssignments = assignments.Count(a => 
            a.Campaign.Deadline.HasValue && 
            a.Campaign.Deadline.Value < DateTime.Now && 
            a.Status != AssignmentStatus.Submitted && 
            a.Status != AssignmentStatus.Approved);

        // Get user's reviews
        var reviewsQuery = _context.ReviewAssignments
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Where(ra => ra.ReviewerId == user.Id);

        if (!IsPlatformAdmin)
        {
            reviewsQuery = reviewsQuery.Where(ra => ra.CampaignAssignment.TargetOrganizationId == CurrentOrganizationId);
        }

        var reviews = await reviewsQuery.ToListAsync();

        summary.PendingReviews = reviews.Count(r => r.Status == ReviewStatus.Pending || r.Status == ReviewStatus.InReview);
        summary.CompletedReviews = reviews.Count(r => r.Status == ReviewStatus.Completed || r.Status == ReviewStatus.Approved);

        // Get campaigns if user can see them
        if (viewModel.ShowCampaigns)
        {
            var campaignsQuery = _context.Campaigns.AsQueryable();

            if (!IsPlatformAdmin)
            {
                campaignsQuery = campaignsQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
            }

            var campaigns = await campaignsQuery.ToListAsync();

            summary.MyCampaignsCount = campaigns.Count;
            summary.ActiveCampaignsCount = campaigns.Count(c => c.Status == CampaignStatus.Active);
        }
    }

    private async Task BuildMyAssignments(HomeViewModel viewModel, User user)
    {
        var assignmentsQuery = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.Responses)
            .Include(ca => ca.QuestionAssignments)
            .Where(ca => ca.LeadResponderId == user.Id);

        // Add organization filter for supplier users
        if (!IsPlatformAdmin)
        {
            assignmentsQuery = assignmentsQuery.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
        }

        var assignments = await assignmentsQuery
            .OrderByDescending(ca => ca.Status == AssignmentStatus.ChangesRequested)
            .ThenByDescending(ca => ca.Status == AssignmentStatus.InProgress)
            .ThenBy(ca => ca.Campaign.Deadline)
            .ThenByDescending(ca => ca.UpdatedAt ?? ca.CreatedAt)
            .Take(10)
            .ToListAsync();

        viewModel.MyAssignments = assignments.Select(a => new MyAssignmentViewModel
        {
            AssignmentId = a.Id,
            CampaignName = a.Campaign.Name,
            QuestionnaireName = a.QuestionnaireVersion.Questionnaire.Title,
            OrganizationName = a.TargetOrganization.Name,
            Status = a.Status,
            Deadline = a.Campaign.Deadline,
            LastActivityDate = a.UpdatedAt ?? a.CreatedAt,
            QuestionsAnswered = a.Responses.Count(r => 
                !string.IsNullOrWhiteSpace(r.TextValue) || 
                r.NumericValue.HasValue || 
                r.DateValue.HasValue || 
                r.BooleanValue.HasValue || 
                !string.IsNullOrWhiteSpace(r.SelectedValues)),
            TotalQuestions = a.QuestionnaireVersion.Questionnaire.Questions.Count,
            IsLeadResponder = true,
            IsDelegated = a.QuestionAssignments.Any(qa => qa.AssignedUserId != user.Id)
        }).ToList();
    }

    private async Task BuildMyReviews(HomeViewModel viewModel, User user)
    {
        var reviewsQuery = _context.ReviewAssignments
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(ra => ra.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(ra => ra.Question)
            .Include(ra => ra.Comments)
            .Where(ra => ra.ReviewerId == user.Id);

        if (!IsPlatformAdmin)
        {
            reviewsQuery = reviewsQuery.Where(ra => ra.CampaignAssignment.TargetOrganizationId == CurrentOrganizationId);
        }

        var reviews = await reviewsQuery
            .OrderByDescending(ra => ra.Status == ReviewStatus.Pending)
            .ThenByDescending(ra => ra.Status == ReviewStatus.InReview)
            .ThenBy(ra => ra.CampaignAssignment.Campaign.Deadline)
            .ThenByDescending(ra => ra.UpdatedAt ?? ra.CreatedAt)
            .Take(10)
            .ToListAsync();

        viewModel.MyReviews = reviews.Select(r => new MyReviewAssignmentViewModel
        {
            ReviewAssignmentId = r.Id,
            CampaignName = r.CampaignAssignment.Campaign.Name,
            OrganizationName = r.CampaignAssignment.TargetOrganization.Name,
            QuestionnaireName = r.CampaignAssignment.QuestionnaireVersion.Questionnaire.Title,
            Scope = r.Scope,
            SectionName = r.SectionName,
            QuestionText = r.Question?.QuestionText,
            Status = r.Status,
            AssignedAt = r.CreatedAt,
            Deadline = r.CampaignAssignment.Campaign.Deadline,
            CompletedAt = r.Status == ReviewStatus.Completed ? r.UpdatedAt : null,
            PendingResponsesCount = GetPendingResponsesCount(r),
            TotalResponsesCount = GetTotalResponsesCount(r)
        }).ToList();
    }

    private async Task BuildMyCampaigns(HomeViewModel viewModel, User user)
    {
        var campaignsQuery = _context.Campaigns
            .Include(c => c.Assignments)
                .ThenInclude(a => a.TargetOrganization)
            .AsQueryable();

        if (!IsPlatformAdmin)
        {
            campaignsQuery = campaignsQuery.Where(c => c.OrganizationId == CurrentOrganizationId);
        }

        var campaigns = await campaignsQuery
            .Where(c => c.Status != CampaignStatus.Completed && c.Status != CampaignStatus.Cancelled)
            .OrderByDescending(c => c.Status == CampaignStatus.Active)
            .ThenBy(c => c.Deadline)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(10)
            .ToListAsync();

        viewModel.MyCampaigns = campaigns.Select(c => new MyCampaignViewModel
        {
            CampaignId = c.Id,
            Name = c.Name,
            Description = c.Description,
            Status = c.Status,
            Deadline = c.Deadline,
            CreatedAt = c.CreatedAt,
            LastActivityDate = c.UpdatedAt ?? c.CreatedAt,
            TotalAssignments = c.Assignments.Count,
            CompletedAssignments = c.Assignments.Count(a => a.Status == AssignmentStatus.Submitted || a.Status == AssignmentStatus.Approved),
            InProgressAssignments = c.Assignments.Count(a => a.Status == AssignmentStatus.InProgress),
            OverdueAssignments = c.Assignments.Count(a => 
                c.Deadline.HasValue && 
                c.Deadline.Value < DateTime.Now && 
                a.Status != AssignmentStatus.Submitted && 
                a.Status != AssignmentStatus.Approved)
        }).ToList();
    }

    private int GetPendingResponsesCount(ReviewAssignment reviewAssignment)
    {
        // This would need to be implemented based on the specific review scope
        // For now, return a placeholder
        return reviewAssignment.Scope switch
        {
            ReviewScope.Assignment => reviewAssignment.CampaignAssignment.Responses.Count(r => 
                !string.IsNullOrWhiteSpace(r.TextValue) || 
                r.NumericValue.HasValue || 
                r.DateValue.HasValue || 
                r.BooleanValue.HasValue || 
                !string.IsNullOrWhiteSpace(r.SelectedValues)),
            ReviewScope.Section => reviewAssignment.CampaignAssignment.Responses.Count(r => 
                (!string.IsNullOrWhiteSpace(r.TextValue) || 
                 r.NumericValue.HasValue || 
                 r.DateValue.HasValue || 
                 r.BooleanValue.HasValue || 
                 !string.IsNullOrWhiteSpace(r.SelectedValues)) && 
                r.Question.Section == reviewAssignment.SectionName),
            ReviewScope.Question => reviewAssignment.CampaignAssignment.Responses.Count(r => 
                r.QuestionId == reviewAssignment.QuestionId && 
                (!string.IsNullOrWhiteSpace(r.TextValue) || 
                 r.NumericValue.HasValue || 
                 r.DateValue.HasValue || 
                 r.BooleanValue.HasValue || 
                 !string.IsNullOrWhiteSpace(r.SelectedValues))),
            _ => 0
        };
    }

    private int GetTotalResponsesCount(ReviewAssignment reviewAssignment)
    {
        // This would need to be implemented based on the specific review scope
        // For now, return a placeholder
        return reviewAssignment.Scope switch
        {
            ReviewScope.Assignment => reviewAssignment.CampaignAssignment.QuestionnaireVersion.Questionnaire.Questions.Count,
            ReviewScope.Section => reviewAssignment.CampaignAssignment.QuestionnaireVersion.Questionnaire.Questions.Count(q => 
                q.Section == reviewAssignment.SectionName),
            ReviewScope.Question => 1,
            _ => 0
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
