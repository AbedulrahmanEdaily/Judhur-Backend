using System.Reflection;
using FluentValidation;
using Judhur.Application.Common.Behaviors;
namespace Microsoft.Extensions.DependencyInjection;


public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        return services;
    }
}