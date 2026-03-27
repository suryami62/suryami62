#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a persisted application setting stored as a key/value pair.
/// </summary>
public sealed class Setting
{
    /// <summary>
    ///     Gets or sets the unique identifier of the setting.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the unique configuration key.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the stored value for the key.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}