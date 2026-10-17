using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Judhur.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class SchemaTests(JudhurApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Database_AfterStartup_HasEveryMigrationApplied()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync(Ct);
        var applied = await db.Database.GetAppliedMigrationsAsync(Ct);

        Assert.Empty(pending);
        Assert.Equal(db.Database.GetMigrations(), applied);
    }

    [Fact]
    public void Model_ComparedWithLatestMigration_HasNoPendingChanges()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(db.Database.HasPendingModelChanges());
    }
}
