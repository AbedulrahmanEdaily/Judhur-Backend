using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Favorites.Queries.GetMyFavorites;

public sealed class GetMyFavoritesQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyFavoritesQueryHandler> logger) : IRequestHandler<GetMyFavoritesQuery, Result<PaginatedList<PropertySummaryDto>>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyFavoritesQueryHandler> _logger = logger;

    public async Task<Result<PaginatedList<PropertySummaryDto>>> Handle(GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my favorites rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        if (request.Page <= 0)
        {
            _logger.LogWarning("Get my favorites rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }

        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get my favorites rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var publicProperties = _context.Properties
            .Where(p => p.ModerationStatus == ModerationStatus.Approved && p.IsActive);

        var query = _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(publicProperties,
                favorite => favorite.PropertyId,
                property => property.Id,
                (favorite, property) => new { Favorite = favorite, Property = property });

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Favorite.CreatedAtUtc)
            .ThenByDescending(x => x.Favorite.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PropertySummaryDto
            {
                Id = x.Property.Id,
                Title = x.Property.Title,
                Price = x.Property.Price,
                PaymentType = x.Property.PaymentType,
                PropertyType = x.Property.PropertyType,
                PropertyStatus = x.Property.PropertyStatus,
                Area = x.Property.Area,
                City = x.Property.City,
                Region = x.Property.Region,
                MainImageUrl = x.Property.PropertyImages
                    .Where(i => i.IsMainImage)
                    .Select(i => i.FileUrl)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} of {TotalCount} favorites for user {UserId} (page {Page}, size {PageSize})", items.Count, count, userId, request.Page, request.PageSize);
        return new PaginatedList<PropertySummaryDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}
