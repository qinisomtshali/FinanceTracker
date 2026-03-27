using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Services;
using MediatR;

namespace FinanceTracker.Application.Features.Recurring;

// ─── Queries ────────────────────────────────────────────────────

public record GetRecurringTransactionsQuery(string UserId) : IRequest<List<RecurringTransactionDto>>;
public record GetUpcomingBillsQuery(string UserId, int Days = 7) : IRequest<List<UpcomingBillDto>>;
public record GetBillCalendarQuery(string UserId, int Month, int Year) : IRequest<BillCalendarDto>;
public record GetPaydayPlanQuery(string UserId, int SalaryDay = 25) : IRequest<PaydayPlanResult>;

// ─── Commands ───────────────────────────────────────────────────

public record CreateRecurringTransactionCommand(
    string UserId,
    string Name,
    decimal Amount,
    string? Description,
    TransactionType Type,
    Guid CategoryId,
    string Frequency,
    int DayOfMonth,
    int? DayOfWeek,
    DateTime StartDate,
    DateTime? EndDate,
    bool AutoGenerate,
    bool NotifyBeforeDue,
    int NotifyDaysBefore
) : IRequest<RecurringTransactionDto>;

public record UpdateRecurringTransactionCommand(
    string UserId,
    Guid Id,
    string? Name,
    decimal? Amount,
    string? Description,
    Guid? CategoryId,
    string? Frequency,
    int? DayOfMonth,
    bool? IsActive,
    bool? AutoGenerate,
    bool? NotifyBeforeDue,
    int? NotifyDaysBefore
) : IRequest<bool>;

public record DeleteRecurringTransactionCommand(string UserId, Guid Id) : IRequest<bool>;

public record ToggleRecurringTransactionCommand(string UserId, Guid Id) : IRequest<bool>;

// ─── DTOs ───────────────────────────────────────────────────────

public record RecurringTransactionDto(
    Guid Id,
    string Name,
    decimal Amount,
    string? Description,
    string Type,
    Guid CategoryId,
    string? CategoryName,
    string Frequency,
    int DayOfMonth,
    int? DayOfWeek,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime? NextDueDate,
    DateTime? LastGeneratedDate,
    bool IsActive,
    bool AutoGenerate,
    bool NotifyBeforeDue,
    int NotifyDaysBefore,
    DateTime CreatedAt
);

public record UpcomingBillDto(
    Guid RecurringId,
    string Name,
    decimal Amount,
    string Type,
    string? CategoryName,
    DateTime DueDate,
    int DaysUntilDue,
    string Frequency,
    bool IsOverdue
);

public record BillCalendarDto(
    int Month,
    int Year,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    List<CalendarDayDto> Days
);

public record CalendarDayDto(
    int Day,
    DateTime Date,
    bool IsToday,
    bool IsSalaryDay,
    List<CalendarBillDto> Bills
);

public record CalendarBillDto(
    Guid RecurringId,
    string Name,
    decimal Amount,
    string Type,
    string? CategoryName
);
