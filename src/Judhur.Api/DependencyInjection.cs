using System.Text.Json.Serialization;

using Judhur.Api;
using Judhur.Api.Services;
using Judhur.Application.Common.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddCustomProblemDetails()
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddApiDocumentation()
                .AddIdentityInfrastructure();
        return services;
    }

    // Serves the document Program.cs points SwaggerUI at: /openapi/v1.json.
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }

    public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options => options
            .JsonSerializerOptions
            .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
        return services;
    }
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
            context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
        });
        return services;
    }
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
