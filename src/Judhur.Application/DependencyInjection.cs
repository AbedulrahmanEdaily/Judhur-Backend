using FluentValidation;

using Judhur.Application.Common.Behaviors;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(applicationAssembly);
            options.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
            options.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            options.AddRequestPreProcessor(typeof(LoggingBehavior<>));
        });
        return services;
    }
}