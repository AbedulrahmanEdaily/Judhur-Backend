using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.Features.Identity.Events;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Tests.Common;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class SendResetPasswordCodeCommandHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly RecordingDeferredDispatcher _dispatcher = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly SendResetPasswordCodeCommandHandler _handler;

    public SendResetPasswordCodeCommandHandlerTests()
    {
        _handler = new SendResetPasswordCodeCommandHandler(_identity, _dispatcher, _publisher, NullLogger<SendResetPasswordCodeCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_EligibleUser_DefersPasswordResetRequested()
    {
        var code = new PasswordResetCode(Guid.NewGuid(), "123456");
        _identity.SendResetPasswordCodeAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(code);

        var result = await _handler.Handle(new SendResetPasswordCodeCommand("user@example.com"), TestContext.Current.CancellationToken);

        result.ThrowIfError();
        Assert.Equal(1, _dispatcher.DeferredCount);
        Assert.Empty(_publisher.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_EligibleUser_DeferredWorkPublishesUserAndCode()
    {
        var code = new PasswordResetCode(Guid.NewGuid(), "123456");
        _identity.SendResetPasswordCodeAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(code);

        await _handler.Handle(new SendResetPasswordCodeCommand("user@example.com"), TestContext.Current.CancellationToken);
        await _dispatcher.DispatchAsync(TestContext.Current.CancellationToken);

        await _publisher.Received(1).Publish(
            Arg.Is<PasswordResetRequested>(e => e.UserId == code.UserId && e.Code == "123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoEligibleUser_ReturnsSuccessAndDefersNothing()
    {
        _identity.SendResetPasswordCodeAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((PasswordResetCode?)null);

        var result = await _handler.Handle(new SendResetPasswordCodeCommand("ghost@example.com"), TestContext.Current.CancellationToken);

        result.ThrowIfError();
        Assert.Equal(0, _dispatcher.DeferredCount);
        Assert.Empty(_publisher.ReceivedCalls());
    }
}
