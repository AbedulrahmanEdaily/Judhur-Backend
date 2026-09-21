using Judhur.Domain.Common;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Data;

public class ApplicationDbContextInitializer(ILogger<ApplicationDbContextInitializer> logger, AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while initializing the database.");
            throw;
        }
    }
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        var roles = new[] { Roles.User, Roles.Admin };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole<Guid>(role));

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        roleResult.Errors.Select(e =>
                            $"{e.Code}: {e.Description}"));

                    _logger.LogError(
                        "Failed to create role {Role}. Errors: {Errors}",
                        role,
                        errors);

                    continue;
                }

                _logger.LogInformation(
                    "Role {Role} created successfully.",
                    role);
            }
        }

        // Admin

        var admin = await _userManager.FindByEmailAsync(
            "admin@judhur.com");

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = "admin@judhur.com",
                FullName = "Abdulrahman Edaily",
                UserName = "Abed",
                EmailConfirmed = true,
                City = "Nablus"
            };

            var createResult = await _userManager.CreateAsync(
                admin,
                "Admin@12345");

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    " | ",
                    createResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                _logger.LogError(
                    "Failed to create admin user {Email}. Errors: {Errors}",
                    admin.Email,
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "Admin user {Email} created successfully.",
                    admin.Email);
            }
        }

        if (admin.Id != Guid.Empty &&
            await _userManager.IsInRoleAsync(admin, Roles.Admin) == false)
        {
            var roleResult = await _userManager.AddToRoleAsync(
                admin,
                Roles.Admin);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    " | ",
                    roleResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                _logger.LogError(
                    "Failed to assign role {Role} to admin {Email}. Errors: {Errors}",
                    Roles.Admin,
                    admin.Email,
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "Role {Role} assigned to admin {Email}.",
                    Roles.Admin,
                    admin.Email);
            }
        }


        // Normal User

        var user = await _userManager.FindByEmailAsync(
            "user@judhur.com");

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = "user@judhur.com",
                FullName = "Faheem Hasson",
                UserName = "Faheem",
                EmailConfirmed = true,
                City = "Nablus"
            };

            var createResult = await _userManager.CreateAsync(
                user,
                "User@12345");

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    " | ",
                    createResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                _logger.LogError(
                    "Failed to create user {Email}. Errors: {Errors}",
                    user.Email,
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "User {Email} created successfully.",
                    user.Email);
            }
        }

        if (user.Id != Guid.Empty &&
            await _userManager.IsInRoleAsync(user, Roles.User) == false)
        {
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                Roles.User);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    " | ",
                    roleResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                _logger.LogError(
                    "Failed to assign role {Role} to user {Email}. Errors: {Errors}",
                    Roles.User,
                    user.Email,
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "Role {Role} assigned to user {Email}.",
                    Roles.User,
                    user.Email);
            }
        }
    }
}
    public static class InitializerExtensions
    {
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
            await initializer.InitializeAsync();
            await initializer.SeedAsync();
        }
    }