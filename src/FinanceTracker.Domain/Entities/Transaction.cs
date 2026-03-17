using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

/// <summary>
/// Represents a single financial transaction — money in or money out.
/// This is the core entity of the entire system.
/// 
/// DESIGN DECISION: Amount is always positive. The Type (Income/Expense)
/// determines the direction. This makes reporting queries simpler:
///   SUM(Amount) WHERE Type = Income  minus  SUM(Amount) WHERE Type = Expense
/// instead of dealing with negative numbers.
/// </summary>
public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }

    // Foreign keys
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    
}
