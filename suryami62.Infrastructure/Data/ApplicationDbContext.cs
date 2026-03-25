#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Data;

/// <summary>
///     EF Core database context that stores identity data and site content.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    ///     Gets or sets the blog posts persisted by the application.
    /// </summary>
    public DbSet<BlogPost> BlogPosts { get; set; }

    /// <summary>
    ///     Gets or sets the portfolio projects persisted by the application.
    /// </summary>
    public DbSet<Project> Projects { get; set; }

    /// <summary>
    ///     Gets or sets the journey timeline entries persisted by the application.
    /// </summary>
    public DbSet<JourneyHistory> JourneyHistories { get; set; }

    /// <summary>
    ///     Gets or sets the key/value settings persisted by the application.
    /// </summary>
    public DbSet<Setting> Settings { get; set; }

    /// <summary>
    ///     Configures entity mappings and value converters for the application's models.
    /// </summary>
    /// <param name="builder">The model builder used to configure entities.</param>
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

        builder.Entity<JourneyHistory>(entity =>
        {
            entity.Property(item => item.Summary).HasDefaultValue(string.Empty);
            entity.HasIndex(item => new { item.Section, item.DisplayOrder });
        });
    }

    /// <summary>
    ///     Rehydrates an absolute URI from the string value stored in the database.
    /// </summary>
    /// <param name="value">The raw value read from persistence.</param>
    /// <returns>The parsed absolute URI, or <see langword="null" /> when the value is empty or invalid.</returns>
    private static Uri? ParseAbsoluteUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}