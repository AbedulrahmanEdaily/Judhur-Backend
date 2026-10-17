using System.Security.Claims;
using System.Text;

using CloudinaryDotNet;

using Judhur.Application.Common.Interfaces;
using Judhur.Infrastructure.Data;
using Judhur.Infrastructure.CloudinaryStorage;
using Judhur.Infrastructure.Common;
using Judhur.Infrastructure.Data.Interceptors;
using Judhur.Infrastructure.Email;
using Judhur.Infrastructure.Identity;
using Judhur.Infrastructure.Identity.GoogleLogin;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDeferredDispatcher, DeferredDispatcher>();

        services.AddPersistence(configuration)
                .AddEmail(configuration)
                .AddIdentityServices(configuration)
                .AddCaching()
                .AddCloudinary(configuration);

        return services;
    }

    private static IServiceCollection AddCloudinary(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudinarySettings = CloudinarySettings.Bind(configuration);
        var account = new Account
        {
            ApiKey = cloudinarySettings.ApiKey,
            Cloud = cloudinarySettings.CloudName,
            ApiSecret = cloudinarySettings.ApiSecret
        };
        var cloudinary = new Cloudinary(account);
        services.AddSingleton<ICloudinary>(cloudinary);
        services.AddScoped<IFileStorage, CloudinaryFileStorage>();
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .SetApplicationName("Judhur");
        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(FrontendSettings.Bind(configuration));
        services.AddSingleton(EmailSettings.Bind(configuration));
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddSingleton<ChannelEmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<ChannelEmailQueue>());
        services.AddHostedService<EmailBackgroundService>();
        return services;
    }
    private static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = JwtSettings.Bind(configuration);
        services.AddSingleton(jwtSettings);

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager()
        .AddErrorDescriber<ArabicIdentityErrorDescriber>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                    {
                        return;
                    }
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var account = await dbContext.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.SecurityStamp, u.BannedAtUtc, u.DeletedAtUtc })
                        .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                    if (account is null || account.BannedAtUtc is not null || account.DeletedAtUtc is not null)
                    {
                        context.Fail("Account is no longer active.");
                        return;
                    }
                    if (context.Principal?.FindFirstValue(TokenProvider.SecurityStampClaim) != account.SecurityStamp)
                    {
                        context.Fail("Session ended by a credential change.");
                    }
                }
            };
        });

        services.AddAuthorization();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddSingleton(GoogleSettings.Bind(configuration));
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
            };
        });

        return services;
    }
}
