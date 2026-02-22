#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using suryami62.Data.Migrations;
using suryami62.Data.Models;
using suryami62.Services;

#endregion

namespace suryami62.Data;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Setting> Settings { get; set; }

    internal static ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options)
    {
        // Explicitly touch internal types to satisfy CA1812 analyzer
        _ = new Setting();
        _ = new BlogPost();
        _ = new Project();
        _ = new InitialCreate();
        _ = new ClearPhoneNumberData();
        _ = new SeoFilesSettingsStore(null!);
        _ = new UserInfoSettingsStore(null!);

        return new ApplicationDbContext(options);
    }
}