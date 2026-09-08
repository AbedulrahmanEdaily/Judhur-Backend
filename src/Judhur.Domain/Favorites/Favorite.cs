using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Favorites;

public sealed class Favorite : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid PropertyId { get; private set; }

    private Favorite() { }
    private Favorite(Guid id, Guid userId, Guid propertyId) : base(id)
    {
        PropertyId = propertyId;
        UserId = userId;
    }

    public static Result<Favorite> Create(Guid id, Guid userId, Guid propertyId)
    {
        if (propertyId == Guid.Empty)
        {
            return FavoriteErrors.PropertyIdRequired;
        }
        if (userId == Guid.Empty)
        {
            return FavoriteErrors.UserIdRequired;
        }

        return new Favorite(id, userId, propertyId);
    }
}