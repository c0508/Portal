using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Services;

public class QuestionAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionAssignmentService> _logger;
    private readonly IQuestionAssignmentTrackingService _trackingService;

    public QuestionAssignmentService(ApplicationDbContext context, ILogger<QuestionAssignmentService> logger, IQuestionAssignmentTrackingService trackingService)
    {
        _context = context;
        _logger = logger;
        _trackingService = trackingService;
    }

    #region Assignment Creation

    public async Task<QuestionAssignment> CreateQuestionAssignmentAsync(int campaignAssignmentId, int questionId, 
        string assignedUserId, string createdById, string? instructions = null)
    {
        var assignment = new QuestionAssignment
        {
            CampaignAssignmentId = campaignAssignmentId,
            QuestionId = questionId,
            AssignmentType = QuestionAssignmentType.Question,
            AssignedUserId = assignedUserId,
            CreatedById = createdById,
            Instructions = instructions,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuestionAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Track the assignment creation
        await _trackingService.TrackAssignmentCreatedAsync(assignment, createdById, "Question assigned");

        _logger.LogInformation("Question assignment created: Question {QuestionId} assigned to user {UserId} for campaign assignment {CampaignAssignmentId}", 
            questionId, assignedUserId, campaignAssignmentId);

        return assignment;
    }

    public async Task<QuestionAssignment> CreateSectionAssignmentAsync(int campaignAssignmentId, string sectionName, 
        string assignedUserId, string createdById, string? instructions = null)
    {
        var assignment = new QuestionAssignment
        {
            CampaignAssignmentId = campaignAssignmentId,
            SectionName = sectionName,
            AssignmentType = QuestionAssignmentType.Section,
            AssignedUserId = assignedUserId,
            CreatedById = createdById,
            Instructions = instructions,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuestionAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Track the assignment creation
        await _trackingService.TrackAssignmentCreatedAsync(assignment, createdById, "Section assigned");

        _logger.LogInformation("Section assignment created: Section '{SectionName}' assigned to user {UserId} for campaign assignment {CampaignAssignmentId}", 
            sectionName, assignedUserId, campaignAssignmentId);

        return assignment;
    }

    public async Task<List<QuestionAssignment>> CreateBulkAssignmentsAsync(int campaignAssignmentId, 
        List<int> questionIds, string assignedUserId, string createdById, string? instructions = null)
    {
        var assignments = new List<QuestionAssignment>();

        foreach (var questionId in questionIds)
        {
            var assignment = new QuestionAssignment
            {
                CampaignAssignmentId = campaignAssignmentId,
                QuestionId = questionId,
                AssignmentType = QuestionAssignmentType.Question,
                AssignedUserId = assignedUserId,
                CreatedById = createdById,
                Instructions = instructions,
                CreatedAt = DateTime.UtcNow
            };
            assignments.Add(assignment);
        }

        _context.QuestionAssignments.AddRange(assignments);
        await _context.SaveChangesAsync();

        // Track each assignment creation
        foreach (var assignment in assignments)
        {
            await _trackingService.TrackAssignmentCreatedAsync(assignment, createdById, "Bulk question assignment");
        }

        _logger.LogInformation("Bulk question assignments created: {Count} questions assigned to user {UserId} for campaign assignment {CampaignAssignmentId}", 
            questionIds.Count, assignedUserId, campaignAssignmentId);

        return assignments;
    }

    #endregion

    #region Assignment Management

    public async Task<QuestionAssignmentManagementViewModel> GetAssignmentManagementViewModelAsync(int campaignAssignmentId)
    {
        var campaignAssignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(q => q.QuestionTypeMaster)
            .Include(ca => ca.LeadResponder)
            .Include(ca => ca.QuestionAssignments)
                .ThenInclude(qa => qa.AssignedUser)
            .Include(ca => ca.QuestionAssignments)
                .ThenInclude(qa => qa.Question)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        if (campaignAssignment == null)
            throw new ArgumentException($"Campaign assignment with ID {campaignAssignmentId} not found");

        var questions = campaignAssignment.QuestionnaireVersion.Questionnaire.Questions
            .OrderBy(q => q.DisplayOrder)
            .ToList();

        var assignments = campaignAssignment.QuestionAssignments.ToList();

        // Group questions by section
        var sections = questions
            .GroupBy(q => q.Section ?? "Other")
            .Select(g => new AssignmentSectionViewModel
            {
                SectionName = g.Key,
                DisplayName = g.Key == "Other" ? "Other Questions" : g.Key,
                Questions = g.Select(q => new QuestionAssignmentItemViewModel
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionTypeMaster?.Name ?? "Unknown",
                    IsRequired = q.IsRequired,
                    IsAssigned = assignments.Any(a => a.QuestionId == q.Id),
                    AssignedToUserId = assignments.FirstOrDefault(a => a.QuestionId == q.Id)?.AssignedUserId,
                    AssignedToUserName = assignments.FirstOrDefault(a => a.QuestionId == q.Id)?.AssignedUser?.FullName,
                    AssignmentId = assignments.FirstOrDefault(a => a.QuestionId == q.Id)?.Id,
                    Instructions = assignments.FirstOrDefault(a => a.QuestionId == q.Id)?.Instructions
                }).ToList()
            })
            .ToList();

        // Calculate section-level assignments
        foreach (var section in sections)
        {
            section.TotalQuestions = section.Questions.Count;
            section.AssignedQuestions = section.Questions.Count(q => q.IsAssigned);
            
            // Check if entire section is assigned to one user
            var sectionAssignment = assignments.FirstOrDefault(a => a.SectionName == section.SectionName);
            if (sectionAssignment != null)
            {
                section.AssignedToUserId = sectionAssignment.AssignedUserId;
                section.AssignedToUserName = sectionAssignment.AssignedUser?.FullName;
                section.SectionAssignmentId = sectionAssignment.Id;
                section.IsFullyAssigned = true;
            }
            else
            {
                // Check if all questions in section are assigned to the same user
                var assignedUsers = section.Questions.Where(q => q.IsAssigned).Select(q => q.AssignedToUserId).Distinct().ToList();
                if (assignedUsers.Count == 1 && section.AssignedQuestions == section.TotalQuestions)
                {
                    section.AssignedToUserId = assignedUsers.First();
                    section.AssignedToUserName = section.Questions.First(q => q.AssignedToUserId == assignedUsers.First())?.AssignedToUserName;
                    section.IsFullyAssigned = true;
                }
            }
        }

        // Get available users from the target organization
        var availableUsers = await _context.Users
            .Where(u => u.OrganizationId == campaignAssignment.TargetOrganizationId && u.IsActive)
            .Select(u => new AssignmentUserViewModel
            {
                UserId = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Role = "User", // Role information would need to be retrieved separately through UserManager
                IsLeadResponder = u.Id == campaignAssignment.LeadResponderId
            })
            .ToListAsync();

        // Calculate assignment statistics
        var assignmentsByUser = assignments
            .GroupBy(a => a.AssignedUserId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Update user assignment counts
        foreach (var user in availableUsers)
        {
            user.AssignedQuestions = assignments.Count(a => a.AssignedUserId == user.UserId && a.QuestionId.HasValue);
            user.AssignedSections = assignments.Count(a => a.AssignedUserId == user.UserId && !string.IsNullOrEmpty(a.SectionName));
        }

        return new QuestionAssignmentManagementViewModel
        {
            CampaignAssignmentId = campaignAssignmentId,
            CampaignName = campaignAssignment.Campaign.Name,
            TargetOrganizationName = campaignAssignment.TargetOrganization.Name,
            QuestionnaireTitle = campaignAssignment.QuestionnaireVersion.Questionnaire.Title,
            LeadResponderName = campaignAssignment.LeadResponder?.FullName,
            Sections = sections,
            CurrentAssignments = assignments.Select(a => new QuestionAssignmentViewModel
            {
                Id = a.Id,
                CampaignAssignmentId = a.CampaignAssignmentId,
                AssignmentType = a.AssignmentType,
                QuestionId = a.QuestionId,
                QuestionText = a.Question?.QuestionText,
                SectionName = a.SectionName,
                AssignedUserId = a.AssignedUserId,
                AssignedUserName = a.AssignedUser?.FullName ?? "",
                AssignedUserEmail = a.AssignedUser?.Email ?? "",
                Instructions = a.Instructions,
                CreatedAt = a.CreatedAt
            }).ToList(),
            AvailableUsers = availableUsers,
            TotalQuestions = questions.Count,
            AssignedQuestions = assignments.Count(a => a.QuestionId.HasValue),
            UnassignedQuestions = questions.Count - assignments.Count(a => a.QuestionId.HasValue),
            AssignmentsByUser = assignmentsByUser
        };
    }

    public async Task<bool> DeleteAssignmentAsync(int assignmentId, string? removedById = null)
    {
        var assignment = await _context.QuestionAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return false;

        // Track the assignment removal before deleting
        if (!string.IsNullOrEmpty(removedById))
        {
            await _trackingService.TrackAssignmentRemovedAsync(assignment, removedById, "Assignment removed");
        }

        _context.QuestionAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Question assignment deleted: ID {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<bool> UpdateAssignmentAsync(int assignmentId, string? instructions, string? updatedById = null)
    {
        var assignment = await _context.QuestionAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return false;

        // Create a copy of the original assignment for tracking
        var originalAssignment = new QuestionAssignment
        {
            Id = assignment.Id,
            CampaignAssignmentId = assignment.CampaignAssignmentId,
            QuestionId = assignment.QuestionId,
            SectionName = assignment.SectionName,
            AssignedUserId = assignment.AssignedUserId,
            AssignmentType = assignment.AssignmentType,
            Instructions = assignment.Instructions,
            CreatedAt = assignment.CreatedAt,
            CreatedById = assignment.CreatedById
        };

        assignment.Instructions = instructions;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Track the assignment modification
        if (!string.IsNullOrEmpty(updatedById))
        {
            await _trackingService.TrackAssignmentModifiedAsync(originalAssignment, assignment, updatedById, "Assignment instructions updated");
        }

        _logger.LogInformation("Question assignment updated: ID {AssignmentId}", assignmentId);
        return true;
    }

    #endregion

    #region Assignment Queries

    public async Task<List<QuestionAssignment>> GetAssignmentsForCampaignAsync(int campaignAssignmentId)
    {
        return await _context.QuestionAssignments
            .Include(qa => qa.Question)
            .Include(qa => qa.AssignedUser)
            .Where(qa => qa.CampaignAssignmentId == campaignAssignmentId)
            .ToListAsync();
    }

    public async Task<List<QuestionAssignment>> GetAssignmentsForUserAsync(string userId, int campaignAssignmentId)
    {
        return await _context.QuestionAssignments
            .Include(qa => qa.Question)
            .Where(qa => qa.AssignedUserId == userId && qa.CampaignAssignmentId == campaignAssignmentId)
            .ToListAsync();
    }

    public async Task<bool> IsQuestionAssignedAsync(int questionId, int campaignAssignmentId)
    {
        return await _context.QuestionAssignments
            .AnyAsync(qa => qa.QuestionId == questionId && qa.CampaignAssignmentId == campaignAssignmentId);
    }

    public async Task<bool> IsSectionAssignedAsync(string sectionName, int campaignAssignmentId)
    {
        return await _context.QuestionAssignments
            .AnyAsync(qa => qa.SectionName == sectionName && qa.CampaignAssignmentId == campaignAssignmentId);
    }

    public async Task<string?> GetQuestionAssigneeAsync(int questionId, int campaignAssignmentId)
    {
        var assignment = await _context.QuestionAssignments
            .FirstOrDefaultAsync(qa => qa.QuestionId == questionId && qa.CampaignAssignmentId == campaignAssignmentId);
        
        if (assignment != null)
            return assignment.AssignedUserId;

        // Check if question is in an assigned section
        var question = await _context.Questions.FindAsync(questionId);
        if (question?.Section != null)
        {
            var sectionAssignment = await _context.QuestionAssignments
                .FirstOrDefaultAsync(qa => qa.SectionName == question.Section && qa.CampaignAssignmentId == campaignAssignmentId);
            return sectionAssignment?.AssignedUserId;
        }

        return null;
    }

    public async Task<List<int>> GetAssignedQuestionIdsForUserAsync(string userId, int campaignAssignmentId)
    {
        var questionIds = new List<int>();

        // Get directly assigned questions
        var directlyAssignedQuestions = await _context.QuestionAssignments
            .Where(qa => qa.AssignedUserId == userId && qa.CampaignAssignmentId == campaignAssignmentId && qa.QuestionId.HasValue)
            .Select(qa => qa.QuestionId.Value)
            .ToListAsync();

        questionIds.AddRange(directlyAssignedQuestions);

        // Get questions from assigned sections
        var assignedSections = await _context.QuestionAssignments
            .Where(qa => qa.AssignedUserId == userId && qa.CampaignAssignmentId == campaignAssignmentId && qa.SectionName != null)
            .Select(qa => qa.SectionName)
            .ToListAsync();

        if (assignedSections.Any())
        {
            var campaignAssignment = await _context.CampaignAssignments
                .Include(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
                        .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

            if (campaignAssignment != null)
            {
                var sectionQuestionIds = campaignAssignment.QuestionnaireVersion.Questionnaire.Questions
                    .Where(q => assignedSections.Contains(q.Section))
                    .Select(q => q.Id)
                    .ToList();

                questionIds.AddRange(sectionQuestionIds);
            }
        }

        return questionIds.Distinct().ToList();
    }

    #endregion

    #region Progress Tracking

    public async Task<AssignmentProgressViewModel> GetAssignmentProgressAsync(int campaignAssignmentId)
    {
        var campaignAssignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.TargetOrganization)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .Include(ca => ca.QuestionAssignments)
                .ThenInclude(qa => qa.AssignedUser)
            .Include(ca => ca.Responses)
            .FirstOrDefaultAsync(ca => ca.Id == campaignAssignmentId);

        if (campaignAssignment == null)
            throw new ArgumentException($"Campaign assignment with ID {campaignAssignmentId} not found");

        var questions = campaignAssignment.QuestionnaireVersion.Questionnaire.Questions.ToList();
        var assignments = campaignAssignment.QuestionAssignments.ToList();
        var responses = campaignAssignment.Responses.ToList();

        // Calculate overall progress
        var totalQuestions = questions.Count;
        var assignedQuestions = assignments.Count(a => a.QuestionId.HasValue);
        var completedQuestions = responses.Count;

        var assignmentProgress = totalQuestions > 0 ? (decimal)assignedQuestions / totalQuestions * 100 : 0;
        var completionProgress = assignedQuestions > 0 ? (decimal)completedQuestions / assignedQuestions * 100 : 0;

        // Calculate user progress
        var userProgress = assignments
            .GroupBy(a => a.AssignedUserId)
            .Select(g => new UserProgressViewModel
            {
                UserId = g.Key,
                Name = g.First().AssignedUser?.FullName ?? "Unknown",
                Email = g.First().AssignedUser?.Email ?? "",
                AssignedQuestions = g.Count(a => a.QuestionId.HasValue),
                CompletedQuestions = responses.Count(r => r.ResponderId == g.Key),
                LastActivity = responses.Where(r => r.ResponderId == g.Key).Max(r => (DateTime?)r.UpdatedAt) ?? 
                               responses.Where(r => r.ResponderId == g.Key).Max(r => (DateTime?)r.CreatedAt)
            })
            .ToList();

        foreach (var user in userProgress)
        {
            user.CompletionPercentage = user.AssignedQuestions > 0 ? 
                (decimal)user.CompletedQuestions / user.AssignedQuestions * 100 : 0;
        }

        // Calculate section progress
        var sectionProgress = questions
            .Where(q => !string.IsNullOrEmpty(q.Section))
            .GroupBy(q => q.Section!)
            .Select(g => new SectionProgressViewModel
            {
                SectionName = g.Key,
                DisplayName = g.Key,
                TotalQuestions = g.Count(),
                AssignedQuestions = assignments.Count(a => a.SectionName == g.Key || 
                                                          (a.QuestionId.HasValue && g.Any(q => q.Id == a.QuestionId.Value))),
                CompletedQuestions = responses.Count(r => g.Any(q => q.Id == r.QuestionId)),
                AssignedUsers = assignments
                    .Where(a => a.SectionName == g.Key || (a.QuestionId.HasValue && g.Any(q => q.Id == a.QuestionId.Value)))
                    .Select(a => a.AssignedUser?.FullName ?? "Unknown")
                    .Distinct()
                    .ToList()
            })
            .ToList();

        foreach (var section in sectionProgress)
        {
            section.AssignmentPercentage = section.TotalQuestions > 0 ? 
                (decimal)section.AssignedQuestions / section.TotalQuestions * 100 : 0;
            section.CompletionPercentage = section.AssignedQuestions > 0 ? 
                (decimal)section.CompletedQuestions / section.AssignedQuestions * 100 : 0;
        }

        return new AssignmentProgressViewModel
        {
            CampaignAssignmentId = campaignAssignmentId,
            CampaignName = campaignAssignment.Campaign.Name,
            TargetOrganizationName = campaignAssignment.TargetOrganization.Name,
            TotalQuestions = totalQuestions,
            AssignedQuestions = assignedQuestions,
            CompletedQuestions = completedQuestions,
            AssignmentProgress = assignmentProgress,
            CompletionProgress = completionProgress,
            UserProgress = userProgress,
            SectionProgress = sectionProgress
        };
    }

    #endregion
} 