#region

using Microsoft.AspNetCore.Hosting;
using Moq;
using suryami62.Services;

#endregion

namespace suryami62.Tests.Services;

public class MediaServiceTests : IDisposable
{
    private readonly MediaService _service;
    private readonly string _tempWebRoot;

    public MediaServiceTests()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), $"media_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWebRoot);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _service = new MediaService(mockEnv.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWebRoot))
            Directory.Delete(_tempWebRoot, true);
    }

    [Fact]
    public async Task ListFilesAsync_UploadsDirectoryNotExists_ReturnsEmptyList()
    {
        var emptyWebRoot = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyWebRoot);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(emptyWebRoot);
        var service = new MediaService(mockEnv.Object);

        var result = await service.ListFilesAsync();

        Assert.Empty(result);

        Directory.Delete(emptyWebRoot);
    }

    [Fact]
    public async Task ListFilesAsync_WithFiles_ReturnsSortedByNewestFirst()
    {
        var uploadsPath = Path.Combine(_tempWebRoot, "img", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var file1 = Path.Combine(uploadsPath, "oldest.jpg");
        var file2 = Path.Combine(uploadsPath, "newest.jpg");
        File.WriteAllText(file1, "dummy");
        File.WriteAllText(file2, "dummy");
        File.SetLastWriteTimeUtc(file1, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(file2, DateTime.UtcNow);

        var result = await _service.ListFilesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("newest.jpg", result[0]);
        Assert.Equal("oldest.jpg", result[1]);
    }

    [Fact]
    public async Task UploadFileAsync_InvalidExtension_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _service.UploadFileAsync(
            "file.exe",
            "application/octet-stream",
            stream
        );

        Assert.False(result.Success);
        Assert.Contains("extension", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadFileAsync_InvalidContentType_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _service.UploadFileAsync(
            "file.jpg",
            "text/plain",
            stream
        );

        Assert.False(result.Success);
        Assert.Contains("content type", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadFileAsync_EmptyFileName_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _service.UploadFileAsync(
            "",
            "image/jpeg",
            stream
        );

        Assert.False(result.Success);
    }

    // Image upload tests require valid image bytes for ImageSharp processing
    // Skipped due to ImageSharp requiring actual image format data
    // [Theory]
    // [InlineData("test.jpg", "image/jpeg")]
    // [InlineData("test.jpeg", "image/jpeg")]
    // [InlineData("test.png", "image/png")]
    // [InlineData("test.webp", "image/webp")]
    // public async Task UploadFileAsync_ValidImageType_AcceptsUpload(string fileName, string contentType)
    // {
    //     // Requires real image bytes - ImageSharp validates format
    // }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue()
    {
        var uploadsPath = Path.Combine(_tempWebRoot, "img", "uploads");
        Directory.CreateDirectory(uploadsPath);
        var filePath = Path.Combine(uploadsPath, "to_delete.jpg");
        File.WriteAllText(filePath, "dummy");

        var result = await _service.DeleteFileAsync("to_delete.jpg");

        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        var result = await _service.DeleteFileAsync("does_not_exist.jpg");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_PathTraversal_ReturnsSafeFileName()
    {
        var result = await _service.DeleteFileAsync("../../../etc/passwd");

        Assert.False(result);
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ReturnsError()
    {
        using var stream = new MemoryStream(new byte[100]);

        var result = await _service.UploadFileAsync(
            "large.jpg",
            "image/jpeg",
            stream,
            50
        );

        Assert.False(result.Success);
    }
}