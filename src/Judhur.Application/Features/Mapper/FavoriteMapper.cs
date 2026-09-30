using Judhur.Application.Features.Favorites.Dto;
using Judhur.Domain.Favorites;

namespace Judhur.Application.Features.Mapper;

public static class FavoriteMapper
{
    public static FavoriteDto ToDto(this Favorite favorite)
    {
        return new FavoriteDto
        {
            FavoriteId = favorite.Id,
            PropertyId = favorite.PropertyId,
            UserId = favorite.UserId
        };
    }
    public static List<FavoriteDto> ToDtos(this IEnumerable<Favorite> favorites)
    {
        return [..favorites.Select(f=>f.ToDto())];
    }
}