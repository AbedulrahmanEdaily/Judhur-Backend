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

namespace Judhur.Application.Features.Properties.Queries.GetProperties;

public sealed class GetPropertiesQueryHandler(ILogger<GetPropertiesQueryHandler> logger, IAppDbContext context) : IRequestHandler<GetPropertiesQuery, Result<PaginatedList<PropertySummaryDto>>>
{
    private readonly ILogger<GetPropertiesQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<PaginatedList<PropertySummaryDto>>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        if (request.Page <= 0)
        {
            _logger.LogWarning("Get properties rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get properties rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }
        if (request.Cities.Count > GetPropertiesQuery.MaxCities)
        {
            _logger.LogWarning("Get properties rejected: {CityCount} cities requested, the limit is {MaxCities}", request.Cities.Count, GetPropertiesQuery.MaxCities);
            return PropertyErrors.TooManyCitiesInFilter;
        }
        var propertyQuery = _context.Properties.AsNoTracking().Where(p => p.ModerationStatus == ModerationStatus.Approved && p.IsActive).AsQueryable();
        propertyQuery = ApplyFilters(propertyQuery, request);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            propertyQuery = ApplySearchTerm(propertyQuery, request.SearchTerm);
        }
        propertyQuery = ApplySorting(propertyQuery, request.SortColumn, request.SortDirection)
            .ThenBy(p => p.Id);
        var count = await propertyQuery.CountAsync(cancellationToken);
        var items = await propertyQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(p => new PropertySummaryDto
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            PaymentType = p.PaymentType,
            PropertyType = p.PropertyType,
            PropertyStatus = p.PropertyStatus,
            Area = p.Area,
            City = p.City,
            Region = p.Region,
            MainImageUrl = p.PropertyImages
                .Where(i => i.IsMainImage)
                .Select(i => i.FileUrl)
                .FirstOrDefault()
        })
        .ToListAsync(cancellationToken);
        _logger.LogInformation("Retrieved {Count} of {TotalCount} properties (page {Page}, size {PageSize})", items.Count, count, request.Page, request.PageSize);
        return new PaginatedList<PropertySummaryDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
    private IOrderedQueryable<Property> ApplySorting(IQueryable<Property> query, string sortColumn, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortColumn.ToLowerInvariant() switch
        {
            "createdat" => isDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
            "city" => isDescending ? query.OrderByDescending(p => p.City) : query.OrderBy(p => p.City),
            "price" => isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "landclassification" => isDescending ? query.OrderByDescending(p => p.LandClassification) : query.OrderBy(p => p.LandClassification),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };
    }

    private IQueryable<Property> ApplySearchTerm(IQueryable<Property> query, string searchTerm)
    {
        var normalized = searchTerm.Trim().ToLowerInvariant();
        return query.Where(p => p.Title.ToLower().Contains(normalized));
    }

    private IQueryable<Property> ApplyFilters(IQueryable<Property> query, GetPropertiesQuery searchQuery)
    {
        if (searchQuery.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= searchQuery.MinPrice.Value);
        }
        if (searchQuery.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= searchQuery.MaxPrice.Value);
        }
        if (searchQuery.Cities.Count > 0)
        {
            query = query.Where(p => searchQuery.Cities.Contains(p.City));
        }
        if (searchQuery.LandClassifications.Count > 0)
        {
            query = query.Where(p => searchQuery.LandClassifications.Contains(p.LandClassification));
        }
        if (searchQuery.LegalStatuses.Count > 0)
        {
            query = query.Where(p => searchQuery.LegalStatuses.Contains(p.LegalStatus));
        }
        if (searchQuery.PaymentTypes.Count > 0)
        {
            query = query.Where(p => searchQuery.PaymentTypes.Contains(p.PaymentType));
        }
        if (searchQuery.PropertyStatuses.Count > 0)
        {
            query = query.Where(p => searchQuery.PropertyStatuses.Contains(p.PropertyStatus));
        }
        if (searchQuery.PropertyTypes.Count > 0)
        {
            query = query.Where(p => searchQuery.PropertyTypes.Contains(p.PropertyType));
        }
        return query;
    }
}