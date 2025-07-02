using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Services;
using System.Text.Json;

namespace ESGPlatform.Controllers;

[Authorize]
public class ResponseController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IResponseChangeTrackingService _changeTrackingService;
    private readonly ILogger<ResponseController> _logger;
    private readonly IConditionalQuestionService _conditionalService;
    private readonly IResponseWorkflowService _responseWorkflowService;

    public ResponseController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IResponseChangeTrackingService changeTrackingService, ILogger<ResponseController> logger, IConditionalQuestionService conditionalService, IResponseWorkflowService responseWorkflowService)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _changeTrackingService = changeTrackingService;
        _logger = logger;
        _conditionalService = conditionalService;
        _responseWorkflowService = responseWorkflowService;
    }

    // GET: Response
    public async Task<IActionResult> Index()
    {
        var model = new AssignmentDashboardViewModel();

        var currentUserId = CurrentUserId;

        // Get assignments with user rights filtering
        IQueryable<CampaignAssignment> assignmentQuery = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions)
            .Include(ca => ca.TargetOrganization);

        // Apply access control filtering
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see assignments targeted to them
                assignmentQuery = assignmentQuery.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see assignments for campaigns they created
                assignmentQuery = assignmentQuery.Where(ca => ca.Campaign.OrganizationId == CurrentOrganizationId);
            }
        }

        // Get assignments where user is the lead responder
        var myAssignments = await assignmentQuery
            .Where(ca => ca.LeadResponderId == currentUserId)
            .ToListAsync();

        // Load responses separately to bypass organization filters
        foreach (var assignment in myAssignments)
        {
            assignment.Responses = await _context.Responses
                .IgnoreQueryFilters()
                .Where(r => r.CampaignAssignmentId == assignment.Id)
                .ToListAsync();
        }

        // Get delegations to current user with same access control
        var delegationsQuery = _context.Delegations
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(d => d.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
                        .ThenInclude(q => q.Questions)
            .Include(d => d.Question)
            .Include(d => d.FromUser)
            .Where(d => d.ToUserId == currentUserId && d.IsActive);

        // Apply access control to delegations
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                delegationsQuery = delegationsQuery.Where(d => d.CampaignAssignment.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                delegationsQuery = delegationsQuery.Where(d => d.CampaignAssignment.Campaign.OrganizationId == CurrentOrganizationId);
            }
        }

        var delegationsToMe = await delegationsQuery.ToListAsync();

        // Get question assignments for current user
        var questionAssignmentsQuery = _context.QuestionAssignments
            .IgnoreQueryFilters() // Bypass organization filters for question assignments
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
                        .ThenInclude(q => q.Questions)
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(qa => qa.Question)
            .Where(qa => qa.AssignedUserId == currentUserId);

        // Apply access control to question assignments (manual filtering since we bypassed query filters)
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier orgs see assignments where they are the target
                questionAssignmentsQuery = questionAssignmentsQuery.Where(qa => qa.CampaignAssignment.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform orgs see assignments where they created the campaign OR are the target
                questionAssignmentsQuery = questionAssignmentsQuery.Where(qa => 
                    qa.CampaignAssignment.Campaign.OrganizationId == CurrentOrganizationId ||
                    qa.CampaignAssignment.TargetOrganizationId == CurrentOrganizationId);
            }
        }

        var questionAssignmentsToMe = await questionAssignmentsQuery.ToListAsync();

        // Load responses for delegation assignments
        var delegationAssignmentIds = delegationsToMe.Select(d => d.CampaignAssignmentId).Distinct().ToList();
        foreach (var assignmentId in delegationAssignmentIds)
        {
            var assignment = delegationsToMe.First(d => d.CampaignAssignmentId == assignmentId).CampaignAssignment;
            assignment.Responses = await _context.Responses
                .IgnoreQueryFilters()
                .Where(r => r.CampaignAssignmentId == assignmentId)
                .ToListAsync();
        }

        // Load responses for question assignments
        var questionAssignmentIds = questionAssignmentsToMe.Select(qa => qa.CampaignAssignmentId).Distinct().ToList();
        foreach (var assignmentId in questionAssignmentIds)
        {
            var assignment = questionAssignmentsToMe.First(qa => qa.CampaignAssignmentId == assignmentId).CampaignAssignment;
            assignment.Responses = await _context.Responses
                .IgnoreQueryFilters()
                .Where(r => r.CampaignAssignmentId == assignmentId)
                .ToListAsync();
        }

        // Convert to view models
        model.MyAssignments = await ConvertToAssignmentSummaries(myAssignments);
        model.DelegatedToMe = ConvertDelegationsToAssignmentSummaries(delegationsToMe, isDelegatedToMe: true);
        model.QuestionAssignmentsToMe = ConvertQuestionAssignmentsToAssignmentSummaries(questionAssignmentsToMe);

        return View(model);
    }

    // GET: Response/AnswerQuestionnaire/5
    public async Task<IActionResult> AnswerQuestionnaire(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await GetAssignmentWithAccessCheckAsync(id.Value);
        if (assignment == null) return NotFound();

        // Check permissions and determine access type
        bool isLeadResponder = assignment.LeadResponderId == CurrentUserId;
        bool isDelegatedUser = !isLeadResponder && await HasDelegationForAssignment(id.Value, CurrentUserId);
        bool hasQuestionAssignments = !isLeadResponder && !isDelegatedUser && await HasQuestionAssignmentForAssignment(id.Value, CurrentUserId);
        
        if (!isLeadResponder && !isDelegatedUser && !hasQuestionAssignments)
        {
            return Forbid();
        }

        // Update assignment status if not started (only for lead responder)
        if (isLeadResponder && assignment.Status == AssignmentStatus.NotStarted)
        {
            assignment.Status = AssignmentStatus.InProgress;
            assignment.StartedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Determine user context for filtering questions
        string? filterUserId = null;
        if (isDelegatedUser)
        {
            filterUserId = CurrentUserId;
        }
        else if (hasQuestionAssignments)
        {
            filterUserId = CurrentUserId;
        }
        
        var model = await BuildQuestionnaireResponseViewModel(assignment, filterUserId, hasQuestionAssignments);
        
        // Set branding context
        await SetBrandingContextAsync(campaignId: assignment.CampaignId);

        return View(model);
    }

    // POST: Response/SaveResponse
    [HttpPost]
    public async Task<IActionResult> SaveResponse(SaveResponseRequest request)
    {
        try
        {
            var assignment = await GetAssignmentWithAccessCheckAsync(request.AssignmentId);
            if (assignment == null) return NotFound();

                    // Check permissions
        bool isLeadResponder = assignment.LeadResponderId == CurrentUserId;
        bool isDelegatedUser = !isLeadResponder && await HasDelegationForAssignment(request.AssignmentId, CurrentUserId);
        bool hasQuestionAssignment = !isLeadResponder && !isDelegatedUser && await HasQuestionAssignmentForAssignment(request.AssignmentId, CurrentUserId);
        
        if (!isLeadResponder && !isDelegatedUser && !hasQuestionAssignment)
        {
            return Forbid();
        }
        
        // Additional checks for restricted users
        if (isDelegatedUser && !await HasDelegationForQuestion(request.AssignmentId, request.QuestionId, CurrentUserId))
        {
            return Forbid();
        }
        
        if (hasQuestionAssignment && !await HasQuestionAssignmentForQuestion(request.AssignmentId, request.QuestionId, CurrentUserId))
        {
            return Forbid();
        }

            // Get or create response
            var response = assignment.Responses
                .FirstOrDefault(r => r.QuestionId == request.QuestionId);

            Response? originalResponse = null;
            bool isNewResponse = response == null;

            if (response == null)
            {
                response = new Response
                {
                    QuestionId = request.QuestionId,
                    CampaignAssignmentId = request.AssignmentId,
                    ResponderId = CurrentUserId!,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Responses.Add(response);
            }
            else
            {
                // Create a copy of the original response for change tracking IMMEDIATELY
                originalResponse = new Response
                {
                    Id = response.Id,
                    QuestionId = response.QuestionId,
                    CampaignAssignmentId = response.CampaignAssignmentId,
                    ResponderId = response.ResponderId,
                    TextValue = response.TextValue,
                    NumericValue = response.NumericValue,
                    DateValue = response.DateValue,
                    BooleanValue = response.BooleanValue,
                    SelectedValues = response.SelectedValues,
                    CreatedAt = response.CreatedAt,
                    UpdatedAt = response.UpdatedAt
                };
                
                try
                {
                    _logger.LogInformation("Captured original values: Text='{OriginalText}', Numeric={OriginalNumeric}, Date={OriginalDate}, Boolean={OriginalBoolean}, Selected='{OriginalSelected}'", 
                        originalResponse.TextValue, originalResponse.NumericValue, originalResponse.DateValue, originalResponse.BooleanValue, originalResponse.SelectedValues);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log original response values for Response ID {ResponseId}", response.Id);
                }
                
                response.UpdatedAt = DateTime.UtcNow;
            }

            // Update response values
            response.TextValue = request.TextValue;
            response.NumericValue = request.NumericValue;
            response.DateValue = request.DateValue;
            response.BooleanValue = request.BooleanValue;
            response.SelectedValues = request.SelectedValues != null ? 
                JsonSerializer.Serialize(request.SelectedValues) : null;

            await _context.SaveChangesAsync();

            // Update response status based on content
            await _responseWorkflowService.UpdateStatusForAnswerAsync(response.Id, CurrentUserId!);

            // Track changes for existing responses
            if (!isNewResponse && originalResponse != null)
            {
                _logger.LogInformation("Tracking response change for Response ID {ResponseId}, Question ID {QuestionId}", response.Id, response.QuestionId);
                await _changeTrackingService.TrackResponseChangeAsync(
                    originalResponse, 
                    response, 
                    CurrentUserId!,
                    "Response updated by user"
                );
            }
            else
            {
                _logger.LogInformation("Not tracking changes - IsNewResponse: {IsNewResponse}, OriginalResponse is null: {OriginalIsNull}", isNewResponse, originalResponse == null);
            }

            // Mark delegation as completed if this is a delegated response
            if (isDelegatedUser)
            {
                var delegation = await _context.Delegations
                    .FirstOrDefaultAsync(d => d.CampaignAssignmentId == request.AssignmentId && 
                                            d.QuestionId == request.QuestionId &&
                                            d.ToUserId == CurrentUserId && 
                                            d.IsActive && 
                                            !d.CompletedAt.HasValue);
                
                if (delegation != null)
                {
                    delegation.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "Response saved successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error saving response: " + ex.Message });
        }
    }

    // POST: Response/ClearResponse
    [HttpPost]
    public async Task<IActionResult> ClearResponse(int assignmentId, int questionId)
    {
        try
        {
            var assignment = await GetAssignmentWithAccessCheckAsync(assignmentId);
            if (assignment == null) return NotFound();

            // Check permissions
            bool isLeadResponder = assignment.LeadResponderId == CurrentUserId;
            bool isDelegatedUser = !isLeadResponder && await HasDelegationForAssignment(assignmentId, CurrentUserId);
            bool hasQuestionAssignment = !isLeadResponder && !isDelegatedUser && await HasQuestionAssignmentForAssignment(assignmentId, CurrentUserId);
            
            if (!isLeadResponder && !isDelegatedUser && !hasQuestionAssignment)
            {
                return Forbid();
            }
            
            // Additional checks for restricted users
            if (isDelegatedUser && !await HasDelegationForQuestion(assignmentId, questionId, CurrentUserId))
            {
                return Forbid();
            }
            
            if (hasQuestionAssignment && !await HasQuestionAssignmentForQuestion(assignmentId, questionId, CurrentUserId))
            {
                return Forbid();
            }

            // Find the existing response
            var response = assignment.Responses
                .FirstOrDefault(r => r.QuestionId == questionId);

            if (response != null)
            {
                _logger.LogInformation("Clearing response for Assignment ID {AssignmentId}, Question ID {QuestionId}, Response ID {ResponseId} by User {UserId}", 
                    assignmentId, questionId, response.Id, CurrentUserId);

                // Track the clearing as a response change (preserve history)
                await _changeTrackingService.TrackResponseChangeAsync(
                    originalResponse: response, // Current values before clearing
                    newResponse: new Response // Empty response representing the cleared state
                    {
                        Id = response.Id,
                        QuestionId = response.QuestionId,
                        CampaignAssignmentId = response.CampaignAssignmentId,
                        ResponderId = response.ResponderId,
                        TextValue = null,
                        NumericValue = null,
                        DateValue = null,
                        BooleanValue = null,
                        SelectedValues = null,
                        CreatedAt = response.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    },
                    userId: CurrentUserId!,
                    reason: "Response cleared"
                );

                // Delete associated file uploads
                var fileUploads = await _context.FileUploads
                    .Where(f => f.ResponseId == response.Id)
                    .ToListAsync();

                foreach (var fileUpload in fileUploads)
                {
                    // Delete physical file
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileUpload.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    
                    _context.FileUploads.Remove(fileUpload);
                }

                // Clear the response values instead of deleting the record (preserves history)
                response.TextValue = null;
                response.NumericValue = null;
                response.DateValue = null;
                response.BooleanValue = null;
                response.SelectedValues = null;
                response.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                // Update response status after clearing
                await _responseWorkflowService.UpdateStatusForAnswerAsync(response.Id, CurrentUserId!);

                // Mark delegation as incomplete if this was a delegated response
                if (isDelegatedUser)
                {
                    var delegation = await _context.Delegations
                        .FirstOrDefaultAsync(d => d.CampaignAssignmentId == assignmentId && 
                                                d.QuestionId == questionId &&
                                                d.ToUserId == CurrentUserId && 
                                                d.IsActive && 
                                                d.CompletedAt.HasValue);
                    
                    if (delegation != null)
                    {
                        delegation.CompletedAt = null;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Marked delegation as incomplete for Delegation ID {DelegationId}", delegation.Id);
                    }
                }

                _logger.LogInformation("Successfully cleared response for Assignment ID {AssignmentId}, Question ID {QuestionId}", assignmentId, questionId);
                return Json(new { success = true, message = "Response cleared successfully" });
            }
            else
            {
                return Json(new { success = false, message = "No response found to clear" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing response for Assignment ID {AssignmentId}, Question ID {QuestionId} by User {UserId}", 
                assignmentId, questionId, CurrentUserId);
            return Json(new { success = false, message = "Error clearing response: " + ex.Message });
        }
    }

    // GET: Response/DelegateQuestion/5
    public async Task<IActionResult> DelegateQuestion(int? assignmentId, int? questionId)
    {
        if (assignmentId == null || questionId == null) return NotFound();

        var assignment = await GetAssignmentWithAccessCheckAsync(assignmentId.Value);
        if (assignment == null) return NotFound();

        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return NotFound();

        // Check permissions - only lead responder can delegate
        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        var model = new DelegateQuestionViewModel
        {
            AssignmentId = assignmentId.Value,
            QuestionId = questionId.Value,
            CampaignName = assignment.Campaign.Name,
            QuestionnaireTitle = assignment.QuestionnaireVersion.Questionnaire.Title,
            QuestionText = question.QuestionText,
            HelpText = question.HelpText
        };

        // Load team members from the same organization
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive && u.Id != CurrentUserId)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return View(model);
    }

    // POST: Response/DelegateQuestion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DelegateQuestion(DelegateQuestionViewModel model)
    {
        if (ModelState.IsValid)
        {
            var assignment = await GetAssignmentWithAccessCheckAsync(model.AssignmentId);
            if (assignment == null) return NotFound();

            // Check permissions - only lead responder can delegate
            if (assignment.LeadResponderId != CurrentUserId)
            {
                return Forbid();
            }

            // Create delegation
            var delegation = new Delegation
            {
                CampaignAssignmentId = model.AssignmentId,
                QuestionId = model.QuestionId,
                FromUserId = CurrentUserId!,
                ToUserId = model.ToUserId,
                Instructions = model.Instructions,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Delegations.Add(delegation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Question delegated: Assignment ID {AssignmentId}, Question ID {QuestionId} delegated from {FromUserId} to {ToUserId}", 
                model.AssignmentId, model.QuestionId, CurrentUserId, model.ToUserId);

            TempData["Success"] = "Question delegated successfully!";
            return RedirectToAction(nameof(AnswerQuestionnaire), new { id = model.AssignmentId });
        }

        await LoadDelegateQuestionData(model);
        return View(model);
    }

    // POST: Response/UploadFile
    [HttpPost]
    public async Task<IActionResult> UploadFile(int assignmentId, int questionId, IFormFile file)
    {
        try
        {
            var assignment = await GetAssignmentWithAccessCheckAsync(assignmentId);
            if (assignment == null) return Json(new { success = false, message = "Assignment not found" });

            // Check permissions
            bool isLeadResponder = assignment.LeadResponderId == CurrentUserId;
            bool isDelegatedUser = !isLeadResponder && await HasDelegationForAssignment(assignmentId, CurrentUserId);
            bool hasQuestionAssignment = !isLeadResponder && !isDelegatedUser && await HasQuestionAssignmentForAssignment(assignmentId, CurrentUserId);
            
            if (!isLeadResponder && !isDelegatedUser && !hasQuestionAssignment)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            
            // Additional checks for restricted users
            if (isDelegatedUser && !await HasDelegationForQuestion(assignmentId, questionId, CurrentUserId))
            {
                return Json(new { success = false, message = "Access denied" });
            }
            
            if (hasQuestionAssignment && !await HasQuestionAssignmentForQuestion(assignmentId, questionId, CurrentUserId))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Get or create response
            var response = await GetOrCreateResponse(assignmentId, questionId);

            // Create file upload record
            var fileUpload = new FileUpload
            {
                ResponseId = response.Id,
                FileName = file.FileName,
                FilePath = $"/uploads/{fileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedById = CurrentUserId!
            };

            _context.FileUploads.Add(fileUpload);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "File uploaded successfully",
                fileId = fileUpload.Id,
                fileName = fileUpload.FileName,
                filePath = fileUpload.FilePath
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error uploading file: " + ex.Message });
        }
    }

    // POST: Response/DeleteFile
    [HttpPost]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        try
        {
            var fileUpload = await _context.FileUploads
                .Include(f => f.Response)
                    .ThenInclude(r => r.CampaignAssignment)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileUpload == null)
                return Json(new { success = false, message = "File not found" });

            // Check access rights
            var assignment = await GetAssignmentWithAccessCheckAsync(fileUpload.Response.CampaignAssignmentId);
            if (assignment == null)
                return Json(new { success = false, message = "Access denied" });

            // Check permissions
            bool isLeadResponder = assignment.LeadResponderId == CurrentUserId;
            bool isDelegatedUser = !isLeadResponder && await HasDelegationForAssignment(assignment.Id, CurrentUserId);
            
            if (!isLeadResponder && !isDelegatedUser)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            
            // Additional check for delegated users - they can only delete files for their delegated questions
            if (isDelegatedUser && !await HasDelegationForQuestion(assignment.Id, fileUpload.Response.QuestionId, CurrentUserId))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Delete physical file
            var fileName = Path.GetFileName(fileUpload.FilePath);
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Delete database record
            _context.FileUploads.Remove(fileUpload);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting file: " + ex.Message });
        }
    }

    // GET: Response/ReviewSubmission/5
    public async Task<IActionResult> ReviewSubmission(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await GetAssignmentWithAccessCheckAsync(id.Value);
        if (assignment == null) return NotFound();

        // Check permissions
        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        var model = await BuildSubmissionReviewViewModel(assignment);
        return View(model);
    }

    // POST: Response/SubmitAssignment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssignment(int id)
    {
        var assignment = await GetAssignmentWithAccessCheckAsync(id);
        if (assignment == null) return NotFound();

        // Check permissions
        if (assignment.LeadResponderId != CurrentUserId)
        {
            return Forbid();
        }

        // Update assignment status
        assignment.Status = AssignmentStatus.Submitted;
        assignment.SubmittedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Assignment submitted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // DEBUG: Temporary action to help diagnose question assignment issues
    // POST: Response/GetQuestionVisibility (AJAX endpoint for conditional questions)
    [HttpPost]
    public async Task<IActionResult> GetQuestionVisibility(int assignmentId)
    {
        try
        {
            var assignment = await GetAssignmentWithAccessCheckAsync(assignmentId);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment {AssignmentId} not found for visibility check", assignmentId);
                return Json(new { success = false, message = "Assignment not found" });
            }

            // Get all questions for this questionnaire
            var questions = await _context.Questions
                .IgnoreQueryFilters()
                .Where(q => q.QuestionnaireId == assignment.QuestionnaireVersion.QuestionnaireId)
                .Select(q => q.Id)
                .ToListAsync();

            _logger.LogInformation("Checking visibility for {QuestionCount} questions in assignment {AssignmentId}", 
                questions.Count, assignmentId);

            // Get visibility for all questions
            var visibility = await _conditionalService.GetQuestionVisibilityAsync(questions, assignmentId);

            _logger.LogInformation("Visibility results: {VisibilityResults}", 
                string.Join(", ", visibility.Select(kvp => $"Q{kvp.Key}={kvp.Value}")));

            return Json(new { success = true, visibility = visibility });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question visibility for assignment {AssignmentId}", assignmentId);
            return Json(new { success = false, message = "Error checking question visibility" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DebugQuestionAssignments(string? userId = null)
    {
        userId = userId ?? CurrentUserId;
        
        var debug = new
        {
            UserId = userId,
            CurrentOrgId = CurrentOrganizationId,
            IsPlatformAdmin = IsPlatformAdmin,
            IsCurrentOrgSupplierType = IsCurrentOrgSupplierType,
            IsCurrentOrgPlatformType = IsCurrentOrgPlatformType,
        };

        // Get ALL question assignments for this user (ignoring filters)
        var allQuestionAssignments = await _context.QuestionAssignments
            .IgnoreQueryFilters()
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.Campaign)
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.QuestionnaireVersion)
                    .ThenInclude(qv => qv.Questionnaire)
            .Include(qa => qa.CampaignAssignment)
                .ThenInclude(ca => ca.TargetOrganization)
            .Include(qa => qa.Question)
            .Where(qa => qa.AssignedUserId == userId)
            .ToListAsync();

        var debugAssignments = allQuestionAssignments.Select(qa => new
        {
            qa.Id,
            qa.AssignmentType,
            qa.QuestionId,
            qa.SectionName,
            CampaignId = qa.CampaignAssignment.Campaign.Id,
            CampaignName = qa.CampaignAssignment.Campaign.Name,
            QuestionnaireId = qa.CampaignAssignment.QuestionnaireVersion?.QuestionnaireId,
            QuestionnaireName = qa.CampaignAssignment.QuestionnaireVersion?.Questionnaire?.Title,
            TargetOrgId = qa.CampaignAssignment.TargetOrganizationId,
            TargetOrgName = qa.CampaignAssignment.TargetOrganization?.Name,
            CampaignOrgId = qa.CampaignAssignment.Campaign.OrganizationId,
            Instructions = qa.Instructions
        }).ToList();

        // For section assignments, check how many questions match
        var sectionDebug = allQuestionAssignments
            .Where(qa => !string.IsNullOrEmpty(qa.SectionName))
            .Select(qa => new
            {
                qa.Id,
                qa.SectionName,
                QuestionnaireId = qa.CampaignAssignment.QuestionnaireVersion?.QuestionnaireId,
                MatchingQuestions = qa.CampaignAssignment.QuestionnaireVersion?.QuestionnaireId != null ?
                    _context.Questions
                        .IgnoreQueryFilters()
                        .Where(q => q.QuestionnaireId == qa.CampaignAssignment.QuestionnaireVersion.QuestionnaireId)
                        .Select(q => new { q.Id, Section = q.Section ?? "Other", q.QuestionText })
                        .ToList() : null,
                SectionMatches = qa.CampaignAssignment.QuestionnaireVersion?.QuestionnaireId != null ?
                    _context.Questions
                        .IgnoreQueryFilters()
                        .Where(q => q.QuestionnaireId == qa.CampaignAssignment.QuestionnaireVersion.QuestionnaireId &&
                                   (q.Section ?? "Other") == qa.SectionName)
                        .Count() : 0
            }).ToList();

        return Json(new
        {
            Debug = debug,
            AllQuestionAssignments = debugAssignments,
            SectionAssignmentDebug = sectionDebug,
            Count = allQuestionAssignments.Count
        });
    }

    // Helper method to get assignment with proper access control
    private async Task<CampaignAssignment?> GetAssignmentWithAccessCheckAsync(int assignmentId)
    {
        var assignmentQuery = _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
                    .ThenInclude(q => q.Questions.OrderBy(qu => qu.DisplayOrder))
            .Include(ca => ca.TargetOrganization)
            .Where(ca => ca.Id == assignmentId);

        // Apply access control filtering
        if (!IsPlatformAdmin)
        {
            if (IsCurrentOrgSupplierType)
            {
                // Supplier organizations can only see assignments targeted to them
                assignmentQuery = assignmentQuery.Where(ca => ca.TargetOrganizationId == CurrentOrganizationId);
            }
            else if (IsCurrentOrgPlatformType)
            {
                // Platform organizations can see assignments for campaigns they created
                assignmentQuery = assignmentQuery.Where(ca => ca.Campaign.OrganizationId == CurrentOrganizationId);
            }
        }

        var assignment = await assignmentQuery.FirstOrDefaultAsync();
        
        if (assignment != null)
        {
            // Load responses separately to bypass organization filters
            assignment.Responses = await _context.Responses
                .IgnoreQueryFilters()
                .Include(r => r.FileUploads)
                .Where(r => r.CampaignAssignmentId == assignment.Id)
                .ToListAsync();
        }
        
        return assignment;
    }

    #region Private Helper Methods

    private async Task<List<AssignmentSummaryViewModel>> ConvertToAssignmentSummaries(
        List<CampaignAssignment> assignments)
    {
        var summaries = new List<AssignmentSummaryViewModel>();

        foreach (var assignment in assignments)
        {
            var totalQuestions = assignment.QuestionnaireVersion?.Questionnaire?.Questions?.Count ?? 0;
            var answeredQuestions = assignment.Responses.Count;

            summaries.Add(new AssignmentSummaryViewModel
            {
                AssignmentId = assignment.Id,
                CampaignName = assignment.Campaign.Name,
                QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown",
                VersionNumber = assignment.QuestionnaireVersion?.VersionNumber ?? "1.0",
                OrganizationName = assignment.TargetOrganization?.Name ?? "Unknown",
                Status = assignment.Status,
                Progress = totalQuestions > 0 ? (int)((double)answeredQuestions / totalQuestions * 100) : 0,
                TotalQuestions = totalQuestions,
                AnsweredQuestions = answeredQuestions,
                Deadline = assignment.Campaign.Deadline,
                StartedAt = assignment.StartedAt,
                SubmittedAt = assignment.SubmittedAt
            });
        }

        return summaries;
    }

    private List<AssignmentSummaryViewModel> ConvertDelegationsToAssignmentSummaries(
        List<Delegation> delegations, bool isDelegatedToMe)
    {
        return delegations.GroupBy(d => d.CampaignAssignmentId)
            .Select(g => g.First())
            .Select(d => {
                var assignment = d.CampaignAssignment;
                
                // Get all delegations for this assignment for the current user
                var userDelegations = delegations.Where(del => del.CampaignAssignmentId == d.CampaignAssignmentId).ToList();
                var delegatedQuestionIds = userDelegations.Select(del => del.QuestionId).ToList();
                
                // Count only delegated questions and their responses
                var totalDelegatedQuestions = userDelegations.Count;
                var answeredDelegatedQuestions = assignment.Responses?.Count(r => delegatedQuestionIds.Contains(r.QuestionId)) ?? 0;
                
                return new AssignmentSummaryViewModel
                {
                    AssignmentId = d.CampaignAssignmentId,
                    CampaignName = assignment.Campaign.Name,
                    QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown",
                    VersionNumber = assignment.QuestionnaireVersion?.VersionNumber ?? "1.0",
                    OrganizationName = assignment.TargetOrganization?.Name ?? "Unknown",
                    Status = assignment.Status,
                    Progress = totalDelegatedQuestions > 0 ? (int)((double)answeredDelegatedQuestions / totalDelegatedQuestions * 100) : 0,
                    TotalQuestions = totalDelegatedQuestions,
                    AnsweredQuestions = answeredDelegatedQuestions,
                    DelegatedQuestions = totalDelegatedQuestions,
                    Deadline = assignment.Campaign.Deadline,
                    StartedAt = assignment.StartedAt,
                    SubmittedAt = assignment.SubmittedAt,
                    DelegatedBy = d.FromUser?.FullName,
                    DelegationInstructions = userDelegations.FirstOrDefault()?.Instructions
                };
            }).ToList();
    }

    private List<AssignmentSummaryViewModel> ConvertQuestionAssignmentsToAssignmentSummaries(
        List<Models.Entities.QuestionAssignment> questionAssignments)
    {
        return questionAssignments.GroupBy(qa => qa.CampaignAssignmentId)
            .Select(g => g.First())
            .Select(qa => {
                var assignment = qa.CampaignAssignment;
                
                // Get all question assignments for this assignment for the current user
                var userQuestionAssignments = questionAssignments.Where(q => q.CampaignAssignmentId == qa.CampaignAssignmentId).ToList();
                
                // Get assigned question IDs from direct question assignments
                var directlyAssignedQuestionIds = userQuestionAssignments.Where(q => q.QuestionId.HasValue).Select(q => q.QuestionId!.Value).ToList();
                
                // Get assigned question IDs from section assignments
                var assignedSections = userQuestionAssignments.Where(q => !string.IsNullOrEmpty(q.SectionName)).Select(q => q.SectionName).Distinct().ToList();
                List<int> sectionAssignedQuestionIds = new List<int>();
                
                if (assignedSections.Any() && assignment.QuestionnaireVersion?.QuestionnaireId != null)
                {
                    // Query questions directly to bypass global filters
                    sectionAssignedQuestionIds = _context.Questions
                        .IgnoreQueryFilters()
                        .Where(q => q.QuestionnaireId == assignment.QuestionnaireVersion.QuestionnaireId &&
                                   assignedSections.Contains(q.Section ?? "Other"))
                        .Select(q => q.Id)
                        .ToList();
                }
                
                // Combine both types of assignments
                var assignedQuestionIds = directlyAssignedQuestionIds.Union(sectionAssignedQuestionIds).ToList();
                
                // Count only assigned questions and their responses
                var totalAssignedQuestions = assignedQuestionIds.Count;
                var answeredAssignedQuestions = assignment.Responses?.Count(r => assignedQuestionIds.Contains(r.QuestionId)) ?? 0;
                
                return new AssignmentSummaryViewModel
                {
                    AssignmentId = qa.CampaignAssignmentId,
                    CampaignName = assignment.Campaign.Name,
                    QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown",
                    VersionNumber = assignment.QuestionnaireVersion?.VersionNumber ?? "1.0",
                    OrganizationName = assignment.TargetOrganization?.Name ?? "Unknown",
                    Status = assignment.Status,
                    Progress = totalAssignedQuestions > 0 ? (int)((double)answeredAssignedQuestions / totalAssignedQuestions * 100) : 0,
                    TotalQuestions = totalAssignedQuestions,
                    AnsweredQuestions = answeredAssignedQuestions,
                    DelegatedQuestions = totalAssignedQuestions,
                    Deadline = assignment.Campaign.Deadline,
                    StartedAt = assignment.StartedAt,
                    SubmittedAt = assignment.SubmittedAt,
                    DelegationInstructions = userQuestionAssignments.FirstOrDefault()?.Instructions
                };
            }).ToList();
    }

    /// <summary>
    /// Builds the questionnaire response view model, with optional filtering for delegated users or question assignments.
    /// If filterUserId is provided, only questions delegated to that user or assigned to them are included.
    /// This ensures users only see questions specifically assigned to them.
    /// </summary>
    /// <param name="assignment">The campaign assignment</param>
    /// <param name="filterUserId">Optional user ID for filtering</param>
    /// <param name="isQuestionAssignment">Whether this is a question assignment (vs delegation)</param>
    /// <returns>Filtered questionnaire response view model</returns>
    private async Task<QuestionnaireResponseViewModel> BuildQuestionnaireResponseViewModel(
        CampaignAssignment assignment, string? filterUserId = null, bool isQuestionAssignment = false)
    {
        // Get all questions without filters to ensure we see all questions for this questionnaire
        List<Question> allQuestions = new List<Question>();
        if (assignment.QuestionnaireVersion?.QuestionnaireId != null)
        {
            allQuestions = await _context.Questions
                .IgnoreQueryFilters()
                .Where(q => q.QuestionnaireId == assignment.QuestionnaireVersion.QuestionnaireId)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync();
        }

        // Load review assignments for this campaign assignment
        var reviewAssignments = await _context.ReviewAssignments
            .Include(ra => ra.Reviewer)
            .Include(ra => ra.Comments)
            .Where(ra => ra.CampaignAssignmentId == assignment.Id)
            .ToListAsync();

        // Check if current user is a reviewer for any part of this assignment
        bool isCurrentUserReviewer = reviewAssignments.Any(ra => ra.ReviewerId == CurrentUserId);
        var currentUserReviewAssignments = reviewAssignments.Where(ra => ra.ReviewerId == CurrentUserId).ToList();

        // Filter questions if user is accessing as delegated user or has question assignments
        List<Question> questions;
        if (!string.IsNullOrEmpty(filterUserId))
        {
            if (isQuestionAssignment)
            {
                // Get questions assigned directly to this user
                var directQuestionIds = await _context.QuestionAssignments
                    .Where(qa => qa.CampaignAssignmentId == assignment.Id && 
                               qa.AssignedUserId == filterUserId &&
                               qa.QuestionId.HasValue)
                    .Select(qa => qa.QuestionId!.Value)
                    .ToListAsync();
                
                // Get questions assigned to this user via section assignments
                var assignedSections = await _context.QuestionAssignments
                    .Where(qa => qa.CampaignAssignmentId == assignment.Id && 
                               qa.AssignedUserId == filterUserId &&
                               !string.IsNullOrEmpty(qa.SectionName))
                    .Select(qa => qa.SectionName)
                    .Distinct()
                    .ToListAsync();
                
                var sectionQuestionIds = allQuestions
                    .Where(q => assignedSections.Contains(q.Section ?? "Other"))
                    .Select(q => q.Id)
                    .ToList();
                
                // Combine both types of assignments
                var assignedQuestionIds = directQuestionIds.Union(sectionQuestionIds).ToList();
                
                questions = allQuestions.Where(q => assignedQuestionIds.Contains(q.Id)).ToList();
            }
            else
            {
                // Get only questions delegated to this user
                var delegatedQuestionIds = await _context.Delegations
                    .Where(d => d.CampaignAssignmentId == assignment.Id && 
                               d.ToUserId == filterUserId && 
                               d.IsActive)
                    .Select(d => d.QuestionId)
                    .ToListAsync();
                
                questions = allQuestions.Where(q => delegatedQuestionIds.Contains(q.Id)).ToList();
            }
        }
        else
        {
            questions = allQuestions;
        }

        // Get question visibility for conditional logic
        var questionIds = questions.Select(q => q.Id).ToList();
        var questionVisibility = await _conditionalService.GetQuestionVisibilityAsync(questionIds, assignment.Id);
        
        var questionResponses = new List<QuestionResponseViewModel>();

        foreach (var question in questions)
        {
            var response = assignment.Responses.FirstOrDefault(r => r.QuestionId == question.Id);
            var delegations = await _context.Delegations
                .Include(d => d.ToUser)
                .Where(d => d.CampaignAssignmentId == assignment.Id && 
                           d.QuestionId == question.Id && 
                           d.IsActive)
                .ToListAsync();

            var files = response?.FileUploads?.ToList() ?? new List<FileUpload>();

            // Check if question should be visible based on conditional logic
            var isVisible = questionVisibility.GetValueOrDefault(question.Id, true);

            // Get review assignment data for this question with proper scope matching
            ReviewAssignment? questionReviewAssignment = null;
            
            // First, look for direct question assignment
            questionReviewAssignment = reviewAssignments.FirstOrDefault(ra => 
                ra.Scope == ReviewScope.Question && ra.QuestionId == question.Id);
            
            // If no direct assignment, look for section assignment
            if (questionReviewAssignment == null && !string.IsNullOrEmpty(question.Section))
            {
                questionReviewAssignment = reviewAssignments.FirstOrDefault(ra => 
                    ra.Scope == ReviewScope.Section && ra.SectionName == (question.Section ?? "Other"));
            }
            
            // If no section assignment, look for assignment-level review
            if (questionReviewAssignment == null)
            {
                questionReviewAssignment = reviewAssignments.FirstOrDefault(ra => 
                    ra.Scope == ReviewScope.Assignment);
            }
            
            var reviewComments = questionReviewAssignment?.Comments?.Where(c => c.ResponseId == response?.Id).ToList() ?? new List<ReviewComment>();
            
            // Determine the actual review status for this specific question/response
            ReviewStatus? actualReviewStatus = null;
            if (questionReviewAssignment != null && response != null)
            {
                // For question-specific assignments, use the assignment status
                if (questionReviewAssignment.Scope == ReviewScope.Question)
                {
                    actualReviewStatus = questionReviewAssignment.Status;
                }
                // For section or assignment-level reviews, only show approved if there are comments
                // indicating this specific response was reviewed, or if there's a blanket approval
                else if (questionReviewAssignment.Scope == ReviewScope.Section || 
                         questionReviewAssignment.Scope == ReviewScope.Assignment)
                {
                    // Check if this specific response has review comments indicating approval
                    var responseComments = reviewComments.Where(c => c.ResponseId == response.Id).ToList();
                    if (responseComments.Any())
                    {
                        // If there are comments for this response, use the latest comment's action
                        var latestComment = responseComments.OrderByDescending(c => c.CreatedAt).First();
                        actualReviewStatus = latestComment.ActionTaken switch
                        {
                            ReviewStatus.Approved => ReviewStatus.Approved,
                            ReviewStatus.ChangesRequested => ReviewStatus.ChangesRequested,
                            _ => ReviewStatus.Pending
                        };
                    }
                    else if (questionReviewAssignment.Status == ReviewStatus.Approved)
                    {
                        // Only show as approved for assignment/section level if it's a blanket approval
                        // and the response actually has content (not just answered but empty)
                        var hasSubstantiveResponse = !string.IsNullOrWhiteSpace(response.TextValue) ||
                                                   response.NumericValue.HasValue ||
                                                   response.DateValue.HasValue ||
                                                   response.BooleanValue.HasValue ||
                                                   (response.SelectedValues != null && !string.IsNullOrWhiteSpace(response.SelectedValues));
                        
                        actualReviewStatus = hasSubstantiveResponse ? ReviewStatus.Approved : null;
                    }
                    else
                    {
                        // For assignment/section level reviews without specific comments,
                        // only show "Changes Requested" or other statuses if they apply generally
                        // Otherwise, don't show a review status (null = pending review)
                        actualReviewStatus = questionReviewAssignment.Status == ReviewStatus.ChangesRequested ? 
                            null : // Don't show "Changes Requested" unless there are specific comments
                            (questionReviewAssignment.Status == ReviewStatus.InReview ? ReviewStatus.InReview : null);
                    }
                }
            }

            questionResponses.Add(new QuestionResponseViewModel
            {
                // Question properties
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                HelpText = question.HelpText,
                Section = question.Section, // Add section property
                QuestionType = question.QuestionType,
                IsRequired = question.IsRequired,
                DisplayOrder = question.DisplayOrder,
                Options = GetQuestionOptions(question.Options),
                
                // Response data
                Question = question,
                Response = response,
                ResponseId = response?.Id,
                TextValue = response?.TextValue,
                NumericValue = response?.NumericValue,
                DateValue = response?.DateValue,
                BooleanValue = response?.BooleanValue,
                SelectedValues = response != null ? GetSelectedValues(response.SelectedValues) : null,
                
                // Delegation info - only show delegation info to delegators, not delegatees
                Delegations = delegations,
                IsDelegated = delegations.Any(d => d.IsActive) && string.IsNullOrEmpty(filterUserId),
                DelegatedTo = delegations.FirstOrDefault(d => d.IsActive)?.ToUserId,
                DelegatedToName = delegations.FirstOrDefault(d => d.IsActive)?.ToUser?.FullName,
                DelegationInstructions = delegations.FirstOrDefault(d => d.IsActive)?.Instructions,
                DelegatedAt = delegations.FirstOrDefault(d => d.IsActive)?.CreatedAt,
                IsDelegationCompleted = delegations.Any(d => d.IsActive && d.CompletedAt.HasValue),
                
                // Files
                Files = files,
                FileUploads = files?.Select(f => new FileUploadViewModel
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    ContentType = f.ContentType ?? "application/octet-stream",
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    UploadedBy = f.UploadedBy?.FullName ?? "Unknown",
                    FilePath = f.FilePath
                }).ToList(),
                
                // Options (alternative name for compatibility)
                QuestionOptions = GetQuestionOptions(question.Options),
                
                // Delegation capability - only lead responders can delegate
                CanDelegate = string.IsNullOrEmpty(filterUserId),
                
                // Conditional logic
                IsConditionallyVisible = isVisible,
                
                // Review assignment info - only set if there's actually a review assignment
                IsAssignedForReview = questionReviewAssignment != null,
                ReviewStatus = actualReviewStatus,
                ReviewAssignmentId = questionReviewAssignment?.Id,
                ReviewerName = questionReviewAssignment?.Reviewer?.FullName,
                ReviewInstructions = questionReviewAssignment?.Instructions,
                IsCurrentUserReviewer = questionReviewAssignment?.ReviewerId == CurrentUserId,
                ReviewAssignedAt = questionReviewAssignment?.CreatedAt,
                ReviewCommentsCount = reviewComments.Count,
                HasUnresolvedComments = reviewComments.Any(c => !c.IsResolved)
            });
        }

        // Calculate completion percentage based on filtered questions
        var answeredQuestions = questionResponses.Count(qr => qr.HasResponse);
        
        // Group questions by section
        var sections = questionResponses
            .GroupBy(q => string.IsNullOrEmpty(q.Section) ? "Other" : q.Section)
            .OrderBy(g => g.Key == "Other" ? "zzz" : g.Key) // Put "Other" section last
            .Select(g => new QuestionSectionViewModel
            {
                SectionName = g.Key,
                Questions = g.OrderBy(q => q.DisplayOrder).ToList()
            })
            .ToList();
        
        return new QuestionnaireResponseViewModel
        {
            AssignmentId = assignment.Id,
            CampaignName = assignment.Campaign.Name,
            QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown Questionnaire",
            VersionNumber = assignment.QuestionnaireVersion?.VersionNumber ?? "1",
            Instructions = assignment.Campaign.Instructions,
            Deadline = assignment.Campaign.Deadline,
            Status = assignment.Status,
            Assignment = assignment,
            Questions = questionResponses,
            Sections = sections, // Add the grouped sections
            CompletionPercentage = questions.Count > 0 ? 
                (int)((double)answeredQuestions / questions.Count * 100) : 0,
            IsDelegatedUser = !string.IsNullOrEmpty(filterUserId),
            
            // Review assignment summary info
            IsCurrentUserReviewer = isCurrentUserReviewer,
            HasReviewAssignments = reviewAssignments.Any(),
            ReviewAssignmentsCount = reviewAssignments.Count,
            PendingReviewsCount = currentUserReviewAssignments.Count(ra => ra.Status == ReviewStatus.Pending),
            CompletedReviewsCount = currentUserReviewAssignments.Count(ra => ra.Status == ReviewStatus.Completed || ra.Status == ReviewStatus.Approved)
        };
    }

    private async Task<SubmissionReviewViewModel> BuildSubmissionReviewViewModel(
        CampaignAssignment assignment)
    {
        var questions = assignment.QuestionnaireVersion?.Questionnaire?.Questions?
            .OrderBy(q => q.DisplayOrder)
            .ToList() ?? new List<Question>();

        var questionSummaries = questions.Select(q =>
        {
            var response = assignment.Responses.FirstOrDefault(r => r.QuestionId == q.Id);
            return new QuestionSummaryViewModel
            {
                Question = q,
                Response = response,
                ResponseText = response != null ? GetResponseSummary(response, q.QuestionType) : "Not answered",
                FileCount = response?.FileUploads?.Count ?? 0
            };
        }).ToList();

        return new SubmissionReviewViewModel
        {
            AssignmentId = assignment.Id,
            CampaignName = assignment.Campaign.Name,
            QuestionnaireTitle = assignment.QuestionnaireVersion?.Questionnaire?.Title ?? "Unknown Questionnaire",
            VersionNumber = assignment.QuestionnaireVersion?.VersionNumber ?? "1.0",
            OrganizationName = assignment.TargetOrganization?.Name ?? "Unknown Organization",
            Deadline = assignment.Campaign.Deadline,
            Assignment = assignment,
            QuestionSummaries = questionSummaries,
            TotalQuestions = questions.Count,
            AnsweredQuestions = assignment.Responses.Count,
            CompletionPercentage = questions.Count > 0 ? 
                (int)((double)assignment.Responses.Count / questions.Count * 100) : 0
        };
    }

    private async Task<bool> HasDelegationForAssignment(int assignmentId, string userId)
    {
        return await _context.Delegations
            .AnyAsync(d => d.CampaignAssignmentId == assignmentId && 
                          d.ToUserId == userId && 
                          d.IsActive);
    }

    private async Task<bool> HasDelegationForQuestion(int assignmentId, int questionId, string userId)
    {
        return await _context.Delegations
            .AnyAsync(d => d.CampaignAssignmentId == assignmentId && 
                          d.QuestionId == questionId &&
                          d.ToUserId == userId && 
                          d.IsActive);
    }

    private async Task<bool> HasQuestionAssignmentForAssignment(int assignmentId, string userId)
    {
        return await _context.QuestionAssignments
            .AnyAsync(qa => qa.CampaignAssignmentId == assignmentId && 
                           qa.AssignedUserId == userId);
    }

    private async Task<bool> HasQuestionAssignmentForQuestion(int assignmentId, int questionId, string userId)
    {
        // Check for direct question assignment
        var hasDirectAssignment = await _context.QuestionAssignments
            .AnyAsync(qa => qa.CampaignAssignmentId == assignmentId && 
                           qa.QuestionId == questionId &&
                           qa.AssignedUserId == userId);
        
        if (hasDirectAssignment) return true;
        
        // Check for section assignment that includes this question
        var question = await _context.Questions.FindAsync(questionId);
        if (question == null) return false;
        
        var questionSection = question.Section ?? "Other";
        
        return await _context.QuestionAssignments
            .AnyAsync(qa => qa.CampaignAssignmentId == assignmentId && 
                           qa.AssignedUserId == userId && 
                           qa.SectionName == questionSection);
    }

    private async Task<Response?> GetOrCreateResponse(int assignmentId, int questionId)
    {
        var response = await _context.Responses
            .FirstOrDefaultAsync(r => r.CampaignAssignmentId == assignmentId && 
                                    r.QuestionId == questionId);

        if (response == null)
        {
            response = new Response
            {
                QuestionId = questionId,
                CampaignAssignmentId = assignmentId,
                ResponderId = CurrentUserId!,
                CreatedAt = DateTime.UtcNow
            };

            _context.Responses.Add(response);
            await _context.SaveChangesAsync();
        }

        return response;
    }

    private async Task LoadDelegateQuestionData(DelegateQuestionViewModel model)
    {
        var assignment = await _context.CampaignAssignments
            .Include(ca => ca.Campaign)
            .Include(ca => ca.QuestionnaireVersion)
                .ThenInclude(qv => qv.Questionnaire)
            .FirstOrDefaultAsync(ca => ca.Id == model.AssignmentId);

        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == model.QuestionId);

        if (assignment != null && question != null)
        {
            model.CampaignName = assignment.Campaign.Name;
            model.QuestionnaireTitle = assignment.QuestionnaireVersion.Questionnaire.Title;
            model.QuestionText = question.QuestionText;
            model.HelpText = question.HelpText;
        }

        // Load team members from the same organization
        model.TeamMembers = await _context.Users
            .Where(u => u.OrganizationId == CurrentOrganizationId && u.IsActive && u.Id != CurrentUserId)
            .Select(u => new TeamMemberViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    private string GetResponseValue(Response response)
    {
        return response.TextValue ?? 
               response.NumericValue?.ToString() ?? 
               response.DateValue?.ToString("yyyy-MM-dd") ?? 
               response.BooleanValue?.ToString() ?? 
               "No value";
    }

    private List<string>? GetQuestionOptions(string? optionsData)
    {
        if (string.IsNullOrEmpty(optionsData))
            return null;

        // First try JSON deserialization (for backward compatibility)
        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsData);
        }
        catch
        {
            // If JSON fails, try parsing as newline-separated text (current format)
            try
            {
                return optionsData
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(option => option.Trim())
                    .Where(option => !string.IsNullOrEmpty(option))
                    .ToList();
            }
            catch
            {
                return null;
            }
        }
    }

    private List<string>? GetSelectedValues(string? selectedValuesJson)
    {
        if (string.IsNullOrEmpty(selectedValuesJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(selectedValuesJson);
        }
        catch
        {
            return null;
        }
    }

    private string GetResponseSummary(Response response, QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.Select or QuestionType.Radio => 
                string.IsNullOrEmpty(response.SelectedValues) ? "Not selected" : 
                string.Join(", ", GetSelectedValues(response.SelectedValues) ?? new List<string>()),
            QuestionType.MultiSelect or QuestionType.Checkbox => 
                string.IsNullOrEmpty(response.SelectedValues) ? "None selected" :
                string.Join(", ", GetSelectedValues(response.SelectedValues) ?? new List<string>()),
            QuestionType.YesNo => response.BooleanValue?.ToString() ?? "Not answered",
            QuestionType.Number => FormatNumericResponse(response) ?? "Not answered",
            QuestionType.Date => response.DateValue?.ToString("yyyy-MM-dd") ?? "Not answered",
            QuestionType.FileUpload => $"{response.FileUploads?.Count ?? 0} file(s)",
            _ => response.TextValue ?? "Not answered"
        };
    }

    private string FormatNumericResponse(Response response)
    {
        if (response.NumericValue == null) return null;
        
        var question = response.Question;
        var value = response.NumericValue.Value.ToString("0.##");
        
        if (question.IsPercentage)
        {
            return $"{value}%";
        }
        else if (!string.IsNullOrEmpty(question.Unit))
        {
            return $"{value} {question.Unit}";
        }
        
        return value;
    }

    #endregion
} 