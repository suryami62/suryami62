// ============================================================================
// SETTING MODEL
// ============================================================================
// This model stores application settings as key-value pairs in the database.
//
// WHAT ARE KEY-VALUE PAIRS?
// Like a dictionary or a real-world key box:
// - Key: Unique identifier (e.g., "Site:Title")
// - Value: The data stored (e.g., "My Portfolio Site")
//
// WHY STORE IN DATABASE?
// Unlike appsettings.json (which requires app restart to change),
// database settings can be updated at runtime by administrators.
//
// SETTING KEY NAMING:
// Use colon-separated hierarchy for organization:
//   Site:Title       - Website title
//   Seo:BaseUrl      - Base URL for sitemap generation
//   UserInfo:Email   - Contact email address
//
// EXAMPLES:
// Key                           Value
// Site:Title                    "John's Portfolio"
// Registration:Enabled          "true"
// Seo:EnableSitemap             "true"
// UserInfo:Github               "https://github.com/johndoe"
//
// DATA TYPES:
// All values are stored as strings. Convert to other types when reading:
//   bool enabled = bool.Parse(setting.Value);  // "true" -> true
// ============================================================================

#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

/// <summary>
///     Represents a single application setting stored in the database.
///     Simple key-value pair structure for configuration data.
/// </summary>
public sealed class Setting
{
    /// <summary>
    ///     Unique identifier for this setting row.
    ///     Database auto-generates this when setting is created.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The setting key/identifier.
    ///     Must be unique across all settings. Required field.
    ///     Examples: "Site:Title", "Registration:Enabled".
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     The setting value stored as a string.
    ///     Can be empty string if no value set.
    ///     Examples: "My Site Title", "true", "https://github.com/user".
    /// </summary>
    public string Value { get; set; } = string.Empty;
}