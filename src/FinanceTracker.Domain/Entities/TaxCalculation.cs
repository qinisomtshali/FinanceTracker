namespace FinanceTracker.Domain.Entities;

public class TaxCalculation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string TaxYear { get; set; } = string.Empty; // e.g., "2025/2026"
    public string Country { get; set; } = "ZA";
    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal EffectiveRate { get; set; } // percentage
    public decimal? MedicalAidCredits { get; set; }
    public decimal? RetirementDeduction { get; set; }
    public int? Age { get; set; }
    public string? TaxBracketDetails { get; set; } // JSON string of bracket breakdown
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
