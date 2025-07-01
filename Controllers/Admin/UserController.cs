using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
public class UserController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserController> _logger;

    public UserController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // GET: /User
    public async Task<IActionResult> Index(int? organizationId, string? role)
    {
        var query = _context.Users
            .Include(u => u.Organization)
            .AsQueryable();

        if (organizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        }

        var users = await query.OrderBy(u => u.Organization.Name).ThenBy(u => u.LastName).ToListAsync();

        var userViewModels = new List<UserManagementViewModel>();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            
            userViewModels.Add(new UserManagementViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                OrganizationName = user.Organization?.Name ?? "No Organization",
                OrganizationId = user.OrganizationId,
                Roles = userRoles.ToList(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        // Filter by role if specified
        if (!string.IsNullOrEmpty(role))
        {
            userViewModels = userViewModels.Where(u => u.Roles.Contains(role)).ToList();
        }

        // Populate filter dropdowns
        ViewBag.Organizations = new SelectList(
            await _context.Organizations.OrderBy(o => o.Name).ToListAsync(),
            "Id", "Name", organizationId);

        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(),
            "Name", "Name", role);

        ViewBag.SelectedOrganizationId = organizationId;
        ViewBag.SelectedRole = role;

        return View(userViewModels);
    }

    // GET: /User/Details/5
    public async Task<IActionResult> Details(string id)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        var model = new UserManagementViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            OrganizationName = user.Organization?.Name ?? "No Organization",
            OrganizationId = user.OrganizationId,
            Roles = userRoles.ToList(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return View(model);
    }

    // GET: /User/Invite
    public async Task<IActionResult> Invite()
    {
        var model = new UserInviteViewModel();

        // Populate dropdowns
        model.Organizations = new SelectList(
            await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync(),
            "Id", "Name").ToList();

        var roles = await _roleManager.Roles
            .Where(r => r.Name != "PlatformAdmin") // Don't allow inviting platform admins
            .OrderBy(r => r.Name)
            .ToListAsync();

        model.Roles = roles.Select(r => new SelectListItem
        {
            Value = r.Name!,
            Text = FormatRoleName(r.Name!)
        }).ToList();

        return View(model);
    }

    // POST: /User/Invite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(UserInviteViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email address already exists.");
            }
            else
            {
                // Create new user
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    OrganizationId = model.OrganizationId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true // Auto-confirm for invited users
                };

                // Generate a temporary password
                var tempPassword = GenerateTemporaryPassword();
                var result = await _userManager.CreateAsync(user, tempPassword);

                if (result.Succeeded)
                {
                    // Add role to user
                    await _userManager.AddToRoleAsync(user, model.Role);

                    _logger.LogInformation("User invited: {Email} to organization {OrganizationId} with role {Role} by {UserId}",
                        model.Email, model.OrganizationId, model.Role, CurrentUserId);

                    // TODO: Send invitation email with temporary password
                    TempData["Success"] = $"User '{model.Email}' invited successfully. Temporary password: {tempPassword}";
                    TempData["TempPassword"] = tempPassword;
                    
                    return RedirectToAction(nameof(Details), new { id = user.Id });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
        }

        // Repopulate dropdowns on error
        model.Organizations = new SelectList(
            await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync(),
            "Id", "Name", model.OrganizationId).ToList();

        var roles = await _roleManager.Roles
            .Where(r => r.Name != "PlatformAdmin")
            .OrderBy(r => r.Name)
            .ToListAsync();

        model.Roles = roles.Select(r => new SelectListItem
        {
            Value = r.Name!,
            Text = FormatRoleName(r.Name!),
            Selected = r.Name == model.Role
        }).ToList();

        return View(model);
    }

    // POST: /User/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Don't allow deactivating platform admins
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains("PlatformAdmin"))
        {
            TempData["Error"] = "Cannot deactivate Platform Administrator accounts.";
            return RedirectToAction(nameof(Details), new { id });
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        var status = user.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("User {Status}: {Email} by {UserId}",
            status, user.Email, CurrentUserId);

        TempData["Success"] = $"User '{user.Email}' {status} successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /User/ResetPassword/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var tempPassword = GenerateTemporaryPassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset for user: {Email} by {UserId}",
                user.Email, CurrentUserId);

            TempData["Success"] = $"Password reset successfully for '{user.Email}'. New temporary password: {tempPassword}";
            TempData["TempPassword"] = tempPassword;
        }
        else
        {
            TempData["Error"] = "Failed to reset password. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password that meets Identity requirements:
        // - At least 8 characters
        // - At least one uppercase letter
        // - At least one lowercase letter  
        // - At least one digit
        // - Non-alphanumeric characters are NOT required
        
        var random = new Random();
        
        // Define character sets (excluding confusing characters like 0, O, l, 1)
        var uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        var lowercase = "abcdefghjkmnpqrstuvwxyz";
        var digits = "23456789";
        
        var password = new List<char>
        {
            uppercase[random.Next(uppercase.Length)],    // At least one uppercase
            lowercase[random.Next(lowercase.Length)],    // At least one lowercase
            digits[random.Next(digits.Length)]           // At least one digit
        };
        
        // Fill the rest with random characters from all sets (8 total characters)
        var allChars = uppercase + lowercase + digits;
        for (int i = 3; i < 8; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }
        
        // Shuffle the password to avoid predictable patterns
        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }
        
        return new string(password.ToArray());
    }

    private string FormatRoleName(string roleName)
    {
        return roleName switch
        {
            "OrgAdmin" => "Organization Administrator",
            "CampaignManager" => "Campaign Manager",
            "LeadResponder" => "Lead Responder",
            "Responder" => "Responder",
            "Reviewer" => "Reviewer",
            _ => roleName
        };
    }
} 