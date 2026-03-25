#region

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using suryami62.Data;

#endregion

namespace suryami62.Components.Account;

/// <summary>
///     Maps account-related endpoints required by the Blazor Identity components.
/// </summary>
internal static partial class IdentityComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Maps additional identity endpoints used by the account area.
    /// </summary>
    /// <remarks>
    ///     These handlers support form posts emitted by the account Razor components, especially the logout flow
    ///     and the personal-data export action from the authenticated manage area.
    /// </remarks>
    /// <param name="endpoints">The route builder used to register endpoints.</param>
    /// <returns>The account route group builder.</returns>
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal _,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync().ConfigureAwait(false);
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

        manageGroup.MapPost("/DownloadPersonalData", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
        {
            var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
            if (user is null)
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");

            var userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);
            LogPersonalDataRequest(downloadLogger, userId);

            // Restrict the export payload to properties explicitly marked as personal data.
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps) personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");

            personalData.Add("Authenticator Key",
                (await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false))!);
            var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

            context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
            return TypedResults.File(fileBytes, "application/json", "PersonalData.json");
        });

        return accountGroup;
    }

    /// <summary>
    ///     Logs that a user requested a personal data export.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userId">The identifier of the requesting user.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' asked for their personal data.")]
    static partial void LogPersonalDataRequest(ILogger logger, string userId);
}