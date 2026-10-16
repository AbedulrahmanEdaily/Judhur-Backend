using Judhur.Application.Common.Behaviors;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging.Abstractions;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class CachingBehaviorTests
{
    private readonly FakeHybridCache _cache = new();
    private readonly CachingBehavior<CachedTestQuery, Result<string>> _behavior;
    private int _handlerCalls;

    public CachingBehaviorTests()
    {
        _behavior = new CachingBehavior<CachedTestQuery, Result<string>>(
            _cache,
            NullLogger<CachingBehavior<CachedTestQuery, Result<string>>>.Instance);
    }

    private Task<Result<string>> Succeed(string value)
    {
        _handlerCalls++;
        return TestResults.Success(value);
    }

    private Task<Result<string>> Fail()
    {
        _handlerCalls++;
        return TestResults.Failed();
    }

    [Fact]
    public async Task Handle_NotCacheable_CallsNextAndWritesNothing()
    {
        var result = await _behavior.Handle(new CachedTestQuery("k", Cacheable: false), _ => Succeed("fresh"), TestContext.Current.CancellationToken);

        Assert.Equal("fresh", result.Value);
        Assert.Equal(1, _handlerCalls);
        Assert.Empty(_cache.Writes);
    }

    [Fact]
    public async Task Handle_NotCacheable_IgnoresExistingEntry()
    {
        _cache.Seed<Result<string>>("k", "cached");

        var result = await _behavior.Handle(new CachedTestQuery("k", Cacheable: false), _ => Succeed("fresh"), TestContext.Current.CancellationToken);

        Assert.Equal("fresh", result.Value);
    }

    [Fact]
    public async Task Handle_Miss_RunsHandlerAndReturnsItsResult()
    {
        var result = await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("fresh"), TestContext.Current.CancellationToken);

        Assert.Equal("fresh", result.Value);
        Assert.Equal(1, _handlerCalls);
    }

    [Fact]
    public async Task Handle_Miss_StoresResultWithKeyTagsAndExpiration()
    {
        await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("fresh"), TestContext.Current.CancellationToken);

        var write = Assert.Single(_cache.Writes);
        Assert.Equal("k", write.Key);
        Assert.Equal(["things", "others"], write.Tags);
        Assert.Equal(TimeSpan.FromMinutes(5), write.Expiration);
    }

    [Fact]
    public async Task Handle_Hit_ReturnsCachedValueWithoutCallingHandler()
    {
        _cache.Seed<Result<string>>("k", "cached");

        var result = await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("fresh"), TestContext.Current.CancellationToken);

        Assert.Equal("cached", result.Value);
        Assert.Equal(0, _handlerCalls);
    }

    [Fact]
    public async Task Handle_SecondCallSameKey_RunsHandlerOnce()
    {
        await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("first"), TestContext.Current.CancellationToken);
        var second = await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("second"), TestContext.Current.CancellationToken);

        Assert.Equal("first", second.Value);
        Assert.Equal(1, _handlerCalls);
    }

    [Fact]
    public async Task Handle_HandlerFails_ReturnsFailure()
    {
        var result = await _behavior.Handle(new CachedTestQuery("k"), _ => Fail(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(TestResults.Failure.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_HandlerFails_DoesNotCacheFailure()
    {
        await _behavior.Handle(new CachedTestQuery("k"), _ => Fail(), TestContext.Current.CancellationToken);

        Assert.False(_cache.Contains("k"));
        Assert.Empty(_cache.Writes);
    }

    [Fact]
    public async Task Handle_AfterFailure_NextCallRunsHandlerAgain()
    {
        await _behavior.Handle(new CachedTestQuery("k"), _ => Fail(), TestContext.Current.CancellationToken);
        var result = await _behavior.Handle(new CachedTestQuery("k"), _ => Succeed("recovered"), TestContext.Current.CancellationToken);

        Assert.Equal("recovered", result.Value);
        Assert.Equal(2, _handlerCalls);
    }

    [Fact]
    public async Task Handle_HandlerThrows_PropagatesException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new CachedTestQuery("k"),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));
        Assert.False(_cache.Contains("k"));
    }
}
