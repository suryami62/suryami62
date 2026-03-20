#region

using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

#endregion

namespace suryami62.Services;

internal sealed class MarkdownRenderer
{
    private readonly HtmlSanitizer _htmlSanitizer = new();

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public MarkupString Render(string? markdown)
    {
        var html = Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
        var sanitizedHtml = _htmlSanitizer.Sanitize(html);
        return new MarkupString(sanitizedHtml);
    }
}