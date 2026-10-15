using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Users.Commands.UnbanUser;

public sealed class UnbanUserCommandHandler(IUser user, ILogger<UnbanUserCommandHandler> logger, IIdentityService identityService)
    : IRequestHandler<UnbanUserCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly ILogger<UnbanUserCommandHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<Updated>> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Unban user rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var unbanResult = await _identityService.UnbanUserAsync(request.UserId, cancellationToken);
        if (unbanResult.IsError)
        {
            _logger.LogWarning("Unban user {UserId} by admin {AdminId} failed: {ErrorCode}", request.UserId, adminId, unbanResult.TopError.Code);
            return unbanResult.Errors;
        }

        _logger.LogInformation("User {UserId} unbanned by admin {AdminId}", request.UserId, adminId);
        return Result.Updated;
    }
}
