using Judhur.Application.Common.Behaviors;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class PerformanceBehaviorTests
{
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly RecordingLogger<PlainTestCommand> _logger = new();
    private readonly PerformanceBehavior<PlainTestCommand, Result<string>> _behavior;

    public PerformanceBehaviorTests()
    {
        _behavior = new PerformanceBehavior<PlainTestCommand, Result<string>>(_user, _logger);
    }

    [Fact]
    public async Task Handle_FastRequest_ReturnsResultWithoutWarning()
    {
        var result = await _behavior.Handle(new PlainTestCommand("x"), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.Equal("done", result.Value);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Handle_SlowRequest_LogsWarningWithRequestNameAndUser()
    {
        var userId = Guid.NewGuid();
        _user.Id.Returns(userId);

        await _behavior.Handle(
            new PlainTestCommand("x"),
            async ct =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(600), ct);
                return "done";
            },
            TestContext.Current.CancellationToken);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(nameof(PlainTestCommand), entry.Properties["RequestName"]);
        Assert.Equal(userId, entry.Properties["UserId"]);
        Assert.True((long)entry.Properties["ElapsedMilliseconds"]! > 500);
    }

    [Fact]
    public async Task Handle_HandlerThrows_PropagatesException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new PlainTestCommand("x"),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));
    }
}
