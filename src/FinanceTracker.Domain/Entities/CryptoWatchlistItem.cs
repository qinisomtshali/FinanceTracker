namespace FinanceTracker.Domain.Entities;

public class CryptoWatchlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string CoinId { get; set; } = string.Empty; // e.g., "bitcoin", "ethereum"
    public string Symbol { get; set; } = string.Empty; // e.g., "BTC", "ETH"
    public string Name { get; set; } = string.Empty;
    public decimal? HoldingQuantity { get; set; }
    public decimal? AverageBuyPrice { get; set; }
    public string Currency { get; set; } = "USD"; // base currency for tracking
    public decimal? AlertPriceAbove { get; set; }
    public decimal? AlertPriceBelow { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
