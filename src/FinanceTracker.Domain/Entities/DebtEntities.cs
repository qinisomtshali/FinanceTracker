namespace FinanceTracker.Domain.Entities;

public class Debt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // "FNB Credit Card", "Woolworths Store Card"
    public string Type { get; set; } = "Other"; // CreditCard, StoreCard, PersonalLoan, CarFinance, HomeLoan, StudentLoan, Other
    public string? Lender { get; set; } // "FNB", "Woolworths", "MFC"
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; } // annual percentage
    public decimal MinimumPayment { get; set; } // monthly minimum
    public decimal ActualPayment { get; set; } // what user actually pays monthly
    public int DueDay { get; set; } = 1; // day of month payment is due
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, PaidOff, Paused
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<DebtPayment> Payments { get; set; } = new();
}

public class DebtPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DebtId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; } // balance after this payment
    public string? Note { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public Debt Debt { get; set; } = null!;
}
