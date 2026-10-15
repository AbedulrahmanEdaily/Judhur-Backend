using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(Guid ReviewId) : IInvalidateCacheCommand<Result<Deleted>>
{
    string[] IInvalidateCacheCommand.Tags => ["sellers"];
}
