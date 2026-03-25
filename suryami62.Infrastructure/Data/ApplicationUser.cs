#region

using Microsoft.AspNetCore.Identity;

#endregion

namespace suryami62.Data;

/// <summary>
///     Represents the application's identity user record.
/// </summary>
/// <remarks>
///     Extend this type when the site needs additional profile fields beyond the default ASP.NET Core Identity user model.
/// </remarks>
public sealed class ApplicationUser : IdentityUser;