using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Common;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Judhur.Api.IntegrationTests.Admin;

public static class AdminAccounts
{
    public static async Task<Guid> CreateExtraAdminAsync(JudhurApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = Scenario.NewEmail("extra-admin");
        var admin = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            FullName = "مشرف إضافي",
            City = "رام الله",
            PhoneNumber = Scenario.NewPhone(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var created = await users.CreateAsync(admin, Scenario.DefaultPassword);
        Assert.True(created.Succeeded, string.Join(" | ", created.Errors.Select(e => e.Code)));
        var role = await users.AddToRoleAsync(admin, Roles.Admin);
        Assert.True(role.Succeeded, string.Join(" | ", role.Errors.Select(e => e.Code)));
        return admin.Id;
    }
}
