#region

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

    ConfigureIdentityTables(builder);

        builder.HasPostgresExtension("pg_trgm");

        var uriConverter = new ValueConverter<Uri?, string?>(
            uri => uri == null ? null : uri.ToString(),
            value => ParseAbsoluteUri(value));

        builder.Entity<Project>(entity =>
        {
            ConfigureXminConcurrencyToken(entity);

            entity.Property(p => p.RepoUrl).HasConversion(uriConverter);
            entity.Property(p => p.DemoUrl).HasConversion(uriConverter);
            entity.Property(p => p.ImageUrl).HasConversion(uriConverter);

            entity.HasIndex(p => p.DisplayOrder);
        });

        builder.Entity<BlogPost>(entity =>
        {
            ConfigureXminConcurrencyToken(entity);

            entity.Property(p => p.Date).HasColumnType("timestamp with time zone");
            entity.Property(p => p.ImageUrl).HasConversion(uriConverter);

            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => new { p.IsPublished, p.Date }).IsDescending(false, true);
            entity.HasIndex(p => p.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(p => p.Summary).HasMethod("gin").HasOperators("gin_trgm_ops");
        });

        builder.Entity<JourneyHistory>(entity =>
        {
            ConfigureXminConcurrencyToken(entity);

            entity.Property(item => item.Summary).HasDefaultValue(string.Empty);

            entity.HasIndex(item => new { item.Section, item.DisplayOrder });
        });

        builder.Entity<Setting>(entity =>
        {
            ConfigureXminConcurrencyToken(entity);

            entity.HasIndex(setting => setting.Key).IsUnique();
        });
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("asp_net_users");
            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("ix_asp_net_users_normalized_email");
            entity.HasIndex(user => user.NormalizedUserName)
                .HasDatabaseName("ux_asp_net_users_normalized_user_name");
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("asp_net_roles");
            entity.HasIndex(role => role.NormalizedName)
                .HasDatabaseName("ux_asp_net_roles_normalized_name");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("asp_net_role_claims"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("asp_net_user_claims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("asp_net_user_logins"));
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("asp_net_user_roles"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("asp_net_user_tokens"));
    }

    private static void ConfigureXminConcurrencyToken<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IConcurrencyTrackedEntity
    {
        entity.Property(item => item.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
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