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

public sealed class JourneyHistory
{
    public int Id { get; set; }

    public JourneySection Section { get; set; }

    [Required] [StringLength(200)] public string Title { get; set; } = string.Empty;

    [Required] [StringLength(300)] public string Organization { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string Period { get; set; } = string.Empty;

    [Required] public string Summary { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}