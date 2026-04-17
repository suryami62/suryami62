#region

using suryami62.Services;

#endregion

namespace suryami62.Tests.Services;

public class MarkdownRendererTests
{
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_NullInput_ReturnsEmptyMarkupString()
    {
        var result = _renderer.Render(null);

        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Render_EmptyString_ReturnsEmptyMarkupString()
    {
        var result = _renderer.Render(string.Empty);

        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData("**bold text**", "<strong>bold text</strong>")]
    [InlineData("*italic text*", "<em>italic text</em>")]
    [InlineData("# Heading 1", "<h1>Heading 1</h1>")]
    [InlineData("## Heading 2", "<h2>Heading 2</h2>")]
    [InlineData("`code`", "<code>code</code>")]
    public void Render_MarkdownSyntax_ConvertsToExpectedHtml(string markdown, string expectedHtml)
    {
        var result = _renderer.Render(markdown);

        Assert.Contains(expectedHtml, result.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_LinkSyntax_ConvertsToAnchorTag()
    {
        var markdown = "[link text](https://example.com)";

        var result = _renderer.Render(markdown);

        Assert.Contains("<a", result.Value);
        Assert.Contains("href=\"https://example.com\"", result.Value);
        Assert.Contains(">link text</a>", result.Value);
    }

    [Fact]
    public void Render_ScriptTag_RemovesDangerousContent()
    {
        var markdown = "<script>alert('xss')</script>Hello";

        var result = _renderer.Render(markdown);

        Assert.DoesNotContain("<script>", result.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_IframeTag_RemovesDangerousContent()
    {
        var markdown = "<iframe src='evil.com'></iframe>Hello";

        var result = _renderer.Render(markdown);

        Assert.DoesNotContain("<iframe", result.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_TableSyntax_ConvertsToTableHtml()
    {
        var markdown = "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |";

        var result = _renderer.Render(markdown);

        Assert.Contains("<table>", result.Value);
        Assert.Contains("</table>", result.Value);
    }

    [Fact]
    public void Render_MultilineContent_PreservesParagraphs()
    {
        var markdown = "First paragraph.\n\nSecond paragraph.";

        var result = _renderer.Render(markdown);

        Assert.Contains("<p>", result.Value);
    }

    [Fact]
    public void Render_ComplexMarkdown_ReturnsValidHtml()
    {
        var markdown = @"# Title

This is **bold** and *italic* text.

- Item 1
- Item 2

[Link](https://example.com)";

        var result = _renderer.Render(markdown);

        Assert.Contains("<h1>", result.Value, StringComparison.Ordinal);
        Assert.Contains("<strong>", result.Value, StringComparison.Ordinal);
        Assert.Contains("<em>", result.Value, StringComparison.Ordinal);
        Assert.Contains("<a", result.Value, StringComparison.Ordinal);
    }
}