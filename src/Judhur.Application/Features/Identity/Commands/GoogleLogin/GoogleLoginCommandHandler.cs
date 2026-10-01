using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IGoogleTokenValidator googleTokenValidator,
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    ILogger<GoogleLoginCommandHandler> logger) : IRequestHandler<GoogleLoginCommand, Result<TokenResponse>>
{
    private readonly IGoogleTokenValidator _googleTokenValidator = googleTokenValidator;
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<GoogleLoginCommandHandler> _logger = logger;

    public async Task<Result<TokenResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var googleUserResult = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (googleUserResult.IsError)
        {
            _logger.LogWarning("Google sign-in rejected: {ErrorCode}", googleUserResult.TopError.Code);
            return googleUserResult.Errors;
        }

        var signInResult = await _identityService.SignInWithGoogleAsync(
            googleUserResult.Value,
            request.PhoneNumber,
            request.City,
            cancellationToken);
        if (signInResult.IsError)
        {
            _logger.LogWarning("Google sign-in failed: {ErrorCode}", signInResult.TopError.Code);
            return signInResult.Errors;
        }

        var tokenResult = await _tokenProvider.GenerateJwtTokenAsync(signInResult.Value, cancellationToken);
        if (tokenResult.IsError)
        {
            _logger.LogError("Token generation failed for user {UserId}: {ErrorCode}", signInResult.Value.UserId, tokenResult.TopError.Code);
            return tokenResult.Errors;
        }

        _logger.LogInformation("User {UserId} signed in with Google", signInResult.Value.UserId);
        return tokenResult.Value;
    }
}
