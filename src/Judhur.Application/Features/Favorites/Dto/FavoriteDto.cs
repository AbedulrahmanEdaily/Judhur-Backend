namespace Judhur.Application.Features.Favorites.Dto;

public sealed class FavoriteDto
{
    public Guid FavoriteId {get;set;}
    public Guid PropertyId{get;set;}
    public Guid UserId{get;set;}
}