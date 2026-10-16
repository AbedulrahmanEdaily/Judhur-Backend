using Judhur.Application.Common.Behaviors;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class UnhandledExceptionBehaviorTests
{
    private readonly RecordingLogger<PlainTestCommand> _logger = new();
    private readonly UnhandledExceptionBehavior<PlainTestCommand, Result<string>> _behavior;

    public UnhandledExceptionBehaviorTests()
    {
        _behavior = new UnhandledExceptionBehavior<PlainTestCommand, Result<string>>(_logger);
    }

    [Fact]
    public async Task Handle_NoException_ReturnsResultWithoutLogging()
    {
        var result = await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.Equal("done", result.Value);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Handle_FailureResult_ReturnsItWithoutLogging()
    {
        var result = await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Failed(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Handle_HandlerThrows_RethrowsSameException()
    {
        var failure = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new PlainTestCommand("x"),
            _ => throw failure,
            TestContext.Current.CancellationToken));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task Handle_HandlerThrows_LogsErrorWithRequestName()
    {
        var failure = new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new PlainTestCommand("x"),
            _ => throw failure,
            TestContext.Current.CancellationToken));

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
        Assert.Equal(nameof(PlainTestCommand), entry.Properties["RequestName"]);
    }
}
