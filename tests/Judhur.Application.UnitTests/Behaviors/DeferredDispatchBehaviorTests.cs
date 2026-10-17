using Judhur.Application.Common.Behaviors;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using NSubstitute;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class DeferredDispatchBehaviorTests
{
    private readonly IDeferredDispatcher _dispatcher = Substitute.For<IDeferredDispatcher>();
    private readonly DeferredDispatchBehavior<PlainTestCommand, Result<string>> _behavior;

    public DeferredDispatchBehaviorTests()
    {
        _behavior = new DeferredDispatchBehavior<PlainTestCommand, Result<string>>(_dispatcher);
    }

    [Fact]
    public async Task Handle_Success_DispatchesDeferredWork()
    {
        await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_DispatchesWithNonCancellableToken()
    {
        using var cts = new CancellationTokenSource();

        await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Success(), cts.Token);

        await _dispatcher.Received(1).DispatchAsync(Arg.Is<CancellationToken>(t => !t.CanBeCanceled));
    }

    [Fact]
    public async Task Handle_Success_ReturnsHandlerResult()
    {
        var result = await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task Handle_Failure_DoesNotDispatch()
    {
        var result = await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Failed(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_HandlerThrows_DoesNotDispatch()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new PlainTestCommand("x"),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Success_RunsWorkDeferredByHandler()
    {
        var dispatcher = new RecordingDeferredDispatcher();
        var behavior = new DeferredDispatchBehavior<PlainTestCommand, Result<string>>(dispatcher);
        var ran = false;

        await behavior.Handle(
            new PlainTestCommand("x"),
            _ =>
            {
                dispatcher.Defer(_ => { ran = true; return Task.CompletedTask; });
                return TestResults.Success();
            },
            TestContext.Current.CancellationToken);

        Assert.True(ran);
        Assert.Equal(0, dispatcher.DeferredCount);
    }

    [Fact]
    public async Task Handle_Failure_DoesNotRunWorkDeferredByHandler()
    {
        var dispatcher = new RecordingDeferredDispatcher();
        var behavior = new DeferredDispatchBehavior<PlainTestCommand, Result<string>>(dispatcher);
        var ran = false;

        await behavior.Handle(
            new PlainTestCommand("x"),
            _ =>
            {
                dispatcher.Defer(_ => { ran = true; return Task.CompletedTask; });
                return TestResults.Failed();
            },
            TestContext.Current.CancellationToken);

        Assert.False(ran);
    }
}
