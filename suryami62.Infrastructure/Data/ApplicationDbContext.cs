#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Setting> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        var uriConverter = new ValueConverter<Uri?, string?>(
            uri => uri == null ? null : uri.ToString(),
            value => ParseAbsoluteUri(value));

        builder.Entity<Project>(entity =>
        {
            entity.Property(p => p.RepoUrl).HasConversion(uriConverter);
            entity.Property(p => p.DemoUrl).HasConversion(uriConverter);
            entity.Property(p => p.ImageUrl).HasConversion(uriConverter);
        });

        builder.Entity<BlogPost>(entity => { entity.Property(p => p.ImageUrl).HasConversion(uriConverter); });
    }

    private static Uri? ParseAbsoluteUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}