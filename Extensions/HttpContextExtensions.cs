using System.Security.Claims;
using ESGPlatform.Models.Entities;

namespace ESGPlatform.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current user's organization ID from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The organization ID of the current user</returns>
    /// <exception cref="InvalidOperationException">Thrown when user is not authenticated or organization ID is not found</exception>
    public static int GetCurrentUserOrganizationId(this HttpContext httpContext)
    {
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        // First, try to get organization ID from HttpContext.Items (set by OrganizationContextMiddleware)
        if (httpContext.Items.TryGetValue("CurrentOrganizationId", out var orgIdObj) && orgIdObj is int orgId)
        {
            return orgId;
        }

        // Fallback: Try to get organization ID from user claims (if stored there)
        var orgIdClaim = httpContext.User.FindFirst("OrganizationId");
        if (orgIdClaim != null && int.TryParse(orgIdClaim.Value, out var orgIdFromClaim))
        {
            return orgIdFromClaim;
        }

        throw new InvalidOperationException("Unable to determine user's organization ID");
    }

    /// <summary>
    /// Gets the current user's organization ID from the HttpContext, returning null if not found
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The organization ID of the current user, or null if not found</returns>
    public static int? TryGetCurrentUserOrganizationId(this HttpContext httpContext)
    {
        try
        {
            return httpContext.GetCurrentUserOrganizationId();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current user's ID from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The user ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when user is not authenticated</exception>
    public static string GetCurrentUserId(this HttpContext httpContext)
    {
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User ID not found in claims");
        }

        return userId;
    }

    /// <summary>
    /// Checks if the current user is in a specific role
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <param name="role">The role to check</param>
    /// <returns>True if user is in the role, false otherwise</returns>
    public static bool IsUserInRole(this HttpContext httpContext, string role)
    {
        return httpContext.User.IsInRole(role);
    }

    /// <summary>
    /// Checks if the current user is a platform admin
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>True if user is a platform admin, false otherwise</returns>
    public static bool IsCurrentUserPlatformAdmin(this HttpContext httpContext)
    {
        return httpContext.User.IsInRole("PlatformAdmin");
    }

    /// <summary>
    /// Gets the current user's email from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The user's email address</returns>
    public static string? GetCurrentUserEmail(this HttpContext httpContext)
    {
        return httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the current user's name from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The user's name</returns>
    public static string? GetCurrentUserName(this HttpContext httpContext)
    {
        return httpContext.User.FindFirst(ClaimTypes.Name)?.Value 
               ?? httpContext.User.Identity?.Name;
    }

    /// <summary>
    /// Gets the current user's organization name from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The organization name</returns>
    public static string? GetCurrentUserOrganizationName(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("CurrentOrganizationName", out var orgNameObj) && orgNameObj is string orgName)
        {
            return orgName;
        }
        return null;
    }

    /// <summary>
    /// Gets the current user's organization type from the HttpContext
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    /// <returns>The organization type</returns>
    public static OrganizationType? GetCurrentUserOrganizationType(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("CurrentOrganizationType", out var orgTypeObj) && orgTypeObj is OrganizationType orgType)
        {
            return orgType;
        }
        return null;
    }
}

// Placeholder interface for user service (to be implemented if needed)
public interface IUserService
{
    Task<int?> GetUserOrganizationIdAsync(string userId);
} 