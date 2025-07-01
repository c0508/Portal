using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ESGPlatform.Middleware;

public class OrganizationContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrganizationContextMiddleware> _logger;

    public OrganizationContextMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<OrganizationContextMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for anonymous requests or static files
        if (!context.User.Identity?.IsAuthenticated == true ||
            context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/lib") ||
            context.Request.Path.StartsWithSegments("/favicon.ico"))
        {
            await _next(context);
            return;
        }

        try
        {
            await SetOrganizationContextAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting organization context for user {UserId}", 
                context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        await _next(context);
    }

    private async Task SetOrganizationContextAsync(HttpContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Get user's organization with type information
        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { 
                u.OrganizationId, 
                u.Organization.Name, 
                u.Organization.Type 
            })
            .FirstOrDefaultAsync();

        if (user != null)
        {
            // Check if user is Platform Admin
            var isPlatformAdmin = context.User.IsInRole("PlatformAdmin");
            
            if (!isPlatformAdmin)
            {
                // Set organization context for data filtering (non-admin users only)
                dbContext.SetOrganizationContext(user.OrganizationId);
                _logger.LogDebug("Organization filtering applied for user {UserId}", userId);
            }
            else
            {
                _logger.LogDebug("Platform admin detected - no organization filtering applied, but context still set");
            }
            
            // Store in HttpContext for use in controllers (for both admins and regular users)
            context.Items["CurrentOrganizationId"] = user.OrganizationId;
            context.Items["CurrentOrganizationName"] = user.Name;
            context.Items["CurrentOrganizationType"] = user.Type;

            _logger.LogDebug("Organization context set to {OrganizationId} ({OrganizationName}, {OrganizationType}) for user {UserId}",
                user.OrganizationId, user.Name, user.Type, userId);
        }
        else
        {
            _logger.LogWarning("User {UserId} not found or has no organization", userId);
        }
    }
}

public static class OrganizationContextMiddlewareExtensions
{
    public static IApplicationBuilder UseOrganizationContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OrganizationContextMiddleware>();
    }
} 