using Judhur.Application.Common.Behaviors;
using Judhur.Application.Features.Reports.Commands.ResolveReport;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class CacheInvalidationBehaviorTests
{
    private readonly FakeHybridCache _cache = new();
    private readonly RecordingLogger<CacheInvalidationBehavior<InvalidatingTestCommand, Result<string>>> _logger = new();
    private readonly CacheInvalidationBehavior<InvalidatingTestCommand, Result<string>> _behavior;

    public CacheInvalidationBehaviorTests()
    {
        _behavior = new CacheInvalidationBehavior<InvalidatingTestCommand, Result<string>>(_cache, _logger);
    }

    [Fact]
    public async Task Handle_Success_RemovesEveryTag()
    {
        await _behavior.Handle(new InvalidatingTestCommand(["properties", "sellers"]), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        Assert.Equal(["properties", "sellers"], _cache.RemovedTags);
    }

    [Fact]
    public async Task Handle_Success_ReturnsHandlerResult()
    {
        var result = await _behavior.Handle(new InvalidatingTestCommand(["properties"]), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task Handle_Failure_RemovesNothing()
    {
        var result = await _behavior.Handle(new InvalidatingTestCommand(["properties"]), _ => TestResults.Failed(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Handle_NoTags_RemovesNothing()
    {
        await _behavior.Handle(new InvalidatingTestCommand([]), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        Assert.Empty(_cache.RemovedTags);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Handle_NullTags_RemovesNothing()
    {
        await _behavior.Handle(new InvalidatingTestCommand(null!), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Handle_HandlerThrows_RemovesNothing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new InvalidatingTestCommand(["properties"]),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));

        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Handle_CacheThrows_StillReturnsSuccess()
    {
        _cache.RemoveByTagException = new InvalidOperationException("cache down");

        var result = await _behavior.Handle(new InvalidatingTestCommand(["properties"]), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task Handle_CacheThrows_LogsErrorWithException()
    {
        var failure = new InvalidOperationException("cache down");
        _cache.RemoveByTagException = failure;

        await _behavior.Handle(new InvalidatingTestCommand(["properties"]), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task Handle_Success_LogsInvalidatedTags()
    {
        await _behavior.Handle(new InvalidatingTestCommand(["properties"]), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(nameof(InvalidatingTestCommand), entry.Properties["RequestName"]);
    }

    [Fact]
    public async Task Handle_ResolveReportWithoutSuspension_RemovesNothing()
    {
        var cache = new FakeHybridCache();
        var behavior = new CacheInvalidationBehavior<ResolveReportCommand, Result<Updated>>(
            cache,
            NullLogger<CacheInvalidationBehavior<ResolveReportCommand, Result<Updated>>>.Instance);

        await behavior.Handle(
            new ResolveReportCommand(Guid.NewGuid(), null, SuspendProperty: false, SuspensionReason: null),
            _ => Task.FromResult<Result<Updated>>(Result.Updated),
            TestContext.Current.CancellationToken);

        Assert.Empty(cache.RemovedTags);
    }

    [Fact]
    public async Task Handle_ResolveReportWithSuspension_RemovesPropertiesTag()
    {
        var cache = new FakeHybridCache();
        var behavior = new CacheInvalidationBehavior<ResolveReportCommand, Result<Updated>>(
            cache,
            NullLogger<CacheInvalidationBehavior<ResolveReportCommand, Result<Updated>>>.Instance);

        await behavior.Handle(
            new ResolveReportCommand(Guid.NewGuid(), null, SuspendProperty: true, SuspensionReason: "مخالف"),
            _ => Task.FromResult<Result<Updated>>(Result.Updated),
            TestContext.Current.CancellationToken);

        Assert.Equal(["properties"], cache.RemovedTags);
    }
}
