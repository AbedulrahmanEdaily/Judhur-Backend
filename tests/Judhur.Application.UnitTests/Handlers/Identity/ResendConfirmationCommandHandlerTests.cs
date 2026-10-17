using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Commands.ResendConfirmation;
using Judhur.Application.Features.Identity.Events;
using Judhur.Tests.Common;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class ResendConfirmationCommandHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ResendConfirmationCommandHandler _handler;

    public ResendConfirmationCommandHandlerTests()
    {
        _handler = new ResendConfirmationCommandHandler(_identity, _publisher, NullLogger<ResendConfirmationCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_UnconfirmedUser_PublishesUserRegistered()
    {
        var userId = Guid.NewGuid();
        _identity.FindUnconfirmedUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(userId);

        var result = await _handler.Handle(new ResendConfirmationCommand("user@example.com"), TestContext.Current.CancellationToken);

        result.ThrowIfError();
        await _publisher.Received(1).Publish(Arg.Is<UserRegistered>(e => e.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownOrConfirmedEmail_ReturnsSuccessWithoutPublishing()
    {
        _identity.FindUnconfirmedUserIdAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _handler.Handle(new ResendConfirmationCommand("ghost@example.com"), TestContext.Current.CancellationToken);

        result.ThrowIfError();
        Assert.Empty(_publisher.ReceivedCalls());
    }
}
