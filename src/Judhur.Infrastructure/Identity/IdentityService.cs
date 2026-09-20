using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

using Microsoft.AspNetCore.Identity;

namespace Judhur.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    public async Task<Result<Success>> CreateNewUserAsync(
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
        return Result.Success;
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
                "This account is temporarily locked after too many failed attempts. Try again later.");
        }

        if (signInResult.IsNotAllowed)
        {
            return Error.Forbidden(
                "Identity.EmailNotConfirmed",
                "Confirm your email address before signing in.");
        }

        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return Error.Failure("Identity.NoRoleAssigned", "The account has no role assigned.");
        }

        return new AppUserDto(user.Id, user.Email!, roles);
    }

    private static Error InvalidCredentials()
        => Error.Unauthorized("Identity.InvalidCredentials", "Invalid email or password.");
}
