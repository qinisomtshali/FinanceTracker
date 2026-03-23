namespace FinanceTracker.Domain.Entities;

public class StockWatchlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty; // e.g., "AAPL", "MSFT"
    public string Exchange { get; set; } = string.Empty; // e.g., "NYSE", "JSE"
    public string Name { get; set; } = string.Empty;
    public decimal? AlertPriceAbove { get; set; }
    public decimal? AlertPriceBelow { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
