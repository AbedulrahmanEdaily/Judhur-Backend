using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Users;

using Microsoft.AspNetCore.Identity;

namespace Judhur.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AppUserDto>> CreateNewUserAsync(
        Guid userId,
        NewUserRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            Email = registration.Email,
            PhoneNumber = registration.PhoneNumber,
            UserName = registration.UserName,
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
        var roles = await _userManager.GetRolesAsync(user);
        return new AppUserDto(user.Id, user.Email!, roles);
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
}
