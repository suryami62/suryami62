// ============================================================================
// APPLICATION USER
// ============================================================================
// This class represents a USER in the authentication system.
// It extends IdentityUser - ASP.NET Core's built-in user class.
//
// WHAT IS IdentityUser?
// A pre-built class from ASP.NET Core Identity that contains:
// - UserId (string, primary key)
// - UserName (string, login name)
// - Email (string, with confirmed status)
// - PasswordHash (string, hashed password)
// - PhoneNumber (string, with confirmed status)
// - Lockout status, security stamp, etc.
//
// WHY EXTEND IT?
// If you want to add CUSTOM fields (ProfilePicture, Bio, DateOfBirth, etc.),
// you add them to this class. Right now it has no extra fields, but the
// class exists as a "placeholder" for future expansion.
//
// SEALED KEYWORD:
// "sealed" = cannot be inherited further. Prevents accidental subclassing.
//
// DATABASE TABLE:
// This class maps to the "AspNetUsers" table (created by Identity).
// ============================================================================

#region

using Microsoft.AspNetCore.Identity;

#endregion

namespace suryami62.Data;

/// <summary>
///     Application's user account class. Extends IdentityUser for authentication.
///     Currently has no extra fields, but serves as extension point for profile data.
/// </summary>
/// <remarks>
///     IdentityUser base class provides:
///     - Id: User identifier (string, e.g., "a1b2c3d4-e5f6...")
///     - UserName: Login name (e.g., "john_doe")
///     - Email: Email address (e.g., "john@example.com")
///     - EmailConfirmed: bool - did user click confirmation link?
///     - PasswordHash: Hashed password (never store plain text!)
///     - SecurityStamp: Random value changed on password/security update
///     - PhoneNumber: Optional phone for 2FA
///     - TwoFactorEnabled: bool - is 2FA active?
///     - LockoutEnd: DateTime - when lockout expires (if any)
///     - LockoutEnabled: bool - can this account be locked?
///     - AccessFailedCount: int - failed login attempts (locks after threshold)
///     To add custom fields, declare properties here:
///     public string? ProfilePictureUrl { get; set; }
///     public string? Bio { get; set; }
///     public DateTime? DateOfBirth { get; set; }
///     Then create and run a migration: dotnet ef migrations add AddProfileFields
/// </remarks>
public sealed class ApplicationUser : IdentityUser
{
    // This class intentionally empty - all base functionality from IdentityUser
    // Add custom profile properties here when needed
}