using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Application.Features.Reviews.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reviews;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<CreateReviewCommandHandler> logger,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<CreateReviewCommandHandler> _logger = logger;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } reviewerId)
        {
            _logger.LogWarning("Create review rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        if (reviewerId == request.SellerId)
        {
            _logger.LogWarning("Create review rejected: user {UserId} tried to review themselves", reviewerId);
            return ReviewErrors.CannotReviewSelf;
        }

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.ReviewerId == reviewerId && r.SellerId == request.SellerId, cancellationToken);
        if (alreadyReviewed)
        {
            _logger.LogWarning("Create review rejected: user {UserId} already reviewed seller {SellerId}", reviewerId, request.SellerId);
            return ReviewErrors.DuplicateReview;
        }

        var isEligible = await SellerRatings.IsEligibleAsync(_context, reviewerId, request.SellerId, cancellationToken);
        if (!isEligible)
        {
            _logger.LogWarning("Create review rejected: user {UserId} has no conversation with a reply from seller {SellerId}", reviewerId, request.SellerId);
            return ReviewErrors.NotEligible;
        }

        var reviewResult = Review.Create(Guid.CreateVersion7(), reviewerId, request.SellerId, request.Rating, request.Comment);
        if (reviewResult.IsError)
        {
            _logger.LogWarning("Create review rejected for user {UserId} on seller {SellerId}: {ErrorCode}", reviewerId, request.SellerId, reviewResult.TopError.Code);
            return reviewResult.Errors;
        }

        var review = reviewResult.Value;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        var created = new ReviewCreated(review.Id, review.SellerId, review.Rating);
        _dispatcher.Defer(ct => _publisher.Publish(created, ct));
        _logger.LogInformation("Review {ReviewId} created by user {UserId} for seller {SellerId} with rating {Rating}", review.Id, reviewerId, review.SellerId, review.Rating);
        return review.ToDto();
    }
}
