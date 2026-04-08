#region

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using suryami62.Application.Persistence;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Tests.Benchmarks;

/// <summary>
///     Benchmarks untuk membandingkan performa SettingsRepository dengan dan tanpa caching.
/// </summary>
[SimpleJob(RunStrategy.Throughput, 1, 3, 5)]
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class CachingBenchmarks
{
    private CachedSettingsRepository _cachedRepo = null!;
    private ISettingsRepository _directRepo = null!;
    private IMemoryCache _memoryCache = null!;
    private Mock<ISettingsRepository> _mockInner = null!;

    [GlobalSetup]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockInner = new Mock<ISettingsRepository>();

        // Setup mock to simulate DB latency (1ms delay per call)
        _mockInner.Setup(m => m.GetValueAsync("test-key", It.IsAny<CancellationToken>()))
            .Returns(async (string key, CancellationToken ct) =>
            {
                await Task.Delay(1, ct); // Simulate 1ms DB latency
                return "test-value";
            });

        _mockInner.Setup(m => m.GetValuesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyCollection<string> keys, CancellationToken ct) =>
            {
                await Task.Delay(1, ct); // Simulate 1ms DB latency
                return keys.ToDictionary(k => k, k => "test-value");
            });

        _cachedRepo = new CachedSettingsRepository(_mockInner.Object, _memoryCache,
            NullLogger<CachedSettingsRepository>.Instance);
        _directRepo = _mockInner.Object;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryCache.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Direct repository call (no caching)")]
    public async Task<string?> GetValue_Direct()
    {
        return await _directRepo.GetValueAsync("test-key");
    }

    [Benchmark(Description = "Cached repository (warm cache - memory hit)")]
    public async Task<string?> GetValue_Cached_Warm()
    {
        // First call to populate cache
        _ = await _cachedRepo.GetValueAsync("test-key");
        // Second call should hit cache
        return await _cachedRepo.GetValueAsync("test-key");
    }

    [Benchmark(Description = "Cached repository (cold cache - DB call + populate)")]
    public async Task<string?> GetValue_Cached_Cold()
    {
        // Clear cache before each iteration
        _memoryCache.Remove("setting:test-key");
        return await _cachedRepo.GetValueAsync("test-key");
    }

    [Benchmark(Description = "Batch get - 5 keys (direct)")]
    public async Task<IReadOnlyDictionary<string, string>> GetValues_Batch_Direct()
    {
        var keys = new[] { "key1", "key2", "key3", "key4", "key5" };
        return await _directRepo.GetValuesAsync(keys);
    }

    [Benchmark(Description = "Batch get - 5 keys (cached)")]
    public async Task<IReadOnlyDictionary<string, string>> GetValues_Batch_Cached()
    {
        // Populate cache first
        var keys = new[] { "key1", "key2", "key3", "key4", "key5" };
        _ = await _cachedRepo.GetValuesAsync(keys);

        // Clear one key to simulate partial cache miss
        _memoryCache.Remove("setting:key3");

        // This call should hit cache for 4 keys, DB for 1
        return await _cachedRepo.GetValuesAsync(keys);
    }
}

/// <summary>
///     Benchmark untuk memory cache operations.
/// </summary>
[SimpleJob(RunStrategy.Throughput, 1, 3, 5)]
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class MemoryCacheBenchmarks
{
    private IMemoryCache _memoryCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Pre-populate cache
        for (var i = 0; i < 100; i++) _memoryCache.Set($"key:{i}", $"value{i}", TimeSpan.FromMinutes(5));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryCache.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Cache read (hit)")]
    public string? CacheRead_Hit()
    {
        return _memoryCache.TryGetValue("key:50", out string? value) ? value : null;
    }

    [Benchmark(Description = "Cache write")]
    public void CacheWrite()
    {
        _memoryCache.Set($"key:{Guid.NewGuid()}", "new-value", TimeSpan.FromMinutes(5));
    }

    [Benchmark(Description = "Cache read (miss)")]
    public bool CacheRead_Miss()
    {
        return _memoryCache.TryGetValue("key:nonexistent", out string? _);
    }
}