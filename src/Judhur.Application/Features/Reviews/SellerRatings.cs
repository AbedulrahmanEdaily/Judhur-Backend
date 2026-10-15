using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Reviews;

using Microsoft.EntityFrameworkCore;

namespace Judhur.Application.Features.Reviews;

public static class SellerRatings
{
    public static async Task<SellerRatingDto> GetAsync(IAppDbContext context, Guid sellerId, CancellationToken cancellationToken)
    {
        var stats = await context.Reviews
            .Where(r => r.SellerId == sellerId)
            .GroupBy(r => r.SellerId)
            .Select(g => new { Average = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);
        return stats is null
            ? new SellerRatingDto(null, 0)
            : new SellerRatingDto(Math.Round(stats.Average, 1, MidpointRounding.AwayFromZero), stats.Count);
    }

    public static Task<bool> IsEligibleAsync(IAppDbContext context, Guid reviewerId, Guid sellerId, CancellationToken cancellationToken)
        => context.Conversations.AnyAsync(
            c => c.BuyerId == reviewerId
                && c.SellerId == sellerId
                && c.Messages.Any(m => m.SenderId == sellerId),
            cancellationToken);

    public static ReviewDto ToDto(this Review review)
        => new(review.Id, review.SellerId, review.Rating, review.Comment, review.CreatedAtUtc, review.LastModifiedUtc);
}
