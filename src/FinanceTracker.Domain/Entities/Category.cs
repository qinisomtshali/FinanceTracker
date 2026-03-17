using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

/// <summary>
/// A category for classifying transactions (e.g., "Groceries", "Salary").
/// Each user has their own set of categories — this is data isolation.
/// 
/// WHY UserId here: Multi-tenancy at the data level. User A can never
/// see or use User B's categories because every query filters by UserId.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string? Icon { get; set; }

    // Foreign key + navigation property
    public Guid UserId { get; set; }
    

    // A category has many transactions
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
