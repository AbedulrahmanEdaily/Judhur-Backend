using System.Text.RegularExpressions;

using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Users.Enums;

namespace Judhur.Domain.Users;

public sealed class User : AuditableEntity
{
    public const int MaxNameLength = 150;
    public const int MaxPhoneNumberLength = 20;
    public const int MaxCityLength = 100;
    public const int MaxBanReasonLength = 500;

    public string Name { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public string City { get; private set; } = null!;
    public bool IsBanned { get; private set; }
    public Guid? BannedBy { get; private set; }
    public DateTimeOffset? BannedAtUtc { get; private set; }
    public string? BannedReason { get; private set; }
    private User()
    { }
    private User(Guid id, string name, string phoneNumber, UserRole role, string city)
        : base(id)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Role = role;
        City = city;
    }

    public static Result<User> Create(Guid id, string name, string phoneNumber, UserRole role, string city)
    {
        var error = ValidateProfile(name, phoneNumber, city);
        if (error is not null)
        {
            return error.Value;
        }
        if (!Enum.IsDefined(role))
        {
            return UserErrors.InvalidUserRole;
        }
        return new User(id, name, phoneNumber, role, city);
    }

    public Result<Updated> Update(string name, string phoneNumber, string city)
    {
        var error = ValidateProfile(name, phoneNumber, city);
        if (error is not null)
        {
            return error.Value;
        }
        Name = name;
        PhoneNumber = phoneNumber;
        return Result.Updated;
    }

    public Result<Updated> Ban(Guid adminId, DateTimeOffset bannedAtUtc, string reason)
    {
        if (IsBanned)
        {
            return UserErrors.UserIsAlreadyBanned;
        }
        if (adminId == Guid.Empty)
        {
            return UserErrors.AdminIdRequired;
        }
        if (Id == adminId)
        {
            return UserErrors.CannotBanSelf;
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return UserErrors.BanReasonRequired;
        }
        if (reason.Length > MaxBanReasonLength)
        {
            return UserErrors.BanReasonTooLong;
        }
        IsBanned = true;
        BannedBy = adminId;
        BannedAtUtc = bannedAtUtc;
        BannedReason = reason;
        return Result.Updated;
    }

    public Result<Updated> Unban()
    {
        if (!IsBanned)
        {
            return UserErrors.UserIsAlreadyUnBanned;
        }
        IsBanned = false;
        BannedBy = null;
        BannedAtUtc = null;
        BannedReason = null;
        return Result.Updated;
    }

    private static Error? ValidateProfile(string name, string phoneNumber, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }
        if (name.Length > MaxNameLength)
        {
            return UserErrors.NameTooLong;
        }
        if (string.IsNullOrWhiteSpace(phoneNumber) || !IsValidPhoneNumber(phoneNumber))
        {
            return UserErrors.InvalidPhoneNumber;
        }
        if (string.IsNullOrWhiteSpace(city))
        {
            return UserErrors.CityRequired;
        }
        if (city.Length > MaxCityLength)
        {
            return UserErrors.CityTooLong;
        }
        return null;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
        => Regex.IsMatch(phoneNumber, @"^(?:\+?(?:970|972)\d{9}|05\d{8})$");
}
