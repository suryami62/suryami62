#region

using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

#endregion

namespace suryami62.Services;

/// <summary>
///     Converts markdown content into sanitized HTML for safe rendering in public and admin Blazor pages.
/// </summary>
/// <remarks>
///     The renderer applies the shared Markdig pipeline first and then sanitizes the generated HTML so rich text
///     content can be displayed without allowing unsafe markup into the component tree.
/// </remarks>
internal sealed class MarkdownRenderer
{
    private readonly HtmlSanitizer _htmlSanitizer = new();

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    ///     Renders persisted markdown text as sanitized markup ready for Blazor output.
    /// </summary>
    /// <param name="markdown">The markdown content to render.</param>
    /// <returns>A <see cref="MarkupString" /> containing sanitized HTML.</returns>
    public MarkupString Render(string? markdown)
    {
        var html = Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
        var sanitizedHtml = _htmlSanitizer.Sanitize(html);
        return new MarkupString(sanitizedHtml);
    }
}