using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Commands.ResubmitPropertyForReview;

public sealed record ResubmitPropertyForReviewCommand(Guid PropertyId) : IRequest<Result<Updated>>;
