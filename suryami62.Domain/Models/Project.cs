#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class Project : IConcurrencyTrackedEntity, IValidatableObject
{
    public int Id { get; set; }

    public uint Version { get; set; }

    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required] public string Description { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty;

    public Uri? RepoUrl { get; set; }

    public Uri? DemoUrl { get; set; }

    public Uri? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var tagsValidationResult = ProjectTagFormatter.Validate(Tags);
        if (tagsValidationResult is not null)
        {
            yield return tagsValidationResult;
        }
    }
}