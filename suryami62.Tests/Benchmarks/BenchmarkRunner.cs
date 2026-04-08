#region

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

#endregion

namespace suryami62.Tests.Benchmarks;

/// <summary>
///     Entry point untuk menjalankan semua benchmarks.
/// </summary>
public static class BenchmarkRunner
{
    /// <summary>
    ///     Menjalankan semua benchmarks dalam proyek.
    /// </summary>
    public static void RunAll()
    {
        var config = new ManualConfig()
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddLogger(ConsoleLogger.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default);

        Console.WriteLine("Running Caching Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<CachingBenchmarks>(config);

        Console.WriteLine("\nRunning Memory Cache Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<MemoryCacheBenchmarks>(config);

        Console.WriteLine("\nRunning Image Processing Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ImageProcessingBenchmarks>(config);
    }

    /// <summary>
    ///     Menjalankan benchmark caching saja.
    /// </summary>
    public static void RunCachingBenchmarks()
    {
        var config = new ManualConfig()
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddLogger(ConsoleLogger.Default);

        BenchmarkDotNet.Running.BenchmarkRunner.Run<CachingBenchmarks>(config);
    }

    /// <summary>
    ///     Menjalankan benchmark image processing saja.
    /// </summary>
    public static void RunImageBenchmarks()
    {
        var config = new ManualConfig()
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddLogger(ConsoleLogger.Default);

        BenchmarkDotNet.Running.BenchmarkRunner.Run<ImageProcessingBenchmarks>(config);
    }
}