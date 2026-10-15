using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews;
using Judhur.Application.Features.Sellers.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Sellers.Queries.GetSellerProfile;

public sealed class GetSellerProfileQueryHandler(IIdentityService identityService, IAppDbContext context, ILogger<GetSellerProfileQueryHandler> logger)
    : IRequestHandler<GetSellerProfileQuery, Result<SellerProfileDto>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetSellerProfileQueryHandler> _logger = logger;

    public async Task<Result<SellerProfileDto>> Handle(GetSellerProfileQuery request, CancellationToken cancellationToken)
    {
        var sellerResult = await _identityService.GetSellerInfoAsync(request.SellerId, cancellationToken);
        if (sellerResult.IsError)
        {
            _logger.LogWarning("Get seller profile rejected: seller {SellerId} not found", request.SellerId);
            return sellerResult.Errors;
        }
        var seller = sellerResult.Value;

        var activeListingsCount = await _context.Properties
            .CountAsync(p => p.SellerId == seller.Id
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive,
                cancellationToken);

        var rating = await SellerRatings.GetAsync(_context, seller.Id, cancellationToken);

        _logger.LogInformation("Seller profile {SellerId} retrieved with {ActiveListingsCount} active listings and {ReviewsCount} reviews", seller.Id, activeListingsCount, rating.ReviewsCount);
        return new SellerProfileDto(
            seller.Id,
            seller.FullName,
            seller.ProfileImageUrl,
            seller.City,
            seller.Bio,
            seller.MemberSinceUtc,
            activeListingsCount,
            rating.AverageRating,
            rating.ReviewsCount);
    }
}
