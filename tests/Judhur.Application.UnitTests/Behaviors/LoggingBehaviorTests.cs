using Judhur.Application.Common.Behaviors;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.UnitTests.TestDoubles;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class LoggingBehaviorTests
{
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly RecordingLogger<PlainTestCommand> _logger = new();
    private readonly LoggingBehavior<PlainTestCommand> _behavior;

    public LoggingBehaviorTests()
    {
        _behavior = new LoggingBehavior<PlainTestCommand>(_user, _logger);
    }

    [Fact]
    public async Task Process_AuthenticatedUser_LogsRequestNameAndUserId()
    {
        var userId = Guid.NewGuid();
        _user.Id.Returns(userId);

        await _behavior.Process(new PlainTestCommand("x"), TestContext.Current.CancellationToken);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal(nameof(PlainTestCommand), entry.Properties["RequestName"]);
        Assert.Equal(userId, entry.Properties["UserId"]);
    }

    [Fact]
    public async Task Process_AnonymousUser_LogsNullUserId()
    {
        _user.Id.Returns((Guid?)null);

        await _behavior.Process(new PlainTestCommand("x"), TestContext.Current.CancellationToken);

        var entry = Assert.Single(_logger.Entries);
        Assert.Null(entry.Properties["UserId"]);
    }
}
