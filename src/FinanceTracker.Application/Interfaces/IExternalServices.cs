namespace FinanceTracker.Application.Interfaces;

// ─── Stock Market ───────────────────────────────────────────────

public interface IStockMarketService
{
    Task<StockQuoteDto?> GetQuoteAsync(string symbol);
    Task<List<StockSearchResultDto>> SearchAsync(string query);
    Task<StockHistoryDto?> GetDailyHistoryAsync(string symbol, int days = 30);
}

public record StockQuoteDto(
    string Symbol,
    string Name,
    string Exchange,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    decimal High,
    decimal Low,
    decimal Open,
    decimal PreviousClose,
    long Volume,
    DateTime Timestamp
);

public record StockSearchResultDto(
    string Symbol,
    string Name,
    string Type,
    string Region,
    string Currency
);

public record StockHistoryDto(
    string Symbol,
    List<StockHistoryPoint> DataPoints
);

public record StockHistoryPoint(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);

// ─── Cryptocurrency ─────────────────────────────────────────────

public interface ICryptoService
{
    Task<CryptoQuoteDto?> GetPriceAsync(string coinId, string vsCurrency = "usd");
    Task<List<CryptoQuoteDto>> GetTopCoinsAsync(int limit = 20, string vsCurrency = "usd");
    Task<List<CryptoSearchResultDto>> SearchAsync(string query);
    Task<CryptoMarketChartDto?> GetMarketChartAsync(string coinId, string vsCurrency = "usd", int days = 30);
}

public record CryptoQuoteDto(
    string Id,
    string Symbol,
    string Name,
    decimal CurrentPrice,
    decimal? MarketCap,
    decimal? Volume24h,
    decimal? PriceChangePercentage24h,
    decimal? High24h,
    decimal? Low24h,
    decimal? CirculatingSupply,
    string? ImageUrl,
    DateTime LastUpdated
);

public record CryptoSearchResultDto(
    string Id,
    string Symbol,
    string Name,
    int? MarketCapRank,
    string? Thumb
);

public record CryptoMarketChartDto(
    string CoinId,
    List<PricePoint> Prices,
    List<PricePoint> MarketCaps,
    List<PricePoint> TotalVolumes
);

public record PricePoint(DateTime Timestamp, decimal Value);

// ─── Currency Exchange ──────────────────────────────────────────

public interface ICurrencyExchangeService
{
    Task<ExchangeRateDto?> GetExchangeRateAsync(string fromCurrency, string toCurrency);
    Task<Dictionary<string, decimal>> GetAllRatesAsync(string baseCurrency = "ZAR");
    Task<CurrencyConversionResultDto> ConvertAsync(string from, string to, decimal amount);
    Task<List<string>> GetSupportedCurrenciesAsync();
}

public record ExchangeRateDto(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime Timestamp
);

public record CurrencyConversionResultDto(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal ConvertedAmount,
    decimal Rate,
    DateTime Timestamp
);
