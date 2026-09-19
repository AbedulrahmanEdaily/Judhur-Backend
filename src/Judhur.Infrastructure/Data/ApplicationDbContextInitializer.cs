using Judhur.Domain.Common;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
            await _context.Database.EnsureCreatedAsync();
        }catch(Exception e)
        {
            _logger.LogError(e,"An error occurred while initializing the database.");
            throw;
        }
    }
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }catch(Exception e)
        {
            _logger.LogError(e, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        var roles = new[] {Roles.User,Roles.Admin};
        foreach(var role in roles)
        {
            if(!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
        var admin = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = "admin@judhur.com",
            FullName = "Abdulrahman Edaily",
            UserName = "Abed",
            EmailConfirmed = true,
            City = "Nablus"
        };
        if (_userManager.Users.All(u => u.Email != admin.Email))
        {
            await _userManager.CreateAsync(admin,"admin");
            await _userManager.AddToRoleAsync(admin,Roles.Admin);
        }
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = "user@judhur.com",
            FullName = "Faheem Hasson",
            UserName = "Faheem",
            EmailConfirmed = true,
            City = "Nablus"
        };
        if (_userManager.Users.All(u => u.Email != user.Email))
        {
            await _userManager.CreateAsync(user,"user");
            await _userManager.AddToRoleAsync(user,Roles.User);
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