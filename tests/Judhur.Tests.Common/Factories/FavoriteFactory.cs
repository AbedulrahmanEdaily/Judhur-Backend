using Judhur.Domain.Common.Results;
using Judhur.Domain.Favorites;

namespace Judhur.Tests.Common.Factories;

public static class FavoriteFactory
{
    public static readonly Guid DefaultUserId = ConversationFactory.DefaultBuyerId;
    public static readonly Guid DefaultPropertyId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000b4");

    public static Result<Favorite> CreateResult(Guid? userId = null, Guid? propertyId = null, Guid? id = null)
        => Favorite.Create(id ?? Guid.NewGuid(), userId ?? DefaultUserId, propertyId ?? DefaultPropertyId);

    public static Favorite Create(Guid? userId = null, Guid? propertyId = null, Guid? id = null)
        => CreateResult(userId, propertyId, id).GetValueOrThrow();
}
