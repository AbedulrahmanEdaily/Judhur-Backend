using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reviews;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(IUser user, IAppDbContext context, ILogger<DeleteReviewCommandHandler> logger)
    : IRequestHandler<DeleteReviewCommand, Result<Deleted>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<DeleteReviewCommandHandler> _logger = logger;

    public async Task<Result<Deleted>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } reviewerId)
        {
            _logger.LogWarning("Delete review rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.ReviewerId == reviewerId, cancellationToken);
        if (review is null)
        {
            _logger.LogWarning("Delete review rejected: review {ReviewId} not found for user {UserId}", request.ReviewId, reviewerId);
            return ReviewErrors.NotFound;
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Review {ReviewId} on seller {SellerId} deleted by user {UserId}", review.Id, review.SellerId, reviewerId);
        return Result.Deleted;
    }
}
