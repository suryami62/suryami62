// ============================================================================
// MARKDOWN RENDERER SERVICE
// ============================================================================
// This service converts Markdown text into safe HTML for display in the browser.
//
// WHAT IS MARKDOWN?
// Markdown is a simple way to format text using plain text characters.
// Examples:
//   **bold** → <strong>bold</strong>
//   # Heading → <h1>Heading</h1>
//   [link](url) → <a href="url">link</a>
//
// WHAT IS SANITIZATION?
// Sanitization removes dangerous HTML (like <script> tags) that could
// be used for attacks. We use HtmlSanitizer library for this.
// ============================================================================

#region

using Ganss.Xss; // For sanitizing HTML (removing dangerous tags)
using Markdig; // For converting Markdown to HTML
using Microsoft.AspNetCore.Components; // For MarkupString type

#endregion

namespace suryami62.Services;

/// <summary>
///     Converts Markdown text into safe HTML that can be displayed in the browser.
/// </summary>
internal sealed class MarkdownRenderer
{
    // HtmlSanitizer removes dangerous HTML tags and attributes
    // This prevents security attacks like XSS (Cross-Site Scripting)
    private readonly HtmlSanitizer _htmlSanitizer = new();

    // MarkdownPipeline is the configuration for how to convert Markdown
    // .UseAdvancedExtensions() enables extra features like tables, footnotes
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // Enable tables, task lists, footnotes, etc.
        .Build(); // Create the pipeline

    /// <summary>
    ///     Converts Markdown text to safe HTML.
    /// </summary>
    /// <param name="markdown">The Markdown text (can be null if empty).</param>
    /// <returns>A MarkupString containing safe HTML ready for display.</returns>
    public MarkupString Render(string? markdown)
    {
        // Step 1: Handle null input - if null, use empty string instead
        var safeMarkdown = markdown ?? string.Empty;

        // Step 2: Convert Markdown to HTML using Markdig
        // Example: "**hello**" → "<strong>hello</strong>"
        var html = Markdown.ToHtml(safeMarkdown, _pipeline);

        // Step 3: Sanitize the HTML to remove dangerous content
        // This removes <script>, <iframe>, and other potentially harmful tags
        var safeHtml = _htmlSanitizer.Sanitize(html);

        // Step 4: Return as MarkupString (tells Blazor this is raw HTML)
        return new MarkupString(safeHtml);
    }
}