#region

using Microsoft.AspNetCore.Identity;

#endregion

namespace suryami62.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
internal sealed class ApplicationUser : IdentityUser
{
    internal static ApplicationUser Create()
    {
        return new ApplicationUser();
    }
}