using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Favorites;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Favorites.Commands.AddFavorite;

public sealed class AddFavoriteCommandHandler(IUser user, IAppDbContext context, ILogger<AddFavoriteCommandHandler> logger) : IRequestHandler<AddFavoriteCommand, Result<Created>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<AddFavoriteCommandHandler> _logger = logger;

    public async Task<Result<Created>> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Add favorite rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var propertyIsPublic = await _context.Properties
            .AnyAsync(p => p.Id == request.PropertyId
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive,
                cancellationToken);
        if (!propertyIsPublic)
        {
            _logger.LogWarning("Add favorite rejected: property {PropertyId} not found or not publicly visible", request.PropertyId);
            return PropertyErrors.NotFound;
        }
        var alreadyFavorite = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId, cancellationToken);
        if (alreadyFavorite)
        {
            _logger.LogWarning("Add favorite rejected: property {PropertyId} is already in the favorites of user {UserId}", request.PropertyId, userId);
            return FavoriteErrors.AlreadyFavorite;
        }
        var createResult = Favorite.Create(Guid.CreateVersion7(), userId, request.PropertyId);
        if (createResult.IsError)
        {
            _logger.LogWarning("Add favorite for property {PropertyId} by user {UserId} failed: {ErrorCode}", request.PropertyId, userId, createResult.TopError.Code);
            return createResult.Errors;
        }
        _context.Favorites.Add(createResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Property {PropertyId} added to the favorites of user {UserId}", request.PropertyId, userId);
        return Result.Created;
    }
}
