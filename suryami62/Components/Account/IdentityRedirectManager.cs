#region

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using suryami62.Data;

#endregion

namespace suryami62.Components.Account;

/// <summary>
///     Centralizes safe redirects used by account-related components.
/// </summary>
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    /// <summary>
    ///     The cookie name used to carry short-lived status messages across redirects.
    /// </summary>
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5)
    };

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    /// <summary>
    ///     Redirects to a relative URI while preventing open redirect attacks.
    /// </summary>
    /// <param name="uri">The destination URI.</param>
    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative)) uri = navigationManager.ToBaseRelativePath(uri);

        navigationManager.NavigateTo(uri);
    }

    /// <summary>
    ///     Redirects to a URI after applying query-string parameters.
    /// </summary>
    /// <param name="uri">The base destination URI.</param>
    /// <param name="queryParameters">The query-string parameters to append.</param>
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    /// <summary>
    ///     Redirects to a URI after writing a short-lived status message cookie.
    /// </summary>
    /// <param name="uri">The destination URI.</param>
    /// <param name="message">The message to persist for the next request.</param>
    /// <param name="context">The current HTTP context.</param>
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    /// <summary>
    ///     Redirects to the current page path.
    /// </summary>
    public void RedirectToCurrentPage()
    {
        RedirectTo(CurrentPath);
    }

    /// <summary>
    ///     Redirects to the current page path while storing a status message.
    /// </summary>
    /// <param name="message">The message to persist for the next request.</param>
    /// <param name="context">The current HTTP context.</param>
    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
    {
        RedirectToWithStatus(CurrentPath, message, context);
    }

    /// <summary>
    ///     Redirects to the invalid-user page when the expected identity user cannot be loaded.
    /// </summary>
    /// <param name="userManager">The user manager used to resolve the current user identifier.</param>
    /// <param name="context">The current HTTP context.</param>
    public void RedirectToInvalidUser(UserManager<ApplicationUser> userManager, HttpContext context)
    {
        RedirectToWithStatus("Account/InvalidUser",
            $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
    }
}