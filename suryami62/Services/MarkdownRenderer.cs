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
        var safeMarkdown = markdown ?? string.Empty;

        var html = Markdown.ToHtml(safeMarkdown, _pipeline);

        var safeHtml = _htmlSanitizer.Sanitize(html);

        return new MarkupString(safeHtml);
    }
}