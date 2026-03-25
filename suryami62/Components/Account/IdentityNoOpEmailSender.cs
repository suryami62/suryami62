#region

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using suryami62.Data;

#endregion

namespace suryami62.Components.Account;

/// <summary>
///     Provides a placeholder email sender backed by the framework no-op implementation.
/// </summary>
/// <remarks>
///     This keeps Identity flows such as registration and password reset operational in development or early
///     deployments until a production email provider is wired into the account feature set.
/// </remarks>
internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly NoOpEmailSender emailSender = new();

    /// <summary>
    ///     Queues the account confirmation message through the no-op email backend.
    /// </summary>
    /// <param name="user">The user receiving the email.</param>
    /// <param name="email">The destination email address.</param>
    /// <param name="confirmationLink">The confirmation URL.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        return emailSender.SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    /// <summary>
    ///     Queues the password reset link message through the no-op email backend.
    /// </summary>
    /// <param name="user">The user receiving the email.</param>
    /// <param name="email">The destination email address.</param>
    /// <param name="resetLink">The password reset URL.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        return emailSender.SendEmailAsync(email, "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    /// <summary>
    ///     Queues the password reset code message through the no-op email backend.
    /// </summary>
    /// <param name="user">The user receiving the email.</param>
    /// <param name="email">The destination email address.</param>
    /// <param name="resetCode">The password reset code.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        return emailSender.SendEmailAsync(email, "Reset your password",
            $"Please reset your password using the following code: {resetCode}");
    }
}