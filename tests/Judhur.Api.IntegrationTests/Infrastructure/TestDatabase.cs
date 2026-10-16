using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class TestDatabase : IAsyncDisposable
{
    public const string ServerVariable = "JUDHUR_TEST_SQL";

    private readonly MsSqlContainer? _container;

    private TestDatabase(MsSqlContainer? container, string serverConnectionString)
    {
        _container = container;
        var builder = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = $"JudhurTests_{Guid.NewGuid():N}",
            TrustServerCertificate = true,
            MultipleActiveResultSets = false
        };
        ConnectionString = builder.ConnectionString;
    }

    public string ConnectionString { get; }

    public static async Task<TestDatabase> StartAsync()
    {
        if (Environment.GetEnvironmentVariable(ServerVariable) is { Length: > 0 } server)
        {
            return new TestDatabase(null, server);
        }

        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await container.StartAsync();
        return new TestDatabase(container, container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        var builder = new SqlConnectionStringBuilder(ConnectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
        await command.ExecuteNonQueryAsync();
    }
}
