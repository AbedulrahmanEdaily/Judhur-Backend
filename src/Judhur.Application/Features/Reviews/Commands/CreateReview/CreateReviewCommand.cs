using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(Guid SellerId, decimal Rating, string? Comment) : IInvalidateCacheCommand<Result<ReviewDto>>
{
    string[] IInvalidateCacheCommand.Tags => ["sellers"];
}
