using System.Net.Mail;
using System.Text.RegularExpressions;
using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Users.Enums;

namespace Judhur.Domain.Users;

public sealed class User : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsBanned { get; private set; } = false;
    public Guid? BannedBy { get; private set; }
    public DateTimeOffset? BannedAtUtc { get; private set; }
    public string? BannedReason { get; private set; }
    private User() { }
    private User(Guid id, string name, string email, string phoneNumber, UserRole userRole) : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        Role = userRole;
    }

    private static bool CheckPhoneNumber(string phoneNumber)
    {
        return Regex.IsMatch(phoneNumber, @"^\+?(?:970|972)\d{9}$");
    }
    public static Result<User> Create(Guid id, string name, string email, string phoneNumber, UserRole userRole)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            return UserErrors.EmailRequired;
        }
        if (string.IsNullOrWhiteSpace(phoneNumber) || !CheckPhoneNumber(phoneNumber))
        {
            return UserErrors.InvalidPhoneNumber;
        }
        if (!Enum.IsDefined(userRole))
        {
            return UserErrors.InvalidUserRole;
        }
        if (!MailAddress.TryCreate(email, out _))
        {
            return UserErrors.InvalidEmail;
        }
        return new User(id, name, email, phoneNumber, userRole);
    }
    public Result<Updated> Update(string name, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }
        if (string.IsNullOrWhiteSpace(phoneNumber) || !CheckPhoneNumber(phoneNumber))
        {
            return UserErrors.InvalidPhoneNumber;
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
        if (string.IsNullOrWhiteSpace(reason))
        {
            return UserErrors.BanReasonRequired;
        }
        if (Id == adminId) {
            return UserErrors.CannotBanSelf;
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
}