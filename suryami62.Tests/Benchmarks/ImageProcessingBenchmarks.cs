#region

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

#endregion

namespace suryami62.Tests.Benchmarks;

/// <summary>
///     Benchmarks untuk image processing dan format comparison.
/// </summary>
[SimpleJob(RunStrategy.Throughput, 1, 2, 3)]
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class ImageProcessingBenchmarks
{
    private byte[] _largeImageBytes = null!;
    private byte[] _mediumImageBytes = null!;
    private string _outputPath = null!;
    private byte[] _smallImageBytes = null!;

    [Params(1920, 1200, 800)] public int TargetWidth { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create test images of different sizes
        _largeImageBytes = CreateTestImage(1920, 1080, new Rgba32(255, 0, 0)); // Red
        _mediumImageBytes = CreateTestImage(1200, 800, new Rgba32(0, 255, 0)); // Green
        _smallImageBytes = CreateTestImage(800, 600, new Rgba32(0, 0, 255)); // Blue

        _outputPath = Path.Combine(Path.GetTempPath(), $"benchmark_output_{Guid.NewGuid()}.webp");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_outputPath)) File.Delete(_outputPath);
    }

    private static byte[] CreateTestImage(int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(x => x.BackgroundColor(color));

        // Add some visual complexity with noise-like pattern to simulate real image content
        var random = new Random(42); // Seeded for reproducibility
        for (var i = 0; i < 1000; i++)
        {
            var x = random.Next(width);
            var y = random.Next(height);
            var pixelColor = new Rgba32(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256),
                200);
            image[x, y] = pixelColor;
        }

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    [Benchmark(Baseline = true, Description = "Load and resize PNG to WebP (original format)")]
    public async Task<long> ProcessAndSave_WebP()
    {
        using var stream = new MemoryStream(_largeImageBytes);
        using var image = await Image.LoadAsync(stream).ConfigureAwait(false);

        // Resize
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(TargetWidth, TargetWidth)
        }));

        // Save as WebP
        await image.SaveAsync(_outputPath, new WebpEncoder { Quality = 85 }).ConfigureAwait(false);

        return new FileInfo(_outputPath).Length;
    }

    [Benchmark(Description = "Load and resize PNG to JPEG")]
    public async Task<long> ProcessAndSave_Jpeg()
    {
        var jpegPath = Path.ChangeExtension(_outputPath, ".jpg");
        using var stream = new MemoryStream(_largeImageBytes);
        using var image = await Image.LoadAsync(stream).ConfigureAwait(false);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(TargetWidth, TargetWidth)
        }));

        await image.SaveAsync(jpegPath, new JpegEncoder { Quality = 85 }).ConfigureAwait(false);

        var size = new FileInfo(jpegPath).Length;
        File.Delete(jpegPath);
        return size;
    }

    [Benchmark(Description = "Load and resize PNG to PNG")]
    public async Task<long> ProcessAndSave_Png()
    {
        var pngPath = Path.ChangeExtension(_outputPath, ".png");
        using var stream = new MemoryStream(_largeImageBytes);
        using var image = await Image.LoadAsync(stream).ConfigureAwait(false);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(TargetWidth, TargetWidth)
        }));

        await image.SaveAsync(pngPath, new PngEncoder()).ConfigureAwait(false);

        var size = new FileInfo(pngPath).Length;
        File.Delete(pngPath);
        return size;
    }

    [Benchmark(Description = "Resize with Lanczos3 resampler")]
    public async Task<long> Resize_Lanczos3()
    {
        using var stream = new MemoryStream(_largeImageBytes);
        using var image = await Image.LoadAsync(stream).ConfigureAwait(false);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(TargetWidth, TargetWidth),
            Sampler = KnownResamplers.Lanczos3
        }));

        await image.SaveAsync(_outputPath, new WebpEncoder { Quality = 85 }).ConfigureAwait(false);
        return new FileInfo(_outputPath).Length;
    }

    [Benchmark(Description = "Resize with Bicubic resampler")]
    public async Task<long> Resize_Bicubic()
    {
        using var stream = new MemoryStream(_largeImageBytes);
        using var image = await Image.LoadAsync(stream).ConfigureAwait(false);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(TargetWidth, TargetWidth),
            Sampler = KnownResamplers.Bicubic
        }));

        await image.SaveAsync(_outputPath, new WebpEncoder { Quality = 85 }).ConfigureAwait(false);
        return new FileInfo(_outputPath).Length;
    }
}