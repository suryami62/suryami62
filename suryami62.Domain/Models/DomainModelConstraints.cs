// ============================================================================
// DOMAIN MODEL CONSTRAINTS
// ============================================================================
// This file stores maximum length limits for text fields across all models.
//
// WHY HAVE A SEPARATE FILE FOR LENGTHS?
// - Consistency: Titles have same max length everywhere (200 chars)
// - Maintainability: Change length in one place, affects all models
// - Database safety: Prevents SQL errors from over-long strings
//
// DATABASE COLUMN SIZES:
// When EF Core creates database tables, it uses these values to set
// column sizes. Too small = data truncation errors. Too large = wasted space.
//
// EXAMPLE USAGE in BlogPost.cs:
//   [StringLength(DomainModelConstraints.TitleMaxLength)]
//   public string Title { get; set; } = string.Empty;
//
// COMMON LENGTHS USED:
// - TitleMaxLength (200): Post titles, project titles, journey titles
// - BlogPostSlugMaxLength (250): URL-friendly slugs
// - JourneyOrganizationMaxLength (300): Company/school names
// - JourneyPeriodMaxLength (100): Date ranges like "2020 - 2023"
// ============================================================================

namespace suryami62.Domain.Models;

/// <summary>
///     Central repository for string length limits used by all domain models.
///     Using constants ensures consistency across the application.
/// </summary>
internal static class DomainModelConstraints
{
    /// <summary>
    ///     Maximum length for all titles (blog posts, projects, journey entries).
    ///     Value: 200 characters.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    ///     Maximum length for blog post URL slugs.
    ///     Slugs are URL-friendly versions of titles (e.g., "my-first-post").
    ///     Value: 250 characters.
    /// </summary>
    public const int BlogPostSlugMaxLength = 250;

    /// <summary>
    ///     Maximum length for organization names in journey entries.
    ///     Used for company names, school names, etc.
    ///     Value: 300 characters.
    /// </summary>
    public const int JourneyOrganizationMaxLength = 300;

    /// <summary>
    ///     Maximum length for period labels in journey entries.
    ///     Examples: "2020 - 2023", "January 2020 - Present".
    ///     Value: 100 characters.
    /// </summary>
    public const int JourneyPeriodMaxLength = 100;
}