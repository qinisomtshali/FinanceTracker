namespace FinanceTracker.Domain.Entities;

public class CurrencyConversion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string FromCurrency { get; set; } = string.Empty; // e.g., "ZAR"
    public string ToCurrency { get; set; } = string.Empty; // e.g., "USD"
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Provider { get; set; } // API source
    public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;
}
