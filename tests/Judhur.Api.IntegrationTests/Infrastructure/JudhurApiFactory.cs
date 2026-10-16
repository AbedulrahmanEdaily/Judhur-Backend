using Judhur.Application.Common.Interfaces;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class JudhurApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@judhur.test";
    public const string AdminPassword = "Admin#Pass2026";

    private TestDatabase? _database;

    public FakeEmailSender Emails { get; } = new();

    public FakeFileStorage Files { get; } = new();

    public string ConnectionString => _database?.ConnectionString
        ?? throw new InvalidOperationException("The test database has not been started.");

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.StartAsync();
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("JwtSettings:Secret", "integration-tests-signing-key-that-is-long-enough-0123456789");
        builder.UseSetting("JwtSettings:Issuer", "judhur-tests");
        builder.UseSetting("JwtSettings:Audience", "judhur-tests");
        builder.UseSetting("JwtSettings:TokenExpirationInMinutes", "15");
        builder.UseSetting("JwtSettings:RefreshTokenExpirationInDays", "7");
        builder.UseSetting("CloudinarySettings:CloudName", "tests");
        builder.UseSetting("CloudinarySettings:ApiKey", "tests");
        builder.UseSetting("CloudinarySettings:ApiSecret", "tests");
        builder.UseSetting("EmailPassword", "tests");
        builder.UseSetting("Google:ClientId", "tests.apps.googleusercontent.com");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        builder.UseSetting("Frontend:ConfirmEmailUrl", "http://localhost:5173/confirm-email");
        builder.UseSetting("Frontend:ConfirmEmailChangeUrl", "http://localhost:5173/confirm-email-change");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
        builder.UseSetting("AdminSeed:FullName", "مدير الاختبار");
        builder.UseSetting("AdminSeed:City", "رام الله");
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage>(Files);
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }
}
