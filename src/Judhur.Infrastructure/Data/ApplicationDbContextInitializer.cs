using Judhur.Domain.Common;
using Judhur.Infrastructure.Data.Seed;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    IConfiguration configuration,
    TimeProvider timeProvider,
    DemoDataSeeder demoDataSeeder)
{
    private const string DevelopmentAdminEmail = "admin@judhur.com";
    private const string DevelopmentAdminPassword = "Admin@12345";

    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly IHostEnvironment _environment = environment;
    private readonly IConfiguration _configuration = configuration;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly DemoDataSeeder _demoDataSeeder = demoDataSeeder;

    public async Task InitializeAsync()
    {
        if (!_configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            _logger.LogInformation("Applying migrations on startup is disabled; the database schema must be updated by the deployment");
            return;
        }
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
        foreach (var role in new[] { Roles.User, Roles.Admin })
        {
            await EnsureRoleAsync(role);
        }

        var adminId = _environment.IsDevelopment()
            ? await EnsureDevelopmentAdminAsync()
            : await EnsureConfiguredAdminAsync();
        if (adminId is null)
        {
            return;
        }

        if (_configuration.GetValue("Database:SeedDemoData", _environment.IsDevelopment()))
        {
            await _demoDataSeeder.SeedAsync(adminId.Value);
        }
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (await _roleManager.RoleExistsAsync(role))
        {
            return;
        }
        var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create the role {role}: {string.Join(" | ", result.Errors.Select(e => e.Code))}");
        }
        _logger.LogInformation("Role {Role} created", role);
    }

    private async Task<Guid?> EnsureDevelopmentAdminAsync()
    {
        var admin = await _userManager.FindByEmailAsync(DevelopmentAdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = DevelopmentAdminEmail,
                UserName = DevelopmentAdminEmail,
                FullName = "عبد الرحمن عديلي",
                City = "نابلس",
                EmailConfirmed = true,
                CreatedAtUtc = _timeProvider.GetUtcNow().AddDays(-365)
            };
            var createResult = await _userManager.CreateAsync(admin, DevelopmentAdminPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Development admin {Email} could not be created: {Errors}",
                    DevelopmentAdminEmail,
                    string.Join(" | ", createResult.Errors.Select(e => e.Code)));
                return null;
            }
            _logger.LogInformation("Development admin {Email} created", DevelopmentAdminEmail);
        }

        await EnsureAdminRoleAsync(admin);
        return admin.Id;
    }

    private async Task<Guid?> EnsureConfiguredAdminAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0)
        {
            return admins.OrderBy(a => a.CreatedAtUtc).First().Id;
        }

        var settings = AdminSeedSettings.Bind(_configuration);
        var admin = await _userManager.FindByEmailAsync(settings.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = settings.Email,
                UserName = settings.Email,
                FullName = settings.FullName,
                City = settings.City,
                EmailConfirmed = true,
                CreatedAtUtc = _timeProvider.GetUtcNow()
            };

            var createResult = await _userManager.CreateAsync(admin, settings.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create the admin account: {string.Join(" | ", createResult.Errors.Select(e => e.Code))}");
            }
        }
        else if (!admin.EmailConfirmed || !await _userManager.CheckPasswordAsync(admin, settings.Password))
        {
            _logger.LogCritical("Admin seed skipped: account {Email} already exists but is unconfirmed or its password does not match AdminSeed:Password", settings.Email);
            return null;
        }

        await EnsureAdminRoleAsync(admin);
        _logger.LogInformation("Admin account {Email} set up from configuration", admin.Email);
        return admin.Id;
    }

    private async Task EnsureAdminRoleAsync(ApplicationUser admin)
    {
        if (await _userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            return;
        }
        var result = await _userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign the admin role to {admin.Email}: {string.Join(" | ", result.Errors.Select(e => e.Code))}");
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
