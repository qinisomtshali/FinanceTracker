using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

/// <summary>
/// A recurring transaction template — debit orders, salary, subscriptions.
/// Generates actual Transaction records on schedule.
/// 
/// Examples:
///   - Rent: R8,500/month on the 1st
///   - Netflix: R199/month on the 15th
///   - Salary: R35,000/month on the 25th
///   - Gym: R399/month on the 1st
///   - Car insurance: R1,200/month on the 3rd
/// </summary>
public class RecurringTransaction : BaseEntity
{
    public string Name { get; set; } = string.Empty; // "Rent", "Netflix", "Salary"
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public TransactionType Type { get; set; } // Income or Expense
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid UserId { get; set; }

    // Schedule
    public string Frequency { get; set; } = "Monthly"; // Monthly, Weekly, BiWeekly, Quarterly, Yearly
    public int DayOfMonth { get; set; } = 1; // 1-31, day the transaction occurs
    public int? DayOfWeek { get; set; } // 0=Sunday to 6=Saturday (for Weekly/BiWeekly)
    
    // Tracking
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } // null = runs forever
    public DateTime? LastGeneratedDate { get; set; } // last time a transaction was auto-created
    public DateTime? NextDueDate { get; set; } // next upcoming occurrence
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool AutoGenerate { get; set; } = true; // if true, auto-creates transactions; if false, just reminders
    
    // Notifications
    public bool NotifyBeforeDue { get; set; } = true;
    public int NotifyDaysBefore { get; set; } = 2; // remind X days before due date
}
