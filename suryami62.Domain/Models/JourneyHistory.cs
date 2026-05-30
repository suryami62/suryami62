#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public enum JourneySection
{
    None = 0,

    Experience = 1,

    Education = 2
}

public sealed class JourneyHistory : IConcurrencyTrackedEntity
{
    public int Id { get; set; }

    public uint Version { get; set; }

    public JourneySection Section { get; set; }

    [Required]
    [StringLength(DomainModelConstraints.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(DomainModelConstraints.JourneyOrganizationMaxLength)]
    public string Organization { get; set; } = string.Empty;

    [Required]
    [StringLength(DomainModelConstraints.JourneyPeriodMaxLength)]
    public string Period { get; set; } = string.Empty;

    [Required] public string Summary { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}