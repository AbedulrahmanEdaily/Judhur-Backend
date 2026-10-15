using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reviews;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(IUser user, IAppDbContext context, ILogger<UpdateReviewCommandHandler> logger)
    : IRequestHandler<UpdateReviewCommand, Result<ReviewDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<UpdateReviewCommandHandler> _logger = logger;

    public async Task<Result<ReviewDto>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } reviewerId)
        {
            _logger.LogWarning("Update review rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.ReviewerId == reviewerId, cancellationToken);
        if (review is null)
        {
            _logger.LogWarning("Update review rejected: review {ReviewId} not found for user {UserId}", request.ReviewId, reviewerId);
            return ReviewErrors.NotFound;
        }

        var updateResult = review.Update(request.Rating, request.Comment);
        if (updateResult.IsError)
        {
            _logger.LogWarning("Update review {ReviewId} failed: {ErrorCode}", review.Id, updateResult.TopError.Code);
            return updateResult.Errors;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Review {ReviewId} updated by user {UserId} with rating {Rating}", review.Id, reviewerId, review.Rating);
        return review.ToDto();
    }
}
