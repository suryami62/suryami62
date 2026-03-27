#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Domain.Tests.Models;

public class BlogPostTests
{
    [Fact]
    public void Constructor_WhenCreated_SetsExpectedDefaultValues()
    {
        var post = new BlogPost();

        Assert.Equal(string.Empty, post.Title);
        Assert.Equal(string.Empty, post.Slug);
        Assert.Equal(string.Empty, post.Content);
        Assert.Equal(string.Empty, post.Label);
        Assert.Equal(string.Empty, post.Summary);
        Assert.False(post.IsPublished);
        Assert.Null(post.ImageUrl);
    }

    [Fact]
    public void Date_WhenCreated_UsesCurrentUtcTime()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var post = new BlogPost();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(post.Date, before, after);
    }

    [Theory]
    [InlineData("My First Blog Post")]
    [InlineData("Another Title")]
    [InlineData("A")]
    public void Title_WhenAssigned_PreservesValue(string title)
    {
        var post = new BlogPost { Title = title };
        Assert.Equal(title, post.Title);
    }

    [Theory]
    [InlineData("my-first-blog-post")]
    [InlineData("another-title")]
    [InlineData("post-123")]
    public void Slug_WhenAssigned_PreservesValue(string slug)
    {
        var post = new BlogPost { Slug = slug };
        Assert.Equal(slug, post.Slug);
    }

    [Fact]
    public void IsPublished_WhenSetToTrue_ReturnsTrue()
    {
        var post = new BlogPost { IsPublished = true };
        Assert.True(post.IsPublished);
    }

    [Fact]
    public void ImageUrl_WhenSetToAbsoluteUri_ReturnsUri()
    {
        var uri = new Uri("https://example.com/image.png");
        var post = new BlogPost { ImageUrl = uri };

        Assert.Equal(uri, post.ImageUrl);
    }

    [Fact]
    public void Id_WhenAssigned_PreservesValue()
    {
        var post = new BlogPost { Id = 42 };
        Assert.Equal(42, post.Id);
    }

    [Theory]
    [InlineData("Technology")]
    [InlineData("Engineering")]
    public void Label_WhenAssigned_PreservesValue(string label)
    {
        var post = new BlogPost { Label = label };
        Assert.Equal(label, post.Label);
    }

    [Fact]
    public void Summary_WhenAssigned_PreservesValue()
    {
        const string summary = "A brief summary of the post.";
        var post = new BlogPost { Summary = summary };

        Assert.Equal(summary, post.Summary);
    }

    [Fact]
    public void Date_WhenAssigned_PreservesValue()
    {
        var date = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var post = new BlogPost { Date = date };

        Assert.Equal(date, post.Date);
    }
}