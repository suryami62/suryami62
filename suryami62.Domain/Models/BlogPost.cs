#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class BlogPost
{
    public int Id { get; set; }

    [Required] [StringLength(200)] public string Title { get; set; } = string.Empty;

    [Required] [StringLength(250)] public string Slug { get; set; } = string.Empty;

    [Required] public string Content { get; set; } = string.Empty;

    [Required] public string Label { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required] public string Summary { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public Uri? ImageUrl { get; set; }
}