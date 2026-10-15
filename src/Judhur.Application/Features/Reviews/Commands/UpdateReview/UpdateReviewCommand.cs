using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand([property: JsonIgnore] Guid ReviewId, decimal Rating, string? Comment) : IInvalidateCacheCommand<Result<ReviewDto>>
{
    string[] IInvalidateCacheCommand.Tags => ["sellers"];
}
