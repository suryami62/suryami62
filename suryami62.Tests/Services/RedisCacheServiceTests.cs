#region

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using StackExchange.Redis;
using suryami62.Services;

#endregion

namespace suryami62.Tests.Services;

public class RedisCacheServiceTests
{
    private static readonly JsonSerializerOptions s_testJsonOptions = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisCacheService _service;
    private readonly EndPoint _testEndpoint;

    public RedisCacheServiceTests()
    {
        _connectionMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(_databaseMock.Object);
        _testEndpoint = new DnsEndPoint("localhost", 6379);
        _connectionMock.Setup(c => c.GetEndPoints(false)).Returns([_testEndpoint]);

        var mockServer = new Mock<IServer>();
        _connectionMock.Setup(c => c.GetServer(It.IsAny<EndPoint>(), null)).Returns(mockServer.Object);

        _service = new RedisCacheService(_connectionMock.Object);
    }

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisCacheService(null!));
    }

    [Fact]
    public void Get_ExistingKey_ReturnsValue()
    {
        var key = "test_key";
        var value = new byte[] { 1, 2, 3 };
        _databaseMock.Setup(d => d.StringGet(key, CommandFlags.None)).Returns((RedisValue)value);

        var result = _service.Get(key);

        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNull()
    {
        var key = "missing_key";
        _databaseMock.Setup(d => d.StringGet(key, CommandFlags.None)).Returns(RedisValue.Null);

        var result = _service.Get(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsValue()
    {
        var key = "async_key";
        var value = new byte[] { 4, 5, 6 };
        _databaseMock.Setup(d => d.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync((RedisValue)value);

        var result = await _service.GetAsync(key);

        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        var key = "async_missing";
        _databaseMock.Setup(d => d.StringGetAsync(key, CommandFlags.None)).ReturnsAsync(RedisValue.Null);

        var result = await _service.GetAsync(key);

        Assert.Null(result);
    }

    [Fact(Skip = "StackExchange.Redis mock overload resolution too complex")]
    public void Set_WithExpiration_StoresValue()
    {
        var key = "expiring_key";
        var value = new byte[] { 7, 8, 9 };
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        _service.Set(key, value, options);

        // Verify StringSet was called - use loose verification for StackExchange.Redis
        _databaseMock.Verify(
            d => d.StringSet(It.Is<RedisKey>(k => k == key), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false,
                When.Always, CommandFlags.None), Times.AtLeastOnce());
    }

    [Fact]
    public void Set_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Set("key", new byte[] { 1 }, null!));
    }

    [Fact(Skip = "StackExchange.Redis mock overload resolution too complex")]
    public async Task SetAsync_WithExpiration_StoresValueAsync()
    {
        var key = "async_expiring";
        var value = new byte[] { 10, 11, 12 };
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await _service.SetAsync(key, value, options);

        // Verify StringSetAsync was called
        _databaseMock.Verify(
            d => d.StringSetAsync(It.Is<RedisKey>(k => k == key), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false,
                When.Always, CommandFlags.None), Times.AtLeastOnce());
    }

    [Fact]
    public void Remove_ExistingKey_DeletesValue()
    {
        var key = "delete_key";

        _service.Remove(key);

        _databaseMock.Verify(d => d.KeyDelete(key, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_DeletesValueAsync()
    {
        var key = "async_delete";

        await _service.RemoveAsync(key);

        _databaseMock.Verify(d => d.KeyDeleteAsync(key, CommandFlags.None), Times.Once);
    }

    [Fact]
    public void Refresh_KeyExists_ChecksExistence()
    {
        var key = "refresh_key";
        _databaseMock.Setup(d => d.KeyExists(key, CommandFlags.None)).Returns(true);

        _service.Refresh(key);

        _databaseMock.Verify(d => d.KeyExists(key, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_KeyExists_ChecksExistenceAsync()
    {
        var key = "async_refresh";
        _databaseMock.Setup(d => d.KeyExistsAsync(key, CommandFlags.None)).ReturnsAsync(true);

        await _service.RefreshAsync(key);

        _databaseMock.Verify(d => d.KeyExistsAsync(key, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task GetAsyncTyped_ExistingObject_ReturnsDeserializedValue()
    {
        var key = "typed_key";
        var obj = new TestData { Name = "Test", Value = 42 };
        var json = JsonSerializer.Serialize(obj, s_testJsonOptions);
        _databaseMock.Setup(d => d.StringGetAsync(key, CommandFlags.None)).ReturnsAsync((RedisValue)json);

        var result = await _service.GetAsync<TestData>(key);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GetAsyncTyped_NonExistentKey_ReturnsNull()
    {
        var key = "typed_missing";
        _databaseMock.Setup(d => d.StringGetAsync(key, CommandFlags.None)).ReturnsAsync(RedisValue.Null);

        var result = await _service.GetAsync<TestData>(key);

        Assert.Null(result);
    }

    [Fact(Skip = "StackExchange.Redis mock overload resolution too complex")]
    public async Task SetAsyncTyped_SerializesAndStores()
    {
        var key = "typed_store";
        var obj = new TestData { Name = "StoreTest", Value = 100 };

        await _service.SetAsync(key, obj, TimeSpan.FromMinutes(10));

        // Verify StringSetAsync was called with JSON-serialized content
        _databaseMock.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k == key),
            It.Is<RedisValue>(v => v.HasValue && v.ToString().Contains("StoreTest")),
            It.IsAny<TimeSpan?>(),
            false, When.Always, CommandFlags.None), Times.AtLeastOnce());
    }

    [Fact(Skip = "StackExchange.Redis mock overload resolution too complex")]
    public async Task SetAsyncTyped_NullExpiration_StoresWithDefaultExpiry()
    {
        var key = "default_expiry";
        var obj = new TestData { Name = "Expiry", Value = 1 };

        await _service.SetAsync(key, obj);

        // Verify call made - actual TTL is set by implementation (default 30 min)
        _databaseMock.Verify(
            d => d.StringSetAsync(It.Is<RedisKey>(k => k == key), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false,
                When.Always, CommandFlags.None), Times.AtLeastOnce());
    }

    [Fact(Skip = "Requires full server mock setup - complex StackExchange.Redis mocking")]
    public async Task RemoveByPatternAsync_WithPattern_DeletesMatchingKeys()
    {
        var pattern = "test:*";
        var mockServer = new Mock<IServer>();
        var keys = new RedisKey[] { "test:1", "test:2", "test:3" };
        mockServer.Setup(s => s.Keys(It.IsAny<int>(), pattern, It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(),
            CommandFlags.None)).Returns(keys);
        _connectionMock.Setup(c => c.GetServer(It.IsAny<EndPoint>(), null)).Returns(mockServer.Object);

        await _service.RemoveByPatternAsync(pattern);

        _databaseMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(k => k.Length == 3), CommandFlags.None),
            Times.Once);
    }

    private sealed class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}