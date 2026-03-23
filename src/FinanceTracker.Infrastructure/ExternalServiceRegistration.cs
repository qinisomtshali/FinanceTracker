using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.ExternalServices;

namespace FinanceTracker.Infrastructure;

public static class ExternalServiceRegistration
{
    /// <summary>
    /// Register all external API services with HttpClient factory
    /// Add this call in Program.cs: builder.Services.AddExternalServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Stock Market - Alpha Vantage
        services.AddHttpClient<IStockMarketService, AlphaVantageStockService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(); // Requires Microsoft.Extensions.Http.Resilience

        // Crypto - CoinGecko
        services.AddHttpClient<ICryptoService, CoinGeckoCryptoService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        // Currency Exchange
        services.AddHttpClient<ICurrencyExchangeService, ExchangeRateCurrencyService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
