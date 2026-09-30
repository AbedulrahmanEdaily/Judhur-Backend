using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Favorites.Queries.GetMyFavorites;

public sealed record GetMyFavoritesQuery(int Page, int PageSize) : IRequest<Result<PaginatedList<PropertySummaryDto>>>;
