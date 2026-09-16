using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<AppUserDto>> CreateNewUserAsync(
        Guid userId,
        NewUserRegistration registration,
        CancellationToken cancellationToken = default);
}
