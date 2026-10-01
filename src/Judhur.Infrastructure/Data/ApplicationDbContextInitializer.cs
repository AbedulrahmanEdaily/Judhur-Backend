using Judhur.Domain.Common;
using Judhur.Infrastructure.Data.Seed;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Data;

public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IHostEnvironment environment,
    TimeProvider timeProvider)
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly IHostEnvironment _environment = environment;
    private readonly TimeProvider _timeProvider = timeProvider;

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
                City = "Nablus",
                CreatedAtUtc = _timeProvider.GetUtcNow()
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
                PhoneNumber = "0599123456",
                EmailConfirmed = true,
                City = "Nablus",
                CreatedAtUtc = _timeProvider.GetUtcNow()
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

        if (!_environment.IsDevelopment())
        {
            return;
        }

        await SeedDevelopmentDataAsync(admin, user);
    }

    private async Task SeedDevelopmentDataAsync(ApplicationUser admin, ApplicationUser user)
    {
        if (string.IsNullOrEmpty(user.PhoneNumber))
        {
            user.PhoneNumber = "0599123456";
            await _userManager.UpdateAsync(user);
        }

        var layla = await EnsureSellerAsync("layla@judhur.com", "ليلى منصور", "layla", "0598765432", "رام الله");
        var samer = await EnsureSellerAsync("samer@judhur.com", "سامر عودة", "samer", "0569876543", "الخليل");
        if (layla is null || samer is null)
        {
            _logger.LogError("Development sellers could not be created, skipping property seeding");
            return;
        }

        await PropertySeeder.SeedAsync(
            _context,
            admin.Id,
            [user.Id, layla.Id, samer.Id],
            user.Id,
            _timeProvider.GetUtcNow(),
            _logger);
    }

    private async Task<ApplicationUser?> EnsureSellerAsync(string email, string fullName, string userName, string phoneNumber, string city)
    {
        var seller = await _userManager.FindByEmailAsync(email);
        if (seller is null)
        {
            seller = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                FullName = fullName,
                UserName = userName,
                PhoneNumber = phoneNumber,
                EmailConfirmed = true,
                City = city,
                CreatedAtUtc = _timeProvider.GetUtcNow()
            };

            var createResult = await _userManager.CreateAsync(seller, "User@12345");
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to create seller {Email}. Errors: {Errors}",
                    email,
                    string.Join(" | ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
                return null;
            }

            _logger.LogInformation("Seller {Email} created successfully.", email);
        }

        if (!await _userManager.IsInRoleAsync(seller, Roles.User))
        {
            var roleResult = await _userManager.AddToRoleAsync(seller, Roles.User);
            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to assign role {Role} to seller {Email}. Errors: {Errors}",
                    Roles.User,
                    email,
                    string.Join(" | ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
                return null;
            }
        }

        return seller;
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