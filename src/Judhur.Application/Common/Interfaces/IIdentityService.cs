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
}
