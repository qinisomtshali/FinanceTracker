using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Infrastructure.ExternalServices;

/// <summary>
/// Cryptocurrency data via CoinGecko API (free tier: 10-30 calls/min)
/// No API key needed for basic endpoints
/// Docs: https://www.coingecko.com/en/api/documentation
/// </summary>
public class CoinGeckoCryptoService : ICryptoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoCryptoService> _logger;

    public CoinGeckoCryptoService(HttpClient httpClient, ILogger<CoinGeckoCryptoService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FinanceTracker/1.0");
    }

    public async Task<CryptoQuoteDto?> GetPriceAsync(string coinId, string vsCurrency = "usd")
    {
        try
        {
            var url = $"coins/{coinId}?localization=false&tickers=false&community_data=false&developer_data=false";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            var marketData = response.GetProperty("market_data");
            var currentPrice = marketData.GetProperty("current_price");

            return new CryptoQuoteDto(
                Id: response.GetProperty("id").GetString() ?? coinId,
                Symbol: response.GetProperty("symbol").GetString()?.ToUpper() ?? "",
                Name: response.GetProperty("name").GetString() ?? "",
                CurrentPrice: GetDecimalFromProperty(currentPrice, vsCurrency),
                MarketCap: GetNullableDecimal(marketData, "market_cap", vsCurrency),
                Volume24h: GetNullableDecimal(marketData, "total_volume", vsCurrency),
                PriceChangePercentage24h: GetNullableDecimalDirect(marketData, "price_change_percentage_24h"),
                High24h: GetNullableDecimal(marketData, "high_24h", vsCurrency),
                Low24h: GetNullableDecimal(marketData, "low_24h", vsCurrency),
                CirculatingSupply: GetNullableDecimalDirect(marketData, "circulating_supply"),
                ImageUrl: response.TryGetProperty("image", out var img) ? img.GetProperty("small").GetString() : null,
                LastUpdated: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get crypto price for {CoinId}", coinId);
            return null;
        }
    }

    public async Task<List<CryptoQuoteDto>> GetTopCoinsAsync(int limit = 20, string vsCurrency = "usd")
    {
        try
        {
            var url = $"coins/markets?vs_currency={vsCurrency}&order=market_cap_desc&per_page={limit}&page=1&sparkline=false";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            return response.EnumerateArray().Select(coin => new CryptoQuoteDto(
                Id: coin.GetProperty("id").GetString() ?? "",
                Symbol: coin.GetProperty("symbol").GetString()?.ToUpper() ?? "",
                Name: coin.GetProperty("name").GetString() ?? "",
                CurrentPrice: coin.GetProperty("current_price").GetDecimal(),
                MarketCap: SafeGetDecimal(coin, "market_cap"),
                Volume24h: SafeGetDecimal(coin, "total_volume"),
                PriceChangePercentage24h: SafeGetDecimal(coin, "price_change_percentage_24h"),
                High24h: SafeGetDecimal(coin, "high_24h"),
                Low24h: SafeGetDecimal(coin, "low_24h"),
                CirculatingSupply: SafeGetDecimal(coin, "circulating_supply"),
                ImageUrl: coin.TryGetProperty("image", out var img) ? img.GetString() : null,
                LastUpdated: DateTime.UtcNow
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top crypto coins");
            return new();
        }
    }

    public async Task<List<CryptoSearchResultDto>> SearchAsync(string query)
    {
        try
        {
            var url = $"search?query={query}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
            var coins = response.GetProperty("coins");

            return coins.EnumerateArray().Take(10).Select(coin => new CryptoSearchResultDto(
                Id: coin.GetProperty("id").GetString() ?? "",
                Symbol: coin.GetProperty("symbol").GetString()?.ToUpper() ?? "",
                Name: coin.GetProperty("name").GetString() ?? "",
                MarketCapRank: coin.TryGetProperty("market_cap_rank", out var rank) && rank.ValueKind != JsonValueKind.Null
                    ? rank.GetInt32() : null,
                Thumb: coin.TryGetProperty("thumb", out var thumb) ? thumb.GetString() : null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search crypto for {Query}", query);
            return new();
        }
    }

    public async Task<CryptoMarketChartDto?> GetMarketChartAsync(string coinId, string vsCurrency = "usd", int days = 30)
    {
        try
        {
            var url = $"coins/{coinId}/market_chart?vs_currency={vsCurrency}&days={days}";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

            return new CryptoMarketChartDto(
                CoinId: coinId,
                Prices: ParsePricePoints(response.GetProperty("prices")),
                MarketCaps: ParsePricePoints(response.GetProperty("market_caps")),
                TotalVolumes: ParsePricePoints(response.GetProperty("total_volumes"))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get market chart for {CoinId}", coinId);
            return null;
        }
    }

    // ─── Helpers ────────────────────────────────────────────────

    private static List<PricePoint> ParsePricePoints(JsonElement arr)
        => arr.EnumerateArray().Select(p =>
        {
            var items = p.EnumerateArray().ToList();
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(items[0].GetInt64()).UtcDateTime;
            var value = items[1].GetDecimal();
            return new PricePoint(timestamp, value);
        }).ToList();

    private static decimal GetDecimalFromProperty(JsonElement parent, string property)
        => parent.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number
            ? val.GetDecimal() : 0;

    private static decimal? GetNullableDecimal(JsonElement parent, string outerProp, string innerProp)
    {
        if (!parent.TryGetProperty(outerProp, out var outer)) return null;
        if (!outer.TryGetProperty(innerProp, out var val)) return null;
        return val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
    }

    private static decimal? GetNullableDecimalDirect(JsonElement parent, string prop)
    {
        if (!parent.TryGetProperty(prop, out var val)) return null;
        return val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
    }

    private static decimal? SafeGetDecimal(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        return val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
    }
}
