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
            return PropertyErrors.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get properties rejected: invalid page size {PageSize}", request.PageSize);
            return PropertyErrors.PageSizeInvalid;
        }
        var propertyQuery = _context.Properties.AsNoTracking().Where(p => p.ModerationStatus == ModerationStatus.Approved && p.IsActive).AsQueryable();
        propertyQuery = ApplyFilters(propertyQuery, request);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            propertyQuery = ApplySearchTerm(propertyQuery, request.SearchTerm);
        }
        propertyQuery = ApplySorting(propertyQuery, request.SortColumn, request.SortDirection);
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
            Region = p.Region
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
    private IQueryable<Property> ApplySorting(IQueryable<Property> query, string sortColumn, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase);
        return sortColumn.ToLower() switch
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
        var normalized = searchTerm.Trim().ToLower();
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
        if (!string.IsNullOrWhiteSpace(searchQuery.City))
        {
            query = query.Where(p => p.City == searchQuery.City);
        }
        if (searchQuery.LandClassification.HasValue)
        {
            query = query.Where(p => p.LandClassification == searchQuery.LandClassification);
        }
        if (searchQuery.LegalStatus.HasValue)
        {
            query = query.Where(p => p.LegalStatus == searchQuery.LegalStatus);
        }
        if (searchQuery.PaymentType.HasValue)
        {
            query = query.Where(p => p.PaymentType == searchQuery.PaymentType);
        }
        if (searchQuery.PropertyStatus.HasValue)
        {
            query = query.Where(p => p.PropertyStatus == searchQuery.PropertyStatus);
        }
        if (searchQuery.PropertyType.HasValue)
        {
            query = query.Where(p => p.PropertyType == searchQuery.PropertyType);
        }
        return query;
    }
}