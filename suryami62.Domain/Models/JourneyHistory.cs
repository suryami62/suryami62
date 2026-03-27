#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents the logical section used to group journey items on the about page.
/// </summary>
public enum JourneySection
{
    /// <summary>
    ///     Indicates that no section has been selected.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Indicates a work experience entry.
    /// </summary>
    Experience = 1,

    /// <summary>
    ///     Indicates an education entry.
    /// </summary>
    Education = 2
}

/// <summary>
///     Represents a single timeline entry shown in the experience or education journey.
/// </summary>
public sealed class JourneyHistory
{
    /// <summary>
    ///     Gets or sets the unique identifier of the journey item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the section where the item is displayed.
    /// </summary>
    public JourneySection Section { get; set; }

    /// <summary>
    ///     Gets or sets the title of the role, program, or milestone.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the organization associated with the entry.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.JourneyOrganizationMaxLength)]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the date range or period label for the entry.
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.JourneyPeriodMaxLength)]
    public string Period { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the descriptive summary for the entry.
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sort order within the selected section.
    /// </summary>
    public int DisplayOrder { get; set; }
}