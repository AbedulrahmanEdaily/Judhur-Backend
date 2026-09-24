using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandHandler(IIdentityService identityService, ILogger<LoginCommandHandler> logger, ITokenProvider tokenProvider) : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<LoginCommandHandler> _logger = logger;
    private readonly ITokenProvider _tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginResult = await _identityService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (loginResult.IsError)
        {
            _logger.LogWarning("Login failed: {ErrorCode}", loginResult.TopError.Code);
            return loginResult.Errors;
        }
        var generateTokenResult = await _tokenProvider.GenerateJwtTokenAsync(loginResult.Value, cancellationToken);
        if (generateTokenResult.IsError)
        {
            _logger.LogError("Token generation failed for user {UserId}: {ErrorCode}", loginResult.Value.UserId, generateTokenResult.TopError.Code);
            return generateTokenResult.Errors;
        }
        _logger.LogInformation("User {UserId} logged in", loginResult.Value.UserId);
        return generateTokenResult.Value;
    }
}