using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Favorites.Queries.GetMyFavoriteIds;

public sealed class GetMyFavoriteIdsQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyFavoriteIdsQueryHandler> logger) : IRequestHandler<GetMyFavoriteIdsQuery, Result<List<Guid>>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyFavoriteIdsQueryHandler> _logger = logger;

    public async Task<Result<List<Guid>>> Handle(GetMyFavoriteIdsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my favorite ids rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var publicPropertyIds = _context.Properties
            .Where(p => p.ModerationStatus == ModerationStatus.Approved && p.IsActive)
            .Select(p => p.Id);

        var propertyIds = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId && publicPropertyIds.Contains(f.PropertyId))
            .Select(f => f.PropertyId)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} favorite property ids for user {UserId}", propertyIds.Count, userId);
        return propertyIds;
    }
}
