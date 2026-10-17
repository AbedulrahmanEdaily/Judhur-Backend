using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.Login;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Tests.Common;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class LoginCommandHandlerTests
{
    private static readonly AppUserDto User = new(Guid.NewGuid(), "user@example.com", ["User"]);
    private static readonly TokenResponse Tokens = new("access", "refresh", TestClock.UtcNow.AddMinutes(15));

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly ITokenProvider _tokens = Substitute.For<ITokenProvider>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_identity, NullLogger<LoginCommandHandler>.Instance, _tokens);
        _identity.AuthenticateAsync("user@example.com", "Passw0rd", Arg.Any<CancellationToken>()).Returns(ResultTask.Of(User));
        _tokens.GenerateJwtTokenAsync(User, Arg.Any<CancellationToken>()).Returns(ResultTask.Of(Tokens));
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        var result = await _handler.Handle(new LoginCommand("user@example.com", "Passw0rd"), TestContext.Current.CancellationToken);

        Assert.Same(Tokens, result.GetValueOrThrow());
    }

    [Fact]
    public async Task Handle_AuthenticationFails_ReturnsErrorAndSkipsTokens()
    {
        _identity.AuthenticateAsync("user@example.com", "Passw0rd", Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<AppUserDto>(ApplicationError.InvalidPassword));

        var result = await _handler.Handle(new LoginCommand("user@example.com", "Passw0rd"), TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.InvalidPassword.Code, result.TopError.Code);
        await _tokens.DidNotReceiveWithAnyArgs().GenerateJwtTokenAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_BannedAccount_ReturnsAccountBanned()
    {
        _identity.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<AppUserDto>(ApplicationError.AccountBanned));

        var result = await _handler.Handle(new LoginCommand("user@example.com", "Passw0rd"), TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.AccountBanned.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_TokenGenerationFails_ReturnsError()
    {
        _tokens.GenerateJwtTokenAsync(User, Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<TokenResponse>(ApplicationError.TokenGenerationFailed));

        var result = await _handler.Handle(new LoginCommand("user@example.com", "Passw0rd"), TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.TokenGenerationFailed.Code, result.TopError.Code);
    }
}
