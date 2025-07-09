using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using System.Security.Claims;

namespace ESGPlatform.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                // Update last login time
                if (user != null)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogInformation("User logged in: {Email}", model.Email);
                return RedirectToLocal(returnUrl);
            }
            
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {Email}", model.Email);
                return RedirectToAction(nameof(Lockout));
            }
            
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return View(nameof(Login));
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Get user email from Microsoft
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        
        // Check if email domain is allowed
        var allowedDomains = new[] { "es.ey.com" };
        if (!allowedDomains.Any(domain => email?.EndsWith($"@{domain}") == true))
        {
            ModelState.AddModelError(string.Empty, "Access denied. Only users from authorized domains can sign in.");
            return View(nameof(Login));
        }

        // Check if user is invited (optional - uncomment to enable invitation system)
        // var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        // var invitation = await context.UserInvitations
        //     .FirstOrDefaultAsync(i => i.Email == email && i.IsActive && !i.IsUsed);
        // if (invitation == null)
        // {
        //     ModelState.AddModelError(string.Empty, "Access denied. You must be invited to use this platform. Please contact an administrator.");
        //     return View(nameof(Login));
        // }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        if (result.IsLockedOut)
        {
            return RedirectToAction(nameof(Lockout));
        }
        else
        {
            // If the user does not have an account, then ask the user to create an account.
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? name?.Split(' ').FirstOrDefault() ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? name?.Split(' ').LastOrDefault() ?? "";

            return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                throw new InvalidOperationException("Error loading external login information during confirmation.");
            }
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                OrganizationId = 1, // Default to first organization for now
                IsActive = true, // Set to false if you want admin approval
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // Auto-confirm for external logins
            };
            
            // Optional: Set user as inactive until admin approval
            // user.IsActive = false;
            // user.RequiresAdminApproval = true;

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    // Add default role
                    await _userManager.AddToRoleAsync(user, "Responder");
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    return RedirectToLocal(returnUrl);
                }
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (ModelState.IsValid)
        {
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                OrganizationId = 1, // Default to first organization for now
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // Auto-confirm for now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password: {Email}", model.Email);
                
                // Add default role
                await _userManager.AddToRoleAsync(user, "Responder");
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        var model = new LoginWith2faViewModel { RememberMe = rememberMe };
        ViewData["ReturnUrl"] = returnUrl;

        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
} 