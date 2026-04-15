// ============================================================================
// JOURNEY HISTORY MODEL
// ============================================================================
// This model represents timeline entries displayed on the About page.
// Think of it like a resume or CV timeline showing your career journey.
//
// ENUM vs CLASS:
// JourneySection (below) is an enum - a list of named values (like a dropdown).
// JourneyHistory is a class - a container with multiple properties.
//
// TWO TYPES OF ENTRIES:
// - Experience: Work history, jobs, careers, volunteer positions
// - Education: Degrees, certificates, courses, training
//
// ORDERING:
// DisplayOrder controls sorting within each section.
// Lower numbers appear first. Example: 1 = first job, 2 = second job.
//
// DISPLAY EXAMPLE:
// EXPERIENCE (section)
//   1. Software Engineer at Microsoft (2020-2023)
//      Summary of work done...
//   2. Junior Developer at Startup (2018-2020)
//      Summary of work done...
//
// EDUCATION (section)
//   1. B.S. Computer Science - MIT (2014-2018)
//      Summary of studies...
// ============================================================================

#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Categories for journey/timeline entries.
///     Used to group items into Experience vs Education sections.
/// </summary>
public enum JourneySection
{
    /// <summary>
    ///     No section assigned (default/invalid state).
    ///     Value: 0
    /// </summary>
    None = 0,

    /// <summary>
    ///     Work experience entries (jobs, internships, careers).
    ///     Value: 1
    /// </summary>
    Experience = 1,

    /// <summary>
    ///     Education entries (degrees, certificates, courses).
    ///     Value: 2
    /// </summary>
    Education = 2
}

/// <summary>
///     Represents a single timeline entry for the About page.
///     Can be a job (Experience) or a degree (Education).
/// </summary>
public sealed class JourneyHistory
{
    /// <summary>
    ///     Unique identifier for this journey entry.
    ///     Database auto-generates this when entry is created.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Which section this entry belongs to (Experience or Education).
    ///     Determines where it appears on the About page.
    /// </summary>
    public JourneySection Section { get; set; }

    /// <summary>
    ///     Title of the role or program.
    ///     Examples: "Software Engineer", "B.S. Computer Science".
    ///     Required field. Maximum length: 200 characters.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Company, school, or organization name.
    ///     Examples: "Microsoft", "Stanford University".
    ///     Required field. Maximum length: 300 characters.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.JourneyOrganizationMaxLength)]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    ///     Time period for this entry.
    ///     Examples: "2020 - 2023", "January 2020 - Present", "2018-2022".
    ///     Required field. Maximum length: 100 characters.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.JourneyPeriodMaxLength)]
    public string Period { get; set; } = string.Empty;

    /// <summary>
    ///     Description of responsibilities, achievements, or studies.
    ///     Shown below the title/organization on the timeline.
    ///     Required field - no maximum length (text field).
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    ///     Sort order within the section.
    ///     Lower numbers appear first. Used to arrange timeline chronologically.
    ///     Example: Current job = 1 (top), Previous job = 2, etc.
    /// </summary>
    public int DisplayOrder { get; set; }
}