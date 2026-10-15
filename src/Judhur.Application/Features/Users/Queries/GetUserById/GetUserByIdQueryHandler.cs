using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Users.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(ILogger<GetUserByIdQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetUserByIdQuery, Result<AdminUserDto>>
{
    private readonly ILogger<GetUserByIdQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<AdminUserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _identityService.GetUserForAdminAsync(request.UserId, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Get user {UserId} for admin failed: {ErrorCode}", request.UserId, result.TopError.Code);
            return result.Errors;
        }

        _logger.LogInformation("User {UserId} retrieved for admin", request.UserId);
        return result.Value;
    }
}
