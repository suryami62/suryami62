#region

using suryami62.Domain.Models;

#endregion

namespace suryami62.Domain.Tests.Models;

public class ProjectTests
{
    [Fact]
    public void Constructor_WhenCreated_SetsExpectedDefaultValues()
    {
        var project = new Project();

        Assert.Equal(string.Empty, project.Title);
        Assert.Equal(string.Empty, project.Description);
        Assert.Equal(string.Empty, project.Tags);
        Assert.Null(project.RepoUrl);
        Assert.Null(project.DemoUrl);
        Assert.Null(project.ImageUrl);
    }

    [Theory]
    [InlineData("My Portfolio Project")]
    [InlineData("Another Project")]
    [InlineData("X")]
    public void Title_WhenAssigned_PreservesValue(string title)
    {
        var project = new Project { Title = title };
        Assert.Equal(title, project.Title);
    }

    [Fact]
    public void Description_WhenAssigned_PreservesValue()
    {
        const string description = "A full-stack web application built with .NET and Blazor.";
        var project = new Project { Description = description };

        Assert.Equal(description, project.Description);
    }

    [Fact]
    public void Tags_WhenAssigned_PreservesValue()
    {
        const string tags = "C#,.NET,Blazor,PostgreSQL";
        var project = new Project { Tags = tags };

        Assert.Equal(tags, project.Tags);
    }

    [Fact]
    public void RepoUrl_WhenSetToAbsoluteUri_ReturnsUri()
    {
        var uri = new Uri("https://github.com/example/repo");
        var project = new Project { RepoUrl = uri };

        Assert.Equal(uri, project.RepoUrl);
    }

    [Fact]
    public void DemoUrl_WhenSetToAbsoluteUri_ReturnsUri()
    {
        var uri = new Uri("https://example.com/demo");
        var project = new Project { DemoUrl = uri };

        Assert.Equal(uri, project.DemoUrl);
    }

    [Fact]
    public void ImageUrl_WhenSetToAbsoluteUri_ReturnsUri()
    {
        var uri = new Uri("https://example.com/preview.png");
        var project = new Project { ImageUrl = uri };

        Assert.Equal(uri, project.ImageUrl);
    }

    [Fact]
    public void Id_WhenAssigned_PreservesValue()
    {
        var project = new Project { Id = 7 };
        Assert.Equal(7, project.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    public void DisplayOrder_WhenAssigned_PreservesValue(int order)
    {
        var project = new Project { DisplayOrder = order };
        Assert.Equal(order, project.DisplayOrder);
    }
}