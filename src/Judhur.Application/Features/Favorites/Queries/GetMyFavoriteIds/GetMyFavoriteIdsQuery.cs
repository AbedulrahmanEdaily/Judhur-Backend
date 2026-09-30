using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Favorites.Queries.GetMyFavoriteIds;

public sealed record GetMyFavoriteIdsQuery : IRequest<Result<List<Guid>>>;
