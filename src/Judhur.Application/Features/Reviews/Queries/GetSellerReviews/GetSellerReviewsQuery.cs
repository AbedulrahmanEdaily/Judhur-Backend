using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Reviews.Queries.GetSellerReviews;

public sealed record GetSellerReviewsQuery(Guid SellerId, int Page, int PageSize) : IRequest<Result<PaginatedList<SellerReviewDto>>>;
