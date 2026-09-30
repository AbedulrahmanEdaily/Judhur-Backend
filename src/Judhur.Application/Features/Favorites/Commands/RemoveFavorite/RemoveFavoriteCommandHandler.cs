using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Favorites;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Favorites.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler(IUser user, IAppDbContext context, ILogger<RemoveFavoriteCommandHandler> logger) : IRequestHandler<RemoveFavoriteCommand, Result<Deleted>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<RemoveFavoriteCommandHandler> _logger = logger;

    public async Task<Result<Deleted>> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Remove favorite rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId, cancellationToken);
        if (favorite is null)
        {
            _logger.LogWarning("Remove favorite rejected: property {PropertyId} is not in the favorites of user {UserId}", request.PropertyId, userId);
            return FavoriteErrors.NotFound;
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} removed from the favorites of user {UserId}", request.PropertyId, userId);
        return Result.Deleted;
    }
}