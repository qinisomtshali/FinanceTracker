using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Infrastructure.ExternalServices;

/// <summary>
/// Currency exchange via ExchangeRate-API (free tier: 1500 requests/month)
/// Sign up: https://www.exchangerate-api.com/
/// Alternative free option: https://open.er-api.com/ (no key needed)
/// </summary>
public class ExchangeRateCurrencyService : ICurrencyExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<ExchangeRateCurrencyService> _logger;

    public ExchangeRateCurrencyService(HttpClient httpClient, IConfiguration config, ILogger<ExchangeRateCurrencyService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["ExternalApis:ExchangeRate:ApiKey"] ?? "";
        _logger = logger;

        // Use the free open API if no key is configured
        _httpClient.BaseAddress = string.IsNullOrEmpty(_apiKey)
            ? new Uri("https://open.er-api.com/v6/")
            : new Uri($"https://v6.exchangerate-api.com/v6/{_apiKey}/");
    }

    public async Task<ExchangeRateDto?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        try
        {
            var url = $"latest/{fromCurrency.ToUpper()}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            if (!response.TryGetProperty("conversion_rates", out var rates) &&
                !response.TryGetProperty("rates", out rates))
                return null;

            if (!rates.TryGetProperty(toCurrency.ToUpper(), out var rateVal))
                return null;

            return new ExchangeRateDto(
                FromCurrency: fromCurrency.ToUpper(),
                ToCurrency: toCurrency.ToUpper(),
                Rate: rateVal.GetDecimal(),
                Timestamp: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get exchange rate {From}/{To}", fromCurrency, toCurrency);
            return null;
        }
    }

    public async Task<Dictionary<string, decimal>> GetAllRatesAsync(string baseCurrency = "ZAR")
    {
        try
        {
            var url = $"latest/{baseCurrency.ToUpper()}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            var ratesProp = response.TryGetProperty("conversion_rates", out var rates)
                ? rates
                : response.GetProperty("rates");

            return ratesProp.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetDecimal());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all rates for {Base}", baseCurrency);
            return new();
        }
    }

    public async Task<CurrencyConversionResultDto> ConvertAsync(string from, string to, decimal amount)
    {
        var rate = await GetExchangeRateAsync(from, to);
        if (rate == null)
            throw new InvalidOperationException($"Could not get exchange rate for {from}/{to}");

        var convertedAmount = Math.Round(amount * rate.Rate, 4);

        return new CurrencyConversionResultDto(
            FromCurrency: from.ToUpper(),
            ToCurrency: to.ToUpper(),
            Amount: amount,
            ConvertedAmount: convertedAmount,
            Rate: rate.Rate,
            Timestamp: DateTime.UtcNow
        );
    }

    public async Task<List<string>> GetSupportedCurrenciesAsync()
    {
        try
        {
            // Get rates for USD to list all supported currencies
            var rates = await GetAllRatesAsync("USD");
            return rates.Keys.OrderBy(k => k).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get supported currencies");
            // Return common currencies as fallback
            return new List<string>
            {
                "ZAR", "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF",
                "CNY", "INR", "BRL", "KRW", "MXN", "NGN", "KES"
            };
        }
    }
}
