#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a portfolio project showcased on the site.
/// </summary>
public sealed class Project
{
    /// <summary>
    ///     Gets or sets the unique identifier of the project.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the project title shown to visitors.
    /// </summary>
    [Required] [StringLength(200)] public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the descriptive text for the project.
    /// </summary>
    [Required] public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a comma-separated list of project tags.
    /// </summary>
    public string Tags { get; set; } = string.Empty; // Comma separated tags

    /// <summary>
    ///     Gets or sets the optional source repository URL.
    /// </summary>
    public Uri? RepoUrl { get; set; }

    /// <summary>
    ///     Gets or sets the optional live demo URL.
    /// </summary>
    public Uri? DemoUrl { get; set; }

    /// <summary>
    ///     Gets or sets the optional preview image URL.
    /// </summary>
    public Uri? ImageUrl { get; set; }

    /// <summary>
    ///     Gets or sets the ordering value used in project listings.
    /// </summary>
    public int DisplayOrder { get; set; }
}