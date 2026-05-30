#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class Setting : IConcurrencyTrackedEntity
{
    public int Id { get; set; }

    public uint Version { get; set; }

    [Required]
    [StringLength(DomainModelConstraints.SettingKeyMaxLength)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}