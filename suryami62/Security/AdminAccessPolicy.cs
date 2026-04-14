// ============================================================================
// ADMIN ACCESS POLICY
// ============================================================================
// This file defines who can access the admin area of the website.
// It uses ASP.NET Core's authorization system to check if a user is allowed.
//
// HOW IT WORKS:
// 1. AdminAccessPolicy - Constants for policy name and config section
// 2. AdminAccessRequirement - A marker class representing "must be admin"
// 3. AdminAccessHandler - The logic that checks if user is on the allow-list
//
// CONFIGURATION (in appsettings.json):
// {
//   "Security": {
//     "AdminAccess": {
//       "AllowedUserNames": ["admin", "surya"],
//       "AllowedEmails": ["admin@example.com"]
//     }
//   }
// }
//
// USAGE IN RAZOR COMPONENTS:
// @attribute [Authorize(Policy = AdminAccessPolicy.Name)]
// ============================================================================

#region

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

#endregion

namespace suryami62.Security;

/// <summary>
///     Constants for the admin access authorization policy.
/// </summary>
internal static class AdminAccessPolicy
{
    /// <summary>
    ///     The name of the authorization policy. Use this in [Authorize] attributes.
    /// </summary>
    public const string Name = "SiteAdmin";

    /// <summary>
    ///     The configuration section path where allowed users are defined.
    ///     Full path: Security:AdminAccess:AllowedUserNames and Security:AdminAccess:AllowedEmails
    /// </summary>
    public const string SectionName = "Security:AdminAccess";
}

/// <summary>
///     A requirement that represents "user must be an admin".
///     This is just a marker class - the actual logic is in AdminAccessHandler.
/// </summary>
internal sealed class AdminAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Private constructor prevents creating new instances.
    ///     Use the shared Instance property instead.
    /// </summary>
    private AdminAccessRequirement()
    {
    }

    /// <summary>
    ///     The single shared instance of this requirement.
    /// </summary>
    public static AdminAccessRequirement Instance { get; } = new();
}

/// <summary>
///     Handles authorization by checking if the user's name or email is on the allow-list.
/// </summary>
internal sealed class AdminAccessHandler : AuthorizationHandler<AdminAccessRequirement>
{
    // Configuration is injected to read the allow-lists from appsettings.json
    private readonly IConfiguration _configuration;

    /// <summary>
    ///     Creates a new handler with the application configuration.
    /// </summary>
    /// <param name="configuration">The configuration containing allow-lists.</param>
    public AdminAccessHandler(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <summary>
    ///     Checks if the current user is allowed to access admin areas.
    /// </summary>
    /// <param name="context">Contains information about the current user.</param>
    /// <param name="requirement">The requirement being checked.</param>
    /// <returns>A completed task (authorization is synchronous).</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        // Step 1: Load allowed usernames from configuration
        var allowedUserNames = LoadAllowedUserNames();

        // Step 2: Load allowed emails from configuration
        var allowedEmails = LoadAllowedEmails();

        // Step 3: Get the current user's name and email
        var userName = context.User.Identity?.Name;
        var email = GetUserEmail(context.User);

        // Step 4: Check if user is on either allow-list
        var userNameIsAllowed = IsAllowed(userName, allowedUserNames);
        var emailIsAllowed = IsAllowed(email, allowedEmails);

        if (userNameIsAllowed || emailIsAllowed)
            // User is allowed - mark the requirement as satisfied
            context.Succeed(requirement);

        // Return completed task (no async operations needed)
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Loads the list of allowed usernames from configuration.
    /// </summary>
    private string[] LoadAllowedUserNames()
    {
        var sectionPath = $"{AdminAccessPolicy.SectionName}:AllowedUserNames";
        var allowedUserNames = _configuration.GetSection(sectionPath).Get<string[]>();

        if (allowedUserNames is null) return Array.Empty<string>();

        return allowedUserNames;
    }

    /// <summary>
    ///     Loads the list of allowed emails from configuration.
    /// </summary>
    private string[] LoadAllowedEmails()
    {
        var sectionPath = $"{AdminAccessPolicy.SectionName}:AllowedEmails";
        var allowedEmails = _configuration.GetSection(sectionPath).Get<string[]>();

        if (allowedEmails is null) return Array.Empty<string>();

        return allowedEmails;
    }

    /// <summary>
    ///     Gets the user's email from their claims.
    ///     Checks both standard ClaimTypes.Email and "email" claim.
    /// </summary>
    private static string? GetUserEmail(ClaimsPrincipal user)
    {
        // Try standard email claim first
        var email = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            // Fall back to "email" claim (some providers use this)
            email = user.FindFirstValue("email");

        return email;
    }

    /// <summary>
    ///     Checks if a value matches any entry in the allowed list (case-insensitive).
    /// </summary>
    /// <param name="candidate">The value to check (username or email).</param>
    /// <param name="allowedValues">The list of allowed values.</param>
    /// <returns>True if the candidate is in the allowed list.</returns>
    private static bool IsAllowed(string? candidate, string[] allowedValues)
    {
        // Empty or whitespace values are never allowed
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        // Check each allowed value for a match
        foreach (var value in allowedValues)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var trimmedValue = value.Trim();
            var match = string.Equals(trimmedValue, candidate, StringComparison.OrdinalIgnoreCase);

            if (match) return true;
        }

        // No match found
        return false;
    }
}