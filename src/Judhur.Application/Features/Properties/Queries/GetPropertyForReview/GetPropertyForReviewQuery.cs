using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Queries.GetPropertyForReview;

public sealed record GetPropertyForReviewQuery(Guid PropertyId) : IRequest<Result<PropertyForReviewDto>>;