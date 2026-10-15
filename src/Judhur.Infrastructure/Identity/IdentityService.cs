using System.Security.Cryptography;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.Features.Users.Dto;
using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports.Enums;
using Judhur.Infrastructure.Data;
using Judhur.Infrastructure.Email;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Judhur.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, FrontendSettings frontendSettings,
    TimeProvider timeProvider, AppDbContext context) : IIdentityService
{
    private const int ResetCodeLifetimeMinutes = 5;
    private const int MaxResetCodeAttempts = 5;
    private const string DeletedUserName = "مستخدم محذوف";
    private const string GoogleLoginProvider = "Google";
    private const int MaxFullNameLength = 150;
    private const int MaxProfileImageUrlLength = 500;

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly FrontendSettings _frontendSettings = frontendSettings;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly AppDbContext _context = context;

    public async Task<Result<Guid>> CreateNewUserAsync(
        NewUserRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = registration.Email,
            PhoneNumber = registration.PhoneNumber,
            UserName = registration.Email,
            City = registration.City,
            FullName = registration.FullName,
            Bio = registration.Bio,
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };
        var createResult = await _userManager.CreateAsync(user, registration.Password);
        if (!createResult.Succeeded)
        {
            return Translate(createResult);
        }
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
            return Translate(roleResult);
        }
        return user.Id;
    }
    private static List<Error> Translate(IdentityResult result)
        => [.. result.Errors
            .Where(error => error.Code != "DuplicateUserName")
            .Select(error => error.Code switch
            {
                "DuplicateEmail" or "ConcurrencyFailure"
                    => Error.Conflict($"Identity.{error.Code}", error.Description),

                "InvalidEmail" or "InvalidUserName"
                    => Error.Validation($"Identity.{error.Code}", error.Description),

                _ when error.Code.StartsWith("Password", StringComparison.Ordinal)
                    => Error.Validation($"Identity.{error.Code}", error.Description),

                _ => Error.Failure($"Identity.{error.Code}", error.Description),
            })];

    public async Task<Result<AppUserDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return InvalidCredentials();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            if (user.BannedAtUtc is not null)
            {
                return await _userManager.CheckPasswordAsync(user, password)
                    ? ApplicationError.AccountBanned
                    : InvalidCredentials();
            }
            return LockedOut();
        }

        if (signInResult.IsNotAllowed)
        {
            if (user.BannedAtUtc is not null)
            {
                return await _userManager.CheckPasswordAsync(user, password)
                    ? ApplicationError.AccountBanned
                    : InvalidCredentials();
            }
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);
                return InvalidCredentials();
            }
            return Error.Forbidden(
                "Identity.EmailNotConfirmed",
                "يجب تأكيد البريد الإلكتروني قبل تسجيل الدخول.");
        }

        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        return await ToAppUserAsync(user);
    }

    private static Error InvalidCredentials()
        => Error.Unauthorized("Identity.InvalidCredentials", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");

    private static Error LockedOut()
        => Error.Forbidden("Identity.LockedOut", "تم قفل الحساب مؤقتًا بسبب كثرة المحاولات الفاشلة، يرجى المحاولة لاحقًا.");

    private async Task<Result<AppUserDto>> ToAppUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return Error.Failure("Identity.NoRoleAssigned", "لا يملك هذا الحساب أي صلاحية، يرجى التواصل مع الدعم.");
        }

        return new AppUserDto(user.Id, user.Email!, roles);
    }

    public async Task<Result<Success>> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return InvalidConfirmationToken();
        }
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded ? Result.Success : InvalidConfirmationToken();
    }
    public async Task<Guid?> FindUnconfirmedUserIdAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        return user is not null && !user.EmailConfirmed
            ? user.Id
            : null;
    }

    private static Error InvalidConfirmationToken()
    => Error.Validation(
        "Identity.InvalidConfirmationToken",
        "رابط التأكيد غير صالح أو منتهي الصلاحية.");
    public async Task<PasswordResetCode?> SendResetPasswordCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        user.ResetCodeHash = TokenHashing.Hash(code);
        user.ResetCodeExpiresAt = _timeProvider.GetUtcNow().AddMinutes(ResetCodeLifetimeMinutes);
        user.ResetCodeFailedAttempts = 0;
        var updateResult = await _userManager.UpdateAsync(user);
        return updateResult.Succeeded ? new PasswordResetCode(user.Id, code) : null;
    }

    public async Task<Result<EmailMessage>> BuildConfirmationEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return UserNoLongerExists();
        }
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user!);
        var link = $"{_frontendSettings.ConfirmEmailUrl}" +
                    $"?userId={user!.Id}" +
                    $"&token={Uri.EscapeDataString(token)}";
        var html = EmailTemplates.Render("ConfirmEmail", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ConfirmLink"] = link,
            ["Year"] = _timeProvider.GetUtcNow().Year.ToString(),
        });
        return new EmailMessage(user.Email!, "تأكيد البريد الإلكتروني", html);
    }

    public async Task<Result<EmailMessage>> BuildPasswordResetEmailAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null || user.ResetCodeHash is null)
        {
            return UserNoLongerExists();
        }
        var html = EmailTemplates.Render("ResetPasswordCode", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ResetCode"] = code,
            ["ExpiryMinutes"] = ResetCodeLifetimeMinutes.ToString(),
            ["Year"] = _timeProvider.GetUtcNow().Year.ToString(),
        });
        return new EmailMessage(user.Email, "رمز استعادة كلمة المرور", html);
    }
    private static Error UserNoLongerExists()
        => Error.NotFound("Identity.UserNotFound", "المستخدم لم يعد موجودًا.");

    public async Task<Result<Guid>> ChangePasswordAsync(string email, string password, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user?.ResetCodeHash is null || user.ResetCodeExpiresAt < _timeProvider.GetUtcNow())
        {
            return InvalidResetCode();
        }
        var attemptReserved = await _context.Users
            .Where(u => u.Id == user.Id && u.ResetCodeFailedAttempts < MaxResetCodeAttempts)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.ResetCodeFailedAttempts, u => u.ResetCodeFailedAttempts + 1), cancellationToken);
        if (attemptReserved == 0 || !TokenHashing.Matches(user.ResetCodeHash, code))
        {
            return InvalidResetCode();
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        ClearResetCode(user);
        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return Translate(result);
        }
        return user.Id;
    }

    private static Error InvalidResetCode()
        => Error.Validation("Identity.InvalidResetCode", "رمز الاستعادة غير صحيح أو منتهي الصلاحية.");

    private static void ClearResetCode(ApplicationUser user)
    {
        user.ResetCodeHash = null;
        user.ResetCodeExpiresAt = null;
        user.ResetCodeFailedAttempts = 0;
    }

    public async Task<Result<EmailMessage>> BuildPasswordResetChangedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return UserNoLongerExists();
        }
        var html = EmailTemplates.Render("PasswordChanged", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["Year"] = _timeProvider.GetUtcNow().Year.ToString(),
        });
        return new EmailMessage(user.Email, "تم تغيير كلمة المرور", html);
    }

    public async Task<Result<AppUserDto>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        if (user.BannedAtUtc is not null)
        {
            return ApplicationError.AccountBanned;
        }
        var roles = await _userManager.GetRolesAsync(user);
        return new AppUserDto(user.Id, user.Email, roles);
    }

    public async Task<Result<UserInfoDto>> GetUserInfoAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        return new UserInfoDto(user.Id, user.FullName, user.PhoneNumber, user.ProfileImageUrl);
    }

    public async Task<Result<MyProfileDto>> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        return await ToMyProfileAsync(user);
    }

    public async Task<Result<MyProfileDto>> UpdateProfileAsync(Guid userId, ProfileUpdate update, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }

        user.FullName = update.FullName.Trim();
        user.PhoneNumber = update.PhoneNumber.Trim();
        user.City = update.City.Trim();
        user.Bio = string.IsNullOrWhiteSpace(update.Bio) ? null : update.Bio.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Translate(updateResult);
        }
        return await ToMyProfileAsync(user);
    }

    public async Task<Result<ReplacedProfileImage>> SetProfileImageAsync(Guid userId, StoredFile? image, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }

        var previous = new ReplacedProfileImage(user.ProfileImageUrl, user.ProfileImagePublicId);
        if (image is null && previous.PreviousUrl is null)
        {
            return previous;
        }

        user.ProfileImageUrl = image?.Url;
        user.ProfileImagePublicId = image?.PublicId;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Translate(updateResult);
        }
        return previous;
    }

    public async Task<Result<Success>> SetPasswordAsync(Guid userId, string? currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }

        IdentityResult result;
        if (await _userManager.HasPasswordAsync(user))
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                return CurrentPasswordRequired();
            }
            result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, newPassword);
        }

        if (!result.Succeeded)
        {
            return Translate(result);
        }
        return Result.Success;
    }

    public async Task<Result<Success>> BanUserAsync(Guid userId, string reason, DateTimeOffset bannedAtUtc, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return ApplicationError.CannotBanAdmin;
        }
        if (user.BannedAtUtc is not null)
        {
            return ApplicationError.UserAlreadyBanned;
        }

        user.BannedAtUtc = bannedAtUtc;
        user.BanReason = reason.Trim();
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        var updateResult = await _userManager.UpdateAsync(user);
        return updateResult.Succeeded ? Result.Success : Translate(updateResult);
    }

    public async Task<Result<Success>> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        if (user.BannedAtUtc is null)
        {
            return ApplicationError.UserNotBanned;
        }

        user.BannedAtUtc = null;
        user.BanReason = null;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        var updateResult = await _userManager.UpdateAsync(user);
        return updateResult.Succeeded ? Result.Success : Translate(updateResult);
    }

    public async Task<PaginatedList<AdminUserDto>> GetUsersForAdminAsync(AdminUsersFilter filter, CancellationToken cancellationToken = default)
    {
        var users = _context.Users.AsNoTracking().Where(u => u.DeletedAtUtc == null);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            users = users.Where(u => u.FullName.Contains(term)
                || u.Email!.Contains(term)
                || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }
        users = filter.Status switch
        {
            AdminUserStatus.Active => users.Where(u => u.BannedAtUtc == null && u.EmailConfirmed),
            AdminUserStatus.Banned => users.Where(u => u.BannedAtUtc != null),
            AdminUserStatus.Unconfirmed => users.Where(u => u.BannedAtUtc == null && !u.EmailConfirmed),
            _ => users
        };

        var count = await users.CountAsync(cancellationToken);
        var adminRoleId = await GetAdminRoleIdAsync(cancellationToken);
        var items = await ProjectForAdmin(
                users.OrderByDescending(u => u.CreatedAtUtc)
                    .ThenBy(u => u.Id)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize),
                adminRoleId)
            .ToListAsync(cancellationToken);

        return new PaginatedList<AdminUserDto>
        {
            Items = items,
            PageNumber = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)filter.PageSize)
        };
    }

    public async Task<Result<AdminUserDto>> GetUserForAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var adminRoleId = await GetAdminRoleIdAsync(cancellationToken);
        var user = await ProjectForAdmin(_context.Users.AsNoTracking().Where(u => u.Id == userId && u.DeletedAtUtc == null), adminRoleId)
            .FirstOrDefaultAsync(cancellationToken);
        return user is null ? ApplicationError.UserNotFound : user;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(DateTimeOffset newSinceUtc, CancellationToken cancellationToken = default)
    {
        var adminRoleId = await GetAdminRoleIdAsync(cancellationToken);
        var users = _context.Users
            .AsNoTracking()
            .Where(u => u.DeletedAtUtc == null && !_context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));
        var total = await users.CountAsync(cancellationToken);
        var newUsers = await users.CountAsync(u => u.CreatedAtUtc >= newSinceUtc, cancellationToken);
        var banned = await users.CountAsync(u => u.BannedAtUtc != null, cancellationToken);
        var unconfirmed = await users.CountAsync(u => u.BannedAtUtc == null && !u.EmailConfirmed, cancellationToken);
        return new UserStatsDto(total, newUsers, banned, unconfirmed);
    }

    public Task<bool> IsActiveAccountAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Id == userId && u.DeletedAtUtc == null && u.BannedAtUtc == null, cancellationToken);

    public async Task<Result<DeletedAccount>> DeleteAccountAsync(Guid userId, string? password, DateTimeOffset deletedAtUtc, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return ApplicationError.AdminCannotDeleteAccount;
        }
        var passwordCheck = await CheckPasswordIfSetAsync(user, password);
        if (passwordCheck.IsError)
        {
            return passwordCheck.Errors;
        }

        foreach (var login in await _userManager.GetLoginsAsync(user))
        {
            var removeResult = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            if (!removeResult.Succeeded)
            {
                return Translate(removeResult);
            }
        }

        var profileImagePublicId = user.ProfileImagePublicId;
        var placeholder = $"deleted-{user.Id:N}";
        user.Email = null;
        user.NormalizedEmail = null;
        user.EmailConfirmed = false;
        user.UserName = placeholder;
        user.NormalizedUserName = placeholder.ToUpperInvariant();
        user.PasswordHash = null;
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.FullName = DeletedUserName;
        user.City = string.Empty;
        user.Bio = null;
        user.ProfileImageUrl = null;
        user.ProfileImagePublicId = null;
        ClearResetCode(user);
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.DeletedAtUtc = deletedAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
        return new DeletedAccount(profileImagePublicId);
    }

    public async Task<Result<Success>> RequestEmailChangeAsync(Guid userId, string newEmail, string? password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return ApplicationError.UserNotFound;
        }
        var passwordCheck = await CheckPasswordIfSetAsync(user, password);
        if (passwordCheck.IsError)
        {
            return passwordCheck.Errors;
        }
        if (_userManager.NormalizeEmail(newEmail) == user.NormalizedEmail)
        {
            return ApplicationError.EmailUnchanged;
        }
        if (await _userManager.FindByEmailAsync(newEmail) is not null)
        {
            return ApplicationError.DuplicateEmail;
        }
        return Result.Success;
    }

    public async Task<Result<EmailMessage>> BuildEmailChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return UserNoLongerExists();
        }
        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var link = $"{_frontendSettings.ConfirmEmailChangeUrl}" +
                    $"?userId={user.Id}" +
                    $"&email={Uri.EscapeDataString(newEmail)}" +
                    $"&token={Uri.EscapeDataString(token)}";
        var html = EmailTemplates.Render("ConfirmEmailChange", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ConfirmLink"] = link,
            ["Year"] = _timeProvider.GetUtcNow().Year.ToString(),
        });
        return new EmailMessage(newEmail, "تأكيد البريد الإلكتروني الجديد", html);
    }

    public async Task<Result<EmailChange>> ConfirmEmailChangeAsync(Guid userId, string newEmail, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return InvalidConfirmationToken();
        }
        if (await _userManager.FindByEmailAsync(newEmail) is { } owner && owner.Id != user.Id)
        {
            return ApplicationError.DuplicateEmail;
        }
        var oldEmail = user.Email;
        var changeResult = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!changeResult.Succeeded)
        {
            return changeResult.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? InvalidConfirmationToken()
                : Translate(changeResult);
        }
        var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
        if (!userNameResult.Succeeded)
        {
            return Translate(userNameResult);
        }
        return new EmailChange(oldEmail, newEmail);
    }

    public async Task<Result<EmailMessage>> BuildEmailChangedNoticeAsync(Guid userId, string oldEmail, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return UserNoLongerExists();
        }
        var html = EmailTemplates.Render("EmailChanged", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["NewEmail"] = user.Email,
            ["Year"] = _timeProvider.GetUtcNow().Year.ToString(),
        });
        return new EmailMessage(oldEmail, "تم تغيير البريد الإلكتروني", html);
    }

    private async Task<Result<Success>> CheckPasswordIfSetAsync(ApplicationUser user, string? password)
    {
        if (!await _userManager.HasPasswordAsync(user))
        {
            return Result.Success;
        }
        if (string.IsNullOrEmpty(password))
        {
            return CurrentPasswordRequired();
        }
        return await _userManager.CheckPasswordAsync(user, password)
            ? Result.Success
            : ApplicationError.InvalidPassword;
    }

    public async Task<IReadOnlyList<Guid>> GetAdminIdsAsync(CancellationToken cancellationToken = default)
    {
        var adminRoleId = await GetAdminRoleIdAsync(cancellationToken);
        return await _context.UserRoles
            .Where(ur => ur.RoleId == adminRoleId)
            .Join(_context.Users.Where(u => u.BannedAtUtc == null), ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
            .ToListAsync(cancellationToken);
    }

    private Task<Guid> GetAdminRoleIdAsync(CancellationToken cancellationToken)
        => _context.Roles
            .Where(r => r.Name == Roles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<AdminUserDto> ProjectForAdmin(IQueryable<ApplicationUser> users, Guid adminRoleId)
        => users.Select(u => new AdminUserDto(
            u.Id,
            u.FullName,
            u.Email!,
            u.PhoneNumber,
            u.City,
            u.ProfileImageUrl,
            _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId),
            u.EmailConfirmed,
            u.BannedAtUtc,
            u.BanReason,
            u.CreatedAtUtc,
            _context.Properties.Count(p => p.SellerId == u.Id),
            _context.Properties.Count(p => p.SellerId == u.Id
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive),
            _context.Reports.Count(r => r.Status == ReportStatus.Pending
                && _context.Properties.Any(p => p.Id == r.PropertyId && p.SellerId == u.Id))));

    public async Task<IReadOnlyDictionary<Guid, UserInfoDto>> GetUsersInfoAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserInfoDto>();
        }
        return await _userManager.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserInfoDto(u.Id, u.FullName, u.PhoneNumber, u.ProfileImageUrl))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    public async Task<Result<SellerInfo>> GetSellerInfoAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(sellerId.ToString());
        if (user?.Email is null || user.BannedAtUtc is not null || await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return ApplicationError.SellerNotFound;
        }
        return new SellerInfo(
            user.Id,
            user.FullName,
            user.ProfileImageUrl,
            user.City,
            user.Bio,
            user.CreatedAtUtc);
    }

    private static Error CurrentPasswordRequired()
        => Error.Validation("Identity.CurrentPasswordRequired", "كلمة المرور الحالية مطلوبة.");

    private async Task<MyProfileDto> ToMyProfileAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var hasPassword = await _userManager.HasPasswordAsync(user);
        return new MyProfileDto(
            user.Id,
            user.FullName,
            user.Email!,
            user.PhoneNumber,
            user.City,
            user.Bio,
            user.ProfileImageUrl,
            [.. roles],
            hasPassword,
            user.CreatedAtUtc);
    }

    public async Task<Result<AppUserDto>> SignInWithGoogleAsync(GoogleUser googleUser, string? phoneNumber, string? city, CancellationToken cancellationToken = default)
    {
        if (!googleUser.EmailVerified)
        {
            return Error.Forbidden(
                "Identity.GoogleEmailNotVerified",
                "البريد الإلكتروني في حساب Google غير مؤكد، يرجى تأكيده ثم المحاولة مجددًا.");
        }

        var user = await _userManager.FindByLoginAsync(GoogleLoginProvider, googleUser.Subject);

        if (user is null)
        {
            user = await _userManager.FindByEmailAsync(googleUser.Email);
            if (user?.BannedAtUtc is not null)
            {
                return ApplicationError.AccountBanned;
            }
            if (user is not null)
            {
                var linkResult = await LinkGoogleLoginAsync(user, googleUser);
                if (linkResult.IsError)
                {
                    return linkResult.Errors;
                }
            }
        }

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(city))
            {
                return Error.Validation(
                    "Identity.GoogleRegistrationIncomplete",
                    "أكمل رقم الهاتف والمدينة لإنشاء حسابك.");
            }

            var createResult = await CreateGoogleUserAsync(googleUser, phoneNumber, city);
            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            user = createResult.Value;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return user.BannedAtUtc is not null ? ApplicationError.AccountBanned : LockedOut();
        }

        return await ToAppUserAsync(user);
    }

    private async Task<Result<Success>> LinkGoogleLoginAsync(ApplicationUser user, GoogleUser googleUser)
    {
        var addLoginResult = await _userManager.AddLoginAsync(
            user,
            new UserLoginInfo(GoogleLoginProvider, googleUser.Subject, GoogleLoginProvider));
        if (!addLoginResult.Succeeded)
        {
            return Translate(addLoginResult);
        }
        if (!user.EmailConfirmed)
        {
            if (await _userManager.HasPasswordAsync(user))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    return Translate(removePasswordResult);
                }
            }
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Translate(updateResult);
            }
        }
        return Result.Success;
    }

    private async Task<Result<ApplicationUser>> CreateGoogleUserAsync(GoogleUser googleUser, string phoneNumber, string city)
    {
        var fullName = googleUser.FullName.Length > MaxFullNameLength
            ? googleUser.FullName[..MaxFullNameLength]
            : googleUser.FullName;
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = googleUser.Email,
            UserName = googleUser.Email,
            EmailConfirmed = true,
            FullName = fullName,
            PhoneNumber = phoneNumber.Trim(),
            City = city.Trim(),
            ProfileImageUrl = googleUser.PictureUrl is { Length: <= MaxProfileImageUrlLength } pictureUrl ? pictureUrl : null,
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return Translate(createResult);
        }
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
            return Translate(roleResult);
        }
        var addLoginResult = await _userManager.AddLoginAsync(
            user,
            new UserLoginInfo(GoogleLoginProvider, googleUser.Subject, GoogleLoginProvider));
        if (!addLoginResult.Succeeded)
        {
            return Translate(addLoginResult);
        }
        return user;
    }
}
