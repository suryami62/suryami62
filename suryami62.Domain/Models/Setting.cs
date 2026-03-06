#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public sealed class Setting
{
    public int Id { get; set; }

    [Required] public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}