#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class BlogPost : IConcurrencyTrackedEntity
{
    public int Id { get; set; }

    public uint Version { get; set; }

    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(DomainModelConstraints.BlogPostSlugMaxLength)]
    public string Slug { get; set; } = string.Empty;

    [Required] public string Content { get; set; } = string.Empty;

    [Required] public string Label { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required] public string Summary { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public Uri? ImageUrl { get; set; }
}