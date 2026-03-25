#region

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using suryami62.Data;

#endregion

namespace suryami62.Components.Account;

/// <summary>
///     Revalidates the authenticated user's security stamp for long-lived Blazor Server circuits.
/// </summary>
/// <remarks>
///     This provider keeps interactive account and admin sessions aligned with the current Identity record by
///     periodically checking whether the connected principal still matches the persisted security stamp.
/// </remarks>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> options)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    /// <summary>
    ///     Gets the interval between security-stamp revalidation checks for the active circuit.
    /// </summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>
    ///     Validates that the current authentication state is still backed by a valid security stamp.
    /// </summary>
    /// <param name="authenticationState">The authentication state to validate.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the state is still valid; otherwise <see langword="false" />.</returns>
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await ValidateSecurityStampAsync(userManager, authenticationState.User).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Compares the security stamp in the connected principal against the latest persisted user value.
    /// </summary>
    /// <param name="userManager">The user manager used to resolve the current user.</param>
    /// <param name="principal">The authenticated principal to validate.</param>
    /// <returns><see langword="true" /> when the stamp matches or stamps are unsupported; otherwise <see langword="false" />.</returns>
    private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return false;

        if (!userManager.SupportsUserSecurityStamp) return true;

        var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user).ConfigureAwait(false);
        return principalStamp == userStamp;
    }
}