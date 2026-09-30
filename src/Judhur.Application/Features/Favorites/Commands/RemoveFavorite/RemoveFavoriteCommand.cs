using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Favorites.Commands.RemoveFavorite;

public sealed record RemoveFavoriteCommand(Guid PropertyId) : IRequest<Result<Deleted>>;