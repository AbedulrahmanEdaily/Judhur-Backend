using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Common;
using Judhur.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Judhur.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class IdentityPersistenceTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_StoresOnlyTheHashOfTheRefreshToken()
    {
        var user = await _scenario.NewUserAsync("tokens");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = Assert.Single(await db.RefreshTokens.AsNoTracking().Where(t => t.UserId == user.Id).ToListAsync(Ct));
        Assert.NotEqual(user.Tokens.RefreshToken, stored.TokenHash);
        Assert.Equal(TokenHashing.Hash(user.Tokens.RefreshToken), stored.TokenHash);
        Assert.Equal(TokenHashing.HashLength, stored.TokenHash.Length);
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.TokenHash == user.Tokens.RefreshToken, Ct));
        Assert.True(stored.ExpiresOnUtc > DateTimeOffset.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task Login_Again_ReplacesTheStoredRefreshToken()
    {
        var user = await _scenario.NewUserAsync("tokens");

        var second = await _scenario.LoginAsync(user.Email, user.Password);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = Assert.Single(await db.RefreshTokens.AsNoTracking().Where(t => t.UserId == user.Id).ToListAsync(Ct));
        Assert.Equal(TokenHashing.Hash(second.RefreshToken), stored.TokenHash);
        Assert.NotEqual(TokenHashing.Hash(user.Tokens.RefreshToken), stored.TokenHash);
    }
}
