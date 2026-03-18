using FinanceTracker.Domain.Interfaces;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Identity;
using FinanceTracker.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Infrastructure.Services;

namespace FinanceTracker.Infrastructure;

/// <summary>
/// Infrastructure layer registers its own services — EF Core, Identity, Repositories.
/// The API layer just calls services.AddInfrastructure(configuration) at startup.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EF Core with SQL Server
        services.AddDbContext<ApplicationDbContext>(options =>
           options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Register ASP.NET Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            // Password requirements — adjust for your needs
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout after 5 failed attempts
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Bind JWT settings from appsettings.json
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Register AuthService
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
