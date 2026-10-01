using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Guid>> CreateNewUserAsync(
            NewUserRegistration registration,
            CancellationToken cancellationToken = default);

    Task<Result<AppUserDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<Success>> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindUnconfirmedUserIdAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Guid?> SendResetPasswordCodeAsync(
        string email,
        CancellationToken cancellationToken = default);
    Task<Result<Guid>> ChangePasswordAsync(
        string email,
        string password,
        string code, CancellationToken cancellationToken = default);

    Task<Result<EmailMessage>> BuildConfirmationEmailAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<EmailMessage>> BuildPasswordResetEmailAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<Result<EmailMessage>> BuildPasswordResetChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<Result<AppUserDto>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserInfoDto>> GetUserInfoAsync(string userId, CancellationToken ct = default);
    Task<Result<MyProfileDto>> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<MyProfileDto>> UpdateProfileAsync(Guid userId, ProfileUpdate update, CancellationToken cancellationToken = default);
    Task<Result<ReplacedProfileImage>> SetProfileImageAsync(Guid userId, StoredFile? image, CancellationToken cancellationToken = default);
    Task<Result<AppUserDto>> SignInWithGoogleAsync(GoogleUser googleUser, string? phoneNumber, string? city, CancellationToken cancellationToken = default);
}
