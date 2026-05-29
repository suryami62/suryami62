#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts { get; set; }

    public DbSet<Project> Projects { get; set; }

    public DbSet<JourneyHistory> JourneyHistories { get; set; }

    public DbSet<Setting> Settings { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeBlogPostDates();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        NormalizeBlogPostDates();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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

        builder.Entity<BlogPost>(entity =>
        {
            entity.Property(p => p.Date).HasColumnType("timestamp with time zone");
            entity.Property(p => p.ImageUrl).HasConversion(uriConverter);

            entity.HasIndex(p => p.Slug).IsUnique();
        });

        builder.Entity<JourneyHistory>(entity =>
        {
            entity.Property(item => item.Summary).HasDefaultValue(string.Empty);

            entity.HasIndex(item => new { item.Section, item.DisplayOrder });
        });

        builder.Entity<Setting>(entity =>
        {
            entity.HasIndex(setting => setting.Key).IsUnique();
        });
    }

    private static Uri? ParseAbsoluteUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return uri;

        return null;
    }

    private void NormalizeBlogPostDates()
    {
        foreach (var post in ChangeTracker.Entries<BlogPost>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                     .Select(entry => entry.Entity))
        {
            post.Date = NormalizeDateTimeToUtc(post.Date);
        }
    }

    private static DateTime NormalizeDateTimeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}