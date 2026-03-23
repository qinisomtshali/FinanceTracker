namespace FinanceTracker.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty; // e.g., "INV-2026-001"
    public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Overdue, Cancelled
    
    // Sender (you / your business)
    public string FromName { get; set; } = string.Empty;
    public string? FromEmail { get; set; }
    public string? FromAddress { get; set; }
    public string? FromVatNumber { get; set; }
    
    // Recipient
    public string ToName { get; set; } = string.Empty;
    public string? ToEmail { get; set; }
    public string? ToAddress { get; set; }
    public string? ToVatNumber { get; set; }
    
    // Financials
    public string Currency { get; set; } = "ZAR";
    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; } = 15m; // SA VAT rate
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    
    // Dates
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    
    // Banking
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
    public string? Reference { get; set; }
    
    public string? Notes { get; set; }
    
    public List<InvoiceLineItem> LineItems { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class InvoiceLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
    
    public Invoice Invoice { get; set; } = null!;
}
