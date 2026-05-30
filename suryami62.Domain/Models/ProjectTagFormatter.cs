#region

using System.ComponentModel.DataAnnotations;

#endregion

namespace suryami62.Domain.Models;

public static class ProjectTagFormatter
{
    public const int MaxTagCount = 20;

    public const int MaxTagLength = 40;

    public static string[] Split(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return Array.Empty<string>();
        }

        var normalizedTags = new List<string>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawTag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalizedTag = NormalizeWhitespace(rawTag);
            if (normalizedTag.Length == 0 || !seenTags.Add(normalizedTag))
            {
                continue;
            }

            normalizedTags.Add(normalizedTag);
        }

        return normalizedTags.ToArray();
    }

    public static string Format(string? tags)
    {
        return string.Join(", ", Split(tags));
    }

    public static ValidationResult? Validate(string? tags)
    {
        if (ContainsUnsupportedControlCharacter(tags))
        {
            return new ValidationResult(
                "Project tags cannot contain control characters.",
                [nameof(Project.Tags)]);
        }

        var normalizedTags = Split(tags);
        if (normalizedTags.Length > MaxTagCount)
        {
            return new ValidationResult(
                $"Use {MaxTagCount} or fewer project tags.",
                [nameof(Project.Tags)]);
        }

        var longTag = normalizedTags.FirstOrDefault(tag => tag.Length > MaxTagLength);
        if (longTag is not null)
        {
            return new ValidationResult(
                $"Project tag '{longTag}' must be {MaxTagLength} characters or fewer.",
                [nameof(Project.Tags)]);
        }

        return null;
    }

    private static string NormalizeWhitespace(string value)
    {
        var normalizedParts = value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', normalizedParts);
    }

    private static bool ContainsUnsupportedControlCharacter(string? value)
    {
        return value is not null && value.Any(character => char.IsControl(character) && !char.IsWhiteSpace(character));
    }
}