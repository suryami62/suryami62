// ============================================================================
// PROJECT MODEL
// ============================================================================
// This model represents a portfolio project displayed on the Projects page.
// Portfolio projects showcase your work to visitors - websites, apps, etc.
//
// PROJECT DISPLAY CARD:
// Each project typically shows:
// - Title and description
// - Image/screenshot preview
// - Technology tags ("C#", "Blazor", "Azure")
// - Links to source code (RepoUrl) and live demo (DemoUrl)
//
// OPTIONAL FIELDS:
// RepoUrl, DemoUrl, and ImageUrl are nullable (can be null).
// Not all projects have public code, live demos, or images.
// Use null when information is not available.
//
// TAGS FORMAT:
// Tags is a simple string, not a list. Store as comma-separated:
//   "C#, Blazor, Entity Framework Core"
// The UI splits this string to display individual tags.
//
// ORDERING:
// DisplayOrder controls sort order on the Projects page.
// Lower numbers appear first. Use this to feature best projects at top.
// ============================================================================

#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a portfolio project showcased on the site.
///     Contains project details, links, and display information.
/// </summary>
public sealed class Project
{
    /// <summary>
    ///     Unique identifier for this project.
    ///     Database auto-generates this when project is created.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Project name shown to visitors.
    ///     Required field. Maximum length: 200 characters.
    ///     Example: "Personal Portfolio Website".
    /// </summary>
    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Detailed description of the project.
    ///     Explains what the project does, technologies used, your role.
    ///     Required field - no maximum length.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Comma-separated list of technology tags.
    ///     Examples: "C#, Blazor, Azure", "Python, Django, PostgreSQL".
    ///     Optional - can be empty string if no tags.
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    ///     Optional URL to source code repository (GitHub, GitLab, etc.).
    ///     Can be null if project is private or not open source.
    ///     Example: https://github.com/username/project-name
    /// </summary>
    public Uri? RepoUrl { get; set; }

    /// <summary>
    ///     Optional URL to live running application.
    ///     Can be null if no live demo available.
    ///     Example: https://myproject.azurewebsites.net
    /// </summary>
    public Uri? DemoUrl { get; set; }

    /// <summary>
    ///     Optional URL to project screenshot or preview image.
    ///     Can be null if no image available.
    ///     Example: https://example.com/images/project-preview.jpg
    /// </summary>
    public Uri? ImageUrl { get; set; }

    /// <summary>
    ///     Sort order on the Projects page.
    ///     Lower numbers appear first. Use to feature best projects.
    ///     Example: Featured project = 1, Regular project = 10.
    /// </summary>
    public int DisplayOrder { get; set; }
}