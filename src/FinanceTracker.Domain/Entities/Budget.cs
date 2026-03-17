using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

/// <summary>
/// A monthly spending limit for a specific category.
/// E.g., "I want to spend no more than R5000 on Groceries in March 2026."
/// 
/// WHY Month + Year instead of a DateTime: We only care about the month/year
/// granularity for budgets. Using separate int fields makes queries cleaner:
///   WHERE Month = 3 AND Year = 2026
/// instead of date range comparisons.
/// </summary>
public class Budget : BaseEntity
{
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Foreign keys
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    
}
