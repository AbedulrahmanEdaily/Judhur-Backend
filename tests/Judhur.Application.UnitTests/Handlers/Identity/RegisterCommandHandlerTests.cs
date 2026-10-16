using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Commands.Register;
using Judhur.Application.Features.Identity.Events;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Tests.Common;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class RegisterCommandHandlerTests
{
    private static readonly RegisterCommand Command = new("علي أحمد", "user@example.com", "0591234567", "رام الله", "نبذة", "Passw0rd");

    private readonly Guid _userId = Guid.NewGuid();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly RecordingDeferredDispatcher _dispatcher = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_identity, _dispatcher, _publisher, NullLogger<RegisterCommandHandler>.Instance);
        _identity.CreateNewUserAsync(Arg.Any<NewUserRegistration>(), Arg.Any<CancellationToken>()).Returns(ResultTask.Of(_userId));
    }

    [Fact]
    public async Task Handle_NewUser_ReturnsSuccess()
    {
        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        result.ThrowIfError();
    }

    [Fact]
    public async Task Handle_NewUser_PassesEveryFieldToIdentity()
    {
        await _handler.Handle(Command, TestContext.Current.CancellationToken);

        await _identity.Received(1).CreateNewUserAsync(
            new NewUserRegistration("علي أحمد", "user@example.com", "0591234567", "رام الله", "نبذة", "Passw0rd"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUser_DefersUserRegisteredInsteadOfPublishingInline()
    {
        await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(1, _dispatcher.DeferredCount);
        Assert.Empty(_publisher.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_NewUser_DeferredWorkPublishesUserRegistered()
    {
        await _handler.Handle(Command, TestContext.Current.CancellationToken);
        await _dispatcher.DispatchAsync(TestContext.Current.CancellationToken);

        await _publisher.Received(1).Publish(Arg.Is<UserRegistered>(e => e.UserId == _userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdentityFails_ReturnsErrorAndDefersNothing()
    {
        _identity.CreateNewUserAsync(Arg.Any<NewUserRegistration>(), Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<Guid>(ApplicationError.DuplicateEmail));

        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.DuplicateEmail.Code, result.TopError.Code);
        Assert.Equal(0, _dispatcher.DeferredCount);
        Assert.Empty(_publisher.ReceivedCalls());
    }
}
