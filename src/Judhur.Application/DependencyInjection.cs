using FluentValidation;

using Judhur.Application.Common.Behaviors;

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddMediatR(options =>
        {
            options.LicenseKey = configuration["MediatR:LicenseKey"];
            options.RegisterServicesFromAssembly(applicationAssembly);
            options.AddOpenRequestPreProcessor(typeof(LoggingBehavior<>));
            options.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
            options.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            options.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services;
    }
}
