#region

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

#endregion

namespace suryami62.Security;

/// <summary>
///     Declares authorization policy names and configuration keys for admin access.
/// </summary>
internal static class AdminAccessPolicy
{
    /// <summary>
    ///     The authorization policy name used by admin pages.
    /// </summary>
    public const string Name = "SiteAdmin";

    /// <summary>
    ///     The configuration section that stores admin access allow-lists.
    /// </summary>
    public const string SectionName = "Security:AdminAccess";
}

/// <summary>
///     Represents the requirement that a user must satisfy to access the admin area.
/// </summary>
internal sealed class AdminAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Prevents external construction so callers reuse the shared singleton requirement instance.
    /// </summary>
    private AdminAccessRequirement()
    {
    }

    public static AdminAccessRequirement Instance { get; } = new();
}

/// <summary>
///     Grants admin access when the authenticated user's name or email is allow-listed in configuration.
/// </summary>
internal sealed class AdminAccessHandler(IConfiguration configuration) : AuthorizationHandler<AdminAccessRequirement>
{
    /// <summary>
    ///     Evaluates whether the current user satisfies the admin access requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement being evaluated.</param>
    /// <returns>A completed task.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var allowedUserNames = configuration
            .GetSection($"{AdminAccessPolicy.SectionName}:AllowedUserNames")
            .Get<string[]>() ?? [];

        var allowedEmails = configuration
            .GetSection($"{AdminAccessPolicy.SectionName}:AllowedEmails")
            .Get<string[]>() ?? [];

        var userName = context.User.Identity?.Name;
        var email = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email");

        if (IsAllowed(userName, allowedUserNames) ||
            IsAllowed(email, allowedEmails))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Determines whether a candidate value matches any configured allow-listed value.
    /// </summary>
    /// <param name="candidate">The value to check.</param>
    /// <param name="allowedValues">The configured allowed values.</param>
    /// <returns><see langword="true" /> when a match is found; otherwise <see langword="false" />.</returns>
    private static bool IsAllowed(string? candidate, IEnumerable<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        return allowedValues.Any(value =>
            string.Equals(value?.Trim(), candidate, StringComparison.OrdinalIgnoreCase));
    }
}