using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Users.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(ILogger<GetUsersQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, Result<PaginatedList<AdminUserDto>>>
{
    private readonly ILogger<GetUsersQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<PaginatedList<AdminUserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (!Pagination.IsPageValid(request.Page))
        {
            _logger.LogWarning("Get users rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get users rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var users = await _identityService.GetUsersForAdminAsync(
            new AdminUsersFilter(request.Search, request.Status, request.Page, request.PageSize),
            cancellationToken);
        _logger.LogInformation("Retrieved {Count} of {TotalCount} {Status} users (page {Page}, size {PageSize})", users.Items?.Count ?? 0, users.TotalCount, request.Status, request.Page, request.PageSize);
        return users;
    }
}
