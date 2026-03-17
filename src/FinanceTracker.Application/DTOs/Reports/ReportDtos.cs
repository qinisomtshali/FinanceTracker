namespace FinanceTracker.Application.DTOs.Reports;

public record MonthlySummaryDto(
    int Month,
    int Year,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount  // Income - Expenses
);

public record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    decimal Percentage,  // What % of total spending this category represents
    int TransactionCount
);

public record BudgetVsActualDto(
    Guid CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal Difference,  // Budget - Actual (positive = under budget)
    decimal PercentageUsed
);
