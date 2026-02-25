#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class Project
{
    public int Id { get; set; }

    [Required] [StringLength(200)] public string Title { get; set; } = string.Empty;

    [Required] public string Description { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty; // Comma separated tags

    public string? RepoUrl { get; set; }

    public string? DemoUrl { get; set; }

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
}