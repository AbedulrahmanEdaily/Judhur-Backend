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
            UserName = registration.UserName,
            City = registration.City,
            FullName = registration.FullName
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
        => [.. result.Errors.Select(error => error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName"
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
            return Error.Forbidden(
                "Identity.LockedOut",
                "تم قفل الحساب مؤقتًا بسبب كثرة المحاولات الفاشلة، يرجى المحاولة لاحقًا.");
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

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return Error.Failure("Identity.NoRoleAssigned", "لا يملك هذا الحساب أي صلاحية، يرجى التواصل مع الدعم.");
        }

        return new AppUserDto(user.Id, user.Email!, roles);
    }

    private static Error InvalidCredentials()
        => Error.Unauthorized("Identity.InvalidCredentials", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");

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
        user.ResetCodeExpiresAt = _timeProvider.GetUtcNow().AddMinutes(5);
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
        return new EmailMessage(
            user.Email!,
            "تأكيد البريد الإلكتروني",
            $"""<div dir="rtl"><p>مرحبًا بك في منصة جذور.</p><p><a href="{link}">اضغط هنا لتأكيد بريدك الإلكتروني</a></p></div>""");
    }

    public async Task<Result<EmailMessage>> BuildPasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null || user.ResetCode is null)
        {
            return UserNoLongerExists();
        }
        return new EmailMessage(
            user.Email,
            "رمز استعادة كلمة المرور",
            $"""<div dir="rtl"><p>رمز استعادة كلمة المرور الخاص بك هو <strong>{user.ResetCode}</strong>، وتنتهي صلاحيته خلال 5 دقائق.</p></div>""");
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
        return new EmailMessage(user.Email, "تم تغيير كلمة المرور", """<div dir="rtl"><p>تم تغيير كلمة المرور الخاصة بحسابك.</p><p>إن لم تقم بهذا التغيير، يرجى التواصل معنا فورًا.</p></div>""");
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
}
