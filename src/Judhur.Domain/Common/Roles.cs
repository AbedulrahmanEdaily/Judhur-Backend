namespace Judhur.Domain.Common;

public static class Roles
{
    public const string User = "User";

    public const string Admin = "Admin";
    public static readonly HashSet<string> RegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        User,Admin
    };
}
