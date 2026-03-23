using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Infrastructure.ExternalServices;

/// <summary>
/// Stock market data via Alpha Vantage API (free tier: 25 requests/day)
/// Sign up: https://www.alphavantage.co/support/#api-key
/// </summary>
public class AlphaVantageStockService : IStockMarketService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<AlphaVantageStockService> _logger;

    public AlphaVantageStockService(HttpClient httpClient, IConfiguration config, ILogger<AlphaVantageStockService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["ExternalApis:AlphaVantage:ApiKey"] ?? "demo";
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://www.alphavantage.co/");
    }

    public async Task<StockQuoteDto?> GetQuoteAsync(string symbol)
    {
        try
        {
            // Try GLOBAL_QUOTE first
            var url = $"query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
           
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
            _logger.LogInformation("Alpha Vantage response: {Response}", response.ToString());

            if (response.TryGetProperty("Global Quote", out var quote))
            {
                var price = GetDecimal(quote, "05. price");
                if (price > 0)
                {
                    return new StockQuoteDto(
                        Symbol: GetString(quote, "01. symbol"),
                        Name: symbol,
                        Exchange: "",
                        Price: price,
                        Change: GetDecimal(quote, "09. change"),
                        ChangePercent: ParsePercent(GetString(quote, "10. change percent")),
                        High: GetDecimal(quote, "03. high"),
                        Low: GetDecimal(quote, "04. low"),
                        Open: GetDecimal(quote, "02. open"),
                        PreviousClose: GetDecimal(quote, "08. previous close"),
                        Volume: GetLong(quote, "06. volume"),
                        Timestamp: DateTime.Parse(GetString(quote, "07. latest trading day"))
                    );
                }
            }

            // Fallback: use TIME_SERIES_DAILY for the latest day's data
            url = $"query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact&apikey={_apiKey}";
           
            response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
            _logger.LogInformation("Alpha Vantage response: {Response}", response.ToString());

            if (!response.TryGetProperty("Time Series (Daily)", out var timeSeries))
                return null;

            var latest = timeSeries.EnumerateObject().First();
            var previousDay = timeSeries.EnumerateObject().Skip(1).FirstOrDefault();

            var closePrice = GetDecimal(latest.Value, "4. close");
            var prevClose = previousDay.Value.ValueKind != JsonValueKind.Undefined
                ? GetDecimal(previousDay.Value, "4. close") : 0m;
            var change = prevClose > 0 ? closePrice - prevClose : 0m;
            var changePct = prevClose > 0 ? Math.Round(change / prevClose * 100, 4) : 0m;

            return new StockQuoteDto(
                Symbol: symbol.ToUpper(),
                Name: symbol,
                Exchange: "",
                Price: closePrice,
                Change: change,
                ChangePercent: changePct,
                High: GetDecimal(latest.Value, "2. high"),
                Low: GetDecimal(latest.Value, "3. low"),
                Open: GetDecimal(latest.Value, "1. open"),
                PreviousClose: prevClose,
                Volume: GetLong(latest.Value, "5. volume"),
                Timestamp: DateTime.Parse(latest.Name)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stock quote for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<StockSearchResultDto>> SearchAsync(string query)
    {
        try
        {
            var url = $"query?function=SYMBOL_SEARCH&keywords={query}&apikey={_apiKey}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            if (!response.TryGetProperty("bestMatches", out var matches))
                return new();

            return matches.EnumerateArray().Select(m => new StockSearchResultDto(
                Symbol: GetString(m, "1. symbol"),
                Name: GetString(m, "2. name"),
                Type: GetString(m, "3. type"),
                Region: GetString(m, "4. region"),
                Currency: GetString(m, "8. currency")
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search stocks for {Query}", query);
            return new();
        }
    }

    public async Task<StockHistoryDto?> GetDailyHistoryAsync(string symbol, int days = 30)
    {
        try
        {
            var outputSize = days > 100 ? "full" : "compact";
            var url = $"query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize={outputSize}&apikey={_apiKey}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            if (!response.TryGetProperty("Time Series (Daily)", out var timeSeries))
                return null;

            var dataPoints = timeSeries.EnumerateObject()
                .Take(days)
                .Select(day => new StockHistoryPoint(
                    Date: DateTime.Parse(day.Name),
                    Open: GetDecimal(day.Value, "1. open"),
                    High: GetDecimal(day.Value, "2. high"),
                    Low: GetDecimal(day.Value, "3. low"),
                    Close: GetDecimal(day.Value, "4. close"),
                    Volume: GetLong(day.Value, "5. volume")
                ))
                .OrderBy(p => p.Date)
                .ToList();

            return new StockHistoryDto(symbol, dataPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stock history for {Symbol}", symbol);
            return null;
        }
    }

    // ─── Helpers ────────────────────────────────────────────────

    private static string GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) ? val.GetString() ?? "" : "";

    private static decimal GetDecimal(JsonElement el, string prop)
    => el.TryGetProperty(prop, out var val)
        && decimal.TryParse(val.GetString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;

    private static long GetLong(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && long.TryParse(val.GetString(), out var l) ? l : 0;

    private static decimal ParsePercent(string value)
    => decimal.TryParse(value.TrimEnd('%'), System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
}
