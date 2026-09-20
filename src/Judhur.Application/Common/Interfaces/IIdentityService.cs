using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Success>> CreateNewUserAsync(
        NewUserRegistration registration,
        CancellationToken cancellationToken = default);
    Task<Result<AppUserDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
