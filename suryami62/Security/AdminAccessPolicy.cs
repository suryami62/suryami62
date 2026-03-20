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

internal sealed class AdminAccessRequirement : IAuthorizationRequirement;

internal sealed class AdminAccessHandler(IConfiguration configuration) : AuthorizationHandler<AdminAccessRequirement>
{
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

    private static bool IsAllowed(string? candidate, IEnumerable<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        return allowedValues.Any(value =>
            string.Equals(value?.Trim(), candidate, StringComparison.OrdinalIgnoreCase));
    }
}