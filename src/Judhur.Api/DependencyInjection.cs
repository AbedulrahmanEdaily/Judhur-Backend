using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using Asp.Versioning;

using Judhur.Api;
using Judhur.Api.Exceptions;
using Judhur.Api.HealthChecks;
using Judhur.Api.Hubs;
using Judhur.Api.OpenApi.Transformer;
using Judhur.Api.Services;
using Judhur.Application.Common.Interfaces;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCustomProblemDetails()
                .AddCustomApiVersioning()
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddApiDocumentation()
                .AddConfiguredCors(configuration)
                .AddForwardedHeadersSupport(configuration)
                .AddRateLimiting(configuration)
                .AddIdentityInfrastructure()
                .AddRealtime();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        return services;
    }

    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        return services;
    }

    public static WebApplication MapRealtime(this WebApplication app)
    {
        app.MapHub<ChatHub>(ChatHub.Route, options => options.CloseOnAuthenticationExpiration = true);
        return app;
    }
    public static IServiceCollection AddForwardedHeadersSupport(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var network in (configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? []).Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });
        return services;
    }

    public static WebApplication UseForwardedHeadersIfEnabled(this WebApplication app, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            app.UseForwardedHeaders();
        }
        return app;
    }

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is { Length: > 0 } origins
            ? origins
            : throw new InvalidOperationException("Configuration value 'Cors:AllowedOrigins' is missing.");
        services.AddCors(options => options.AddPolicy("frontend"
            ,
            policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));
        return services;
    }
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.ConfigureAll<OpenApiOptions>(options =>
        {
            options.AddDocumentTransformer<VersionInfoTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
        });
        return services;
    }

    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        })
        .AddOpenApi();
        return services;
    }
    public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        return services;
    }
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("RateLimiting:Enabled", true);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/hubs") || httpContext.Request.Path.StartsWithSegments("/health"))
                {
                    return RateLimitPartition.GetNoLimiter("unlimited");
                }

                return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId
                    ? FixedWindow(enabled, $"user:{userId}", 300, TimeSpan.FromMinutes(1))
                    : FixedWindow(enabled, $"ip:{ClientIp(httpContext)}", 200, TimeSpan.FromMinutes(1));
            });

            options.AddPolicy(RateLimitPolicies.ResendConfirmation, httpContext => FixedWindow(enabled, ClientIp(httpContext), 3, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.SendResetPasswordCode, httpContext => FixedWindow(enabled, ClientIp(httpContext), 3, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.ChangePassword, httpContext => FixedWindow(enabled, ClientIp(httpContext), 5, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.GoogleLogin, httpContext => FixedWindow(enabled, ClientIp(httpContext), 10, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.Login, httpContext => FixedWindow(enabled, ClientIp(httpContext), 10, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimitPolicies.Register, httpContext => FixedWindow(enabled, ClientIp(httpContext), 5, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.SetMyPassword, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 5, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.CreateReport, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 10, TimeSpan.FromHours(1)));
            options.AddPolicy(RateLimitPolicies.CreateReview, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 5, TimeSpan.FromHours(1)));
            options.AddPolicy(RateLimitPolicies.DeleteAccount, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 5, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.ChangeEmail, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 3, TimeSpan.FromMinutes(15)));
            options.AddPolicy(RateLimitPolicies.SendMessage, httpContext => FixedWindow(enabled, UserOrClientIp(httpContext), 30, TimeSpan.FromMinutes(1)));
        });
        return services;
    }

    private static RateLimitPartition<string> FixedWindow(bool enabled, string partitionKey, int permitLimit, TimeSpan window)
        => enabled
            ? RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
            })
            : RateLimitPartition.GetNoLimiter(partitionKey);

    private static string ClientIp(HttpContext httpContext)
        => httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrClientIp(HttpContext httpContext)
        => httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ClientIp(httpContext);

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUser, CurrentUser>();
        services.AddHttpContextAccessor();
        return services;
    }
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = (context) =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;

            var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
            var title = context.ProblemDetails.Title;
            if (string.IsNullOrEmpty(title)
                || title == ReasonPhrases.GetReasonPhrase(status)
                || title == "One or more validation errors occurred.")
            {
                context.ProblemDetails.Title = status switch
                {
                    400 => "البيانات المدخلة غير صالحة.",
                    401 => "يجب تسجيل الدخول لتنفيذ هذا الإجراء.",
                    403 => "ليست لديك صلاحية لتنفيذ هذا الإجراء.",
                    404 => "المورد المطلوب غير موجود.",
                    405 => "طريقة الطلب غير مدعومة.",
                    409 => "يوجد تعارض مع بيانات موجودة.",
                    415 => "نوع المحتوى غير مدعوم.",
                    429 => "لقد تجاوزت الحد المسموح من المحاولات، يرجى المحاولة لاحقًا.",
                    500 => "حدث خطأ غير متوقع.",
                    _ => title,
                };
            }
        });
        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<DbUpdateExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("frontend");
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        return app;
    }
}
