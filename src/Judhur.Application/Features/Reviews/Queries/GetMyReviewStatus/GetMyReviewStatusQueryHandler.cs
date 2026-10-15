using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reviews.Queries.GetMyReviewStatus;

public sealed class GetMyReviewStatusQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyReviewStatusQueryHandler> logger, IIdentityService identityService)
    : IRequestHandler<GetMyReviewStatusQuery, Result<MyReviewStatusDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyReviewStatusQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<MyReviewStatusDto>> Handle(GetMyReviewStatusQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my review status rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var review = await _context.Reviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReviewerId == userId && r.SellerId == request.SellerId, cancellationToken);
        var canReview = review is null
            && userId != request.SellerId
            && await SellerRatings.IsEligibleAsync(_context, userId, request.SellerId, cancellationToken)
            && await _identityService.IsActiveAccountAsync(request.SellerId, cancellationToken);

        _logger.LogDebug("Review status for user {UserId} on seller {SellerId}: has review {HasReview}, can review {CanReview}", userId, request.SellerId, review is not null, canReview);
        return new MyReviewStatusDto(canReview, review?.ToDto());
    }
}
