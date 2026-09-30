using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Favorites.Commands.AddFavorite;

public sealed record AddFavoriteCommand(Guid PropertyId) : IRequest<Result<Created>>;