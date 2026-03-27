namespace suryami62.Domain.Models;

/// <summary>
///     Centralizes the persisted string length limits used by content models.
/// </summary>
/// <remarks>
///     Keeping these limits in one place reduces the chance that data annotations drift apart between models that
///     represent similar concepts.
/// </remarks>
internal static class DomainModelConstraints
{
    /// <summary>
    ///     Maximum length for titles shown across the public site.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    ///     Maximum length for a blog post slug stored in routes and persistence.
    /// </summary>
    public const int BlogPostSlugMaxLength = 250;

    /// <summary>
    ///     Maximum length for the organization value in a journey entry.
    /// </summary>
    public const int JourneyOrganizationMaxLength = 300;

    /// <summary>
    ///     Maximum length for the human-readable period label in a journey entry.
    /// </summary>
    public const int JourneyPeriodMaxLength = 100;
}