using System.Reflection;
using FinanceTracker.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Application;

/// <summary>
/// Dependency Injection registration for the Application layer.
/// 
/// Each layer is responsible for registering its OWN services.
/// The API layer calls this method at startup. This keeps each layer
/// self-contained — the API doesn't need to know the internal details
/// of what the Application layer registers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register all MediatR handlers found in this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register all FluentValidation validators found in this assembly
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviors (runs in order for every MediatR request)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
