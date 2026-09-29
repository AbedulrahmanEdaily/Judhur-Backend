using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Properties.Queries.GetPendingProperties;

public sealed class GetPendingPropertiesQueryHandler(IUser user, IAppDbContext context, ILogger<GetPendingPropertiesQueryHandler> logger) : IRequestHandler<GetPendingPropertiesQuery, Result<PaginatedList<PendingPropertyDto>>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetPendingPropertiesQueryHandler> _logger = logger;

    public async Task<Result<PaginatedList<PendingPropertyDto>>> Handle(GetPendingPropertiesQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0)
        {
            _logger.LogWarning("Get pending properties rejected: invalid page {Page}", request.Page);
            return PropertyErrors.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get pending properties rejected: invalid page size {PageSize}", request.PageSize);
            return PropertyErrors.PageSizeInvalid;
        }
        var query = _context.Properties
            .AsNoTracking()
            .Where(p => p.ModerationStatus == ModerationStatus.Pending
                && p.OwnershipDocumentPublicId != null
                && p.PropertyImages.Count >= Property.MinImages
                && p.PropertyImages.Any(i => i.IsMainImage));
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PendingPropertyDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                PaymentType = p.PaymentType,
                PropertyType = p.PropertyType,
                City = p.City,
                Region = p.Region,
                MainImageUrl = p.PropertyImages
                    .Where(i => i.IsMainImage)
                    .Select(i => i.FileUrl)
                    .FirstOrDefault(),
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Retrieved {Count} of {TotalCount} pending properties (page {Page}, size {PageSize})", items.Count, count, request.Page, request.PageSize);
        return new PaginatedList<PendingPropertyDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}