using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Favorites;

public static class FavoriteErrors
{
    public static readonly Error UserIdRequired = Error.Validation("FavoriteErrors.UserIdRequired", "المستخدم مطلوب");
    public static readonly Error PropertyIdRequired = Error.Validation("FavoriteErrors.PropertyIdRequired", "العقار مطلوب");
    public static readonly Error AlreadyFavorites = Error.Conflict("FavoriteErrors.AlreadyFavorites", "العقار في المفضله بالفعل لديك");
}