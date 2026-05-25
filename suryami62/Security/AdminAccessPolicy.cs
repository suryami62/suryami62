#region

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

#endregion

namespace suryami62.Security;

internal static class AdminAccessPolicy
{
    public const string Name = "SiteAdmin";

    public const string SectionName = "Security:AdminAccess";
}

internal sealed class AdminAccessRequirement : IAuthorizationRequirement
{
    private AdminAccessRequirement()
    {
    }

    public static AdminAccessRequirement Instance { get; } = new();
}

internal sealed class AdminAccessHandler : AuthorizationHandler<AdminAccessRequirement>
{
    private readonly IConfiguration _configuration;

    public AdminAccessHandler(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var allowedUserNames = LoadAllowedUserNames();

        var allowedEmails = LoadAllowedEmails();

        var userName = context.User.Identity?.Name;
        var email = GetUserEmail(context.User);

        var userNameIsAllowed = IsAllowed(userName, allowedUserNames);
        var emailIsAllowed = IsAllowed(email, allowedEmails);

        if (userNameIsAllowed || emailIsAllowed)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private string[] LoadAllowedUserNames()
    {
        var sectionPath = $"{AdminAccessPolicy.SectionName}:AllowedUserNames";
        var allowedUserNames = _configuration.GetSection(sectionPath).Get<string[]>();

        if (allowedUserNames is null) return Array.Empty<string>();

        return allowedUserNames;
    }

    private string[] LoadAllowedEmails()
    {
        var sectionPath = $"{AdminAccessPolicy.SectionName}:AllowedEmails";
        var allowedEmails = _configuration.GetSection(sectionPath).Get<string[]>();

        if (allowedEmails is null) return Array.Empty<string>();

        return allowedEmails;
    }

    private static string? GetUserEmail(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            email = user.FindFirstValue("email");

        return email;
    }

    private static bool IsAllowed(string? candidate, string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        foreach (var value in allowedValues)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var trimmedValue = value.Trim();
            var match = string.Equals(trimmedValue, candidate, StringComparison.OrdinalIgnoreCase);

            if (match) return true;
        }

        return false;
    }
}