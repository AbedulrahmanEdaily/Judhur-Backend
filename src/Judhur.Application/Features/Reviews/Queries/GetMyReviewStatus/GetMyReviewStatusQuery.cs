using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Reviews.Queries.GetMyReviewStatus;

public sealed record GetMyReviewStatusQuery(Guid SellerId) : IRequest<Result<MyReviewStatusDto>>;
