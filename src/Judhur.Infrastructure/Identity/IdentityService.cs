using System.Security.Cryptography;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Infrastructure.Email;

using Microsoft.AspNetCore.Identity;

namespace Judhur.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, FrontendSettings frontendSettings,
    TimeProvider timeProvider) : IIdentityService
{
    private const int ResetCodeLifetimeMinutes = 5;
    private const string GoogleLoginProvider = "Google";
    private const int MaxFullNameLength = 150;
    private const int MaxProfileImageUrlLength = 500;

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly FrontendSettings _frontendSettings = frontendSettings;
    private readonly TimeProvider _timeProvider = timeProvider;

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
            return LockedOut();
        }

        if (signInResult.IsNotAllowed)
        {
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
    public async Task<Guid?> SendResetPasswordCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        user.ResetCode = code;
        user.ResetCodeExpiresAt = _timeProvider.GetUtcNow().AddMinutes(ResetCodeLifetimeMinutes);
        var updateResult = await _userManager.UpdateAsync(user);
        return updateResult.Succeeded ? user.Id : null;
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

    public async Task<Result<EmailMessage>> BuildPasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null || user.ResetCode is null)
        {
            return UserNoLongerExists();
        }
        var html = EmailTemplates.Render("ResetPasswordCode", new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ResetCode"] = user.ResetCode,
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
        if (user is null || user.ResetCode != code || user.ResetCodeExpiresAt < _timeProvider.GetUtcNow())
        {
            return Error.Validation("Identity.InvalidResetCode", "رمز الاستعادة غير صحيح أو منتهي الصلاحية.");
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return Translate(result);
        }
        user.ResetCode = null;
        user.ResetCodeExpiresAt = null;
        await _userManager.UpdateAsync(user);
        return user.Id;
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
            return LockedOut();
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
