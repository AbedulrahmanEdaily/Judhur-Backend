using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.Logout;

public sealed class LogoutCommandHandler(IAppDbContext context, ILogger<LogoutCommandHandler> logger) : IRequestHandler<LogoutCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<LogoutCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            _logger.LogWarning("Logout called with an unknown or already revoked refresh token");
            return Result.Success;
        }

        _context.RefreshTokens.Remove(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} logged out", refreshToken.UserId);

        return Result.Success;
    }
}
