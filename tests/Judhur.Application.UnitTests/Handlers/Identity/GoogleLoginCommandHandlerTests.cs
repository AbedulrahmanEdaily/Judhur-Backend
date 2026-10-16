using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.GoogleLogin;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Tests.Common;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Judhur.Application.UnitTests.Handlers.Identity;

public sealed class GoogleLoginCommandHandlerTests
{
    private static readonly GoogleUser GoogleUser = new("google-sub", "user@gmail.com", true, "علي أحمد", null);
    private static readonly AppUserDto User = new(Guid.NewGuid(), "user@gmail.com", ["User"]);
    private static readonly TokenResponse Tokens = new("access", "refresh", TestClock.UtcNow.AddMinutes(15));
    private static readonly GoogleLoginCommand Command = new("id-token", "0591234567", "جنين");

    private readonly IGoogleTokenValidator _google = Substitute.For<IGoogleTokenValidator>();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly ITokenProvider _tokens = Substitute.For<ITokenProvider>();
    private readonly GoogleLoginCommandHandler _handler;

    public GoogleLoginCommandHandlerTests()
    {
        _handler = new GoogleLoginCommandHandler(_google, _identity, _tokens, NullLogger<GoogleLoginCommandHandler>.Instance);
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>()).Returns(ResultTask.Of(GoogleUser));
        _identity.SignInWithGoogleAsync(GoogleUser, "0591234567", "جنين", Arg.Any<CancellationToken>()).Returns(ResultTask.Of(User));
        _tokens.GenerateJwtTokenAsync(User, Arg.Any<CancellationToken>()).Returns(ResultTask.Of(Tokens));
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsTokens()
    {
        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Same(Tokens, result.GetValueOrThrow());
    }

    [Fact]
    public async Task Handle_ValidToken_PassesPhoneAndCityToSignIn()
    {
        await _handler.Handle(Command, TestContext.Current.CancellationToken);

        await _identity.Received(1).SignInWithGoogleAsync(GoogleUser, "0591234567", "جنين", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidGoogleToken_ReturnsErrorAndSkipsSignIn()
    {
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<GoogleUser>(ApplicationError.InvalidGoogleToken));

        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.InvalidGoogleToken.Code, result.TopError.Code);
        await _identity.DidNotReceiveWithAnyArgs().SignInWithGoogleAsync(default!, default, default, default);
        await _tokens.DidNotReceiveWithAnyArgs().GenerateJwtTokenAsync(default!, default);
    }

    [Fact]
    public async Task Handle_GoogleUnavailable_ReturnsError()
    {
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<GoogleUser>(ApplicationError.GoogleUnavailable));

        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.GoogleUnavailable.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_SignInFails_ReturnsErrorAndSkipsTokens()
    {
        _identity.SignInWithGoogleAsync(GoogleUser, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<AppUserDto>(ApplicationError.AccountBanned));

        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.AccountBanned.Code, result.TopError.Code);
        await _tokens.DidNotReceiveWithAnyArgs().GenerateJwtTokenAsync(default!, default);
    }

    [Fact]
    public async Task Handle_TokenGenerationFails_ReturnsError()
    {
        _tokens.GenerateJwtTokenAsync(User, Arg.Any<CancellationToken>())
            .Returns(ResultTask.Failed<TokenResponse>(ApplicationError.TokenGenerationFailed));

        var result = await _handler.Handle(Command, TestContext.Current.CancellationToken);

        Assert.Equal(ApplicationError.TokenGenerationFailed.Code, result.TopError.Code);
    }
}
