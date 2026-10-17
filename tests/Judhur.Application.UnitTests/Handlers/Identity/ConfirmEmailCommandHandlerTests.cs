using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Commands.ConfirmEmail;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;
using Judhur.Tests.Common;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class ConfirmEmailCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _handler = new ConfirmEmailCommandHandler(_identity, NullLogger<ConfirmEmailCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsSuccess()
    {
        _identity.ConfirmEmailAsync(_userId, "token", Arg.Any<CancellationToken>()).Returns(ResultTask.Of(Result.Success));

        var result = await _handler.Handle(new ConfirmEmailCommand(_userId, "token"), TestContext.Current.CancellationToken);

        result.ThrowIfError();
        await _identity.Received(1).ConfirmEmailAsync(_userId, "token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdentityFails_ReturnsError()
    {
        _identity.ConfirmEmailAsync(_userId, "token", Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<Success>(ApplicationError.UserNotFound));

        var result = await _handler.Handle(new ConfirmEmailCommand(_userId, "token"), TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.UserNotFound.Code, result.TopError.Code);
    }
}
