namespace FinanceTracker.Application.DTOs.Budgets;

public record BudgetDto(
    Guid Id,
    decimal Amount,
    int Month,
    int Year,
    Guid CategoryId,
    string CategoryName,
    decimal SpentAmount,  // Calculated: how much was actually spent
    decimal RemainingAmount  // Calculated: Amount - SpentAmount
);

public record CreateBudgetDto(
    decimal Amount,
    int Month,
    int Year,
    Guid CategoryId
);

public record UpdateBudgetDto(
    decimal Amount,
    int Month,
    int Year,
    Guid CategoryId
);
