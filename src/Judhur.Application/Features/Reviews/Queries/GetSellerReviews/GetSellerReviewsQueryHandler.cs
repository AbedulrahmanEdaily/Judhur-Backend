using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reviews.Queries.GetSellerReviews;

public sealed class GetSellerReviewsQueryHandler(IAppDbContext context, ILogger<GetSellerReviewsQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetSellerReviewsQuery, Result<PaginatedList<SellerReviewDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetSellerReviewsQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<PaginatedList<SellerReviewDto>>> Handle(GetSellerReviewsQuery request, CancellationToken cancellationToken)
    {
        if (!Pagination.IsPageValid(request.Page))
        {
            _logger.LogWarning("Get seller reviews rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > Pagination.MaxPageSize)
        {
            _logger.LogWarning("Get seller reviews rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        if (!await _identityService.IsActiveAccountAsync(request.SellerId, cancellationToken))
        {
            _logger.LogWarning("Get seller reviews rejected: seller {SellerId} is banned, deleted or does not exist", request.SellerId);
            return ApplicationError.SellerNotFound;
        }

        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.SellerId == request.SellerId);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new { r.Id, r.ReviewerId, r.Rating, r.Comment, r.CreatedAtUtc, r.LastModifiedUtc })
            .ToListAsync(cancellationToken);

        var reviewers = await _identityService.GetUsersInfoAsync(rows.Select(r => r.ReviewerId).Distinct().ToList(), cancellationToken);
        var items = rows
            .Select(r => new SellerReviewDto(
                r.Id,
                r.Rating,
                r.Comment,
                reviewers.TryGetValue(r.ReviewerId, out var reviewer)
                    ? new ReviewerDto(reviewer.FullName, reviewer.ProfileImageUrl)
                    : null,
                r.CreatedAtUtc,
                r.LastModifiedUtc > r.CreatedAtUtc))
            .ToList();

        _logger.LogInformation("Retrieved {Count} of {TotalCount} reviews for seller {SellerId} (page {Page}, size {PageSize})", items.Count, count, request.SellerId, request.Page, request.PageSize);
        return new PaginatedList<SellerReviewDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}
