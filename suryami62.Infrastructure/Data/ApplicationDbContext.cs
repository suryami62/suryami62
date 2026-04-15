// ============================================================================
// APPLICATION DATABASE CONTEXT
// ============================================================================
// This class is the BRIDGE between your C# code and the PostgreSQL database.
// Think of it as the "database connection manager" for your application.
//
// WHAT IS DbContext?
// EF Core's main class for database operations. It:
// - Tracks which objects have changed (change tracking)
// - Generates SQL queries from LINQ
// - Manages database connections
// - Saves changes back to database (SaveChangesAsync)
//
// WHAT IS IdentityDbContext?
// A pre-built DbContext that includes ASP.NET Core Identity tables:
// - Users (AspNetUsers)
// - Roles (AspNetRoles)
// - User logins (AspNetUserLogins)
// - User roles mapping (AspNetUserRoles)
// - etc.
// Inheriting from IdentityDbContext gives you all these tables "for free"
// without writing migration code for them.
//
// WHAT IS DbSet<T>?
// Represents a database TABLE. Each DbSet property = one table.
// BlogPosts DbSet = "BlogPosts" table in PostgreSQL.
// You query and modify data through these DbSet properties.
//
// WHAT IS ValueConverter?
// Database stores strings. C# code uses Uri objects.
// ValueConverter converts Uri <-> string automatically.
// Example: new Uri("https://example.com") stored as "https://example.com" in DB.
//
// OnModelCreating() = CONFIGURATION METHOD
// Called once when EF Core builds the model. Configure table names, columns,
// indexes, foreign keys, default values, etc.
//
// COMPOSITE INDEX EXPLAINED:
// entity.HasIndex(item => new { item.Section, item.DisplayOrder })
// Creates index: (Section, DisplayOrder) together.
// Fast queries: WHERE Section = 'Work' ORDER BY DisplayOrder
// Slow queries: WHERE DisplayOrder = 5 (index not used effectively)
// ============================================================================

#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Data;

/// <summary>
///     Database context for the application. Manages all database connections,
///     change tracking, and queries. Extends IdentityDbContext for user authentication.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    ///     Creates a new database context with the given connection options.
    /// </summary>
    /// <param name="options">Connection string, provider (PostgreSQL), logging, etc.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        // Base constructor handles the options (connection string, provider, etc.)
    }
    // These properties represent database TABLES
    // EF Core creates these tables if they don't exist (via migrations)
    // You query them like: await context.BlogPosts.Where(...).ToListAsync()

    /// <summary>
    ///     The BlogPosts table. Contains all blog articles.
    /// </summary>
    public DbSet<BlogPost> BlogPosts { get; set; }

    /// <summary>
    ///     The Projects table. Contains portfolio project information.
    /// </summary>
    public DbSet<Project> Projects { get; set; }

    /// <summary>
    ///     The JourneyHistories table. Contains career/education timeline entries.
    /// </summary>
    public DbSet<JourneyHistory> JourneyHistories { get; set; }

    /// <summary>
    ///     The Settings table. Contains key-value configuration pairs.
    /// </summary>
    public DbSet<Setting> Settings { get; set; }

    /// <summary>
    ///     Configures how C# entities map to database tables.
    ///     Called once when EF Core initializes. Set up indexes, converters, defaults.
    /// </summary>
    /// <param name="builder">The model builder for fluent configuration.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Step 1: Validate builder parameter
        ArgumentNullException.ThrowIfNull(builder);

        // Step 2: Call base Identity configuration (users, roles, etc.)
        // This sets up all the Identity tables we inherited
        base.OnModelCreating(builder);

        // Step 3: Create ValueConverter for Uri <-> string conversion
        // Database stores strings like "https://github.com/suryami62/project"
        // C# code uses Uri objects for type safety and URL validation
        // ValueConverter handles the two-way conversion automatically
        var uriConverter = new ValueConverter<Uri?, string?>(
            // C# Uri -> Database string (Model to Database)
            uri => uri == null ? null : uri.ToString(),
            // Database string -> C# Uri (Database to Model)
            value => ParseAbsoluteUri(value));

        // Step 4: Configure Project entity (table: Projects)
        // Map all URL properties to use the Uri converter
        builder.Entity<Project>(entity =>
        {
            // RepoUrl is a Uri in C#, string in database
            // EF Core uses uriConverter to convert automatically
            entity.Property(p => p.RepoUrl).HasConversion(uriConverter);
            entity.Property(p => p.DemoUrl).HasConversion(uriConverter);
            entity.Property(p => p.ImageUrl).HasConversion(uriConverter);
        });

        // Step 5: Configure BlogPost entity (table: BlogPosts)
        // Only ImageUrl needs Uri conversion (PostAt is DateTimeOffset, etc.)
        builder.Entity<BlogPost>(entity => { entity.Property(p => p.ImageUrl).HasConversion(uriConverter); });

        // Step 6: Configure JourneyHistory entity (table: JourneyHistories)
        // Add database index for fast queries by Section + DisplayOrder
        builder.Entity<JourneyHistory>(entity =>
        {
            // Set default value for Summary column (empty string if not provided)
            // Prevents null values in database for this required field
            entity.Property(item => item.Summary).HasDefaultValue(string.Empty);

            // Create composite index on (Section, DisplayOrder)
            // Fast queries: WHERE Section = 'Work' ORDER BY DisplayOrder
            // PostgreSQL uses this index for the above query pattern
            entity.HasIndex(item => new { item.Section, item.DisplayOrder });
        });

        // Note: Setting entity uses default configuration (no extra mapping needed)
        // EF Core infers table/columns from property names automatically
    }

    /// <summary>
    ///     Parses a string into a Uri object.
    ///     Returns null if string is empty or not a valid absolute URL.
    /// </summary>
    /// <param name="value">The string from database (e.g., "https://example.com").</param>
    /// <returns>Parsed Uri object, or null if invalid/empty.</returns>
    private static Uri? ParseAbsoluteUri(string? value)
    {
        // Step 1: Handle null or whitespace input
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Step 2: Try to parse as absolute URI
        // UriKind.Absolute = must be complete URL with scheme (http://, https://)
        // Returns true if valid URI, false if malformed
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return uri;

        // Step 3: Invalid URL format - return null
        // EF Core will set the property to null when loading from database
        return null;
    }
}