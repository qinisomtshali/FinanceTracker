using MediatR;
using FinanceTracker.Domain.Services;

namespace FinanceTracker.Application.Features.Debts;

// ─── Queries ────────────────────────────────────────────────────

public record GetDebtsQuery(string UserId) : IRequest<List<DebtDto>>;
public record GetDebtByIdQuery(string UserId, Guid DebtId) : IRequest<DebtDto?>;
public record GetDebtSummaryQuery(string UserId) : IRequest<DebtSummaryDto>;
public record GetDebtPaymentsQuery(string UserId, Guid DebtId) : IRequest<List<DebtPaymentDto>>;
public record GetPayoffPlanQuery(string UserId, decimal ExtraPayment = 0) : IRequest<StrategyComparisonResult>;
public record GetDebtInsightsQuery() : IRequest<List<DebtInsight>>;

// ─── Commands ───────────────────────────────────────────────────

public record CreateDebtCommand(
    string UserId,
    string Name,
    string Type,
    string? Lender,
    decimal OriginalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    decimal MinimumPayment,
    decimal ActualPayment,
    int DueDay,
    DateTime StartDate,
    string? Notes
) : IRequest<DebtDto>;

public record UpdateDebtCommand(
    string UserId,
    Guid DebtId,
    string? Name,
    string? Type,
    string? Lender,
    decimal? CurrentBalance,
    decimal? InterestRate,
    decimal? MinimumPayment,
    decimal? ActualPayment,
    int? DueDay,
    string? Status,
    string? Notes
) : IRequest<bool>;

public record DeleteDebtCommand(string UserId, Guid DebtId) : IRequest<bool>;

public record LogDebtPaymentCommand(
    string UserId,
    Guid DebtId,
    decimal Amount,
    string? Note
) : IRequest<DebtPaymentResultDto>;

// ─── DTOs ───────────────────────────────────────────────────────

public record DebtDto(
    Guid Id,
    string Name,
    string Type,
    string? Lender,
    decimal OriginalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    decimal MinimumPayment,
    decimal ActualPayment,
    int DueDay,
    DateTime StartDate,
    string Status,
    string? Notes,
    decimal PercentagePaidOff,
    decimal TotalPaid,
    int? EstimatedMonthsToPayoff,
    DateTime CreatedAt
);

public record DebtSummaryDto(
    decimal TotalDebt,
    decimal TotalOriginalDebt,
    decimal TotalMonthlyPayments,
    decimal TotalPaidOff,
    decimal OverallProgress,
    int ActiveDebts,
    int PaidOffDebts,
    decimal DebtToIncomeRatio,
    DateTime? EstimatedDebtFreeDate,
    int? EstimatedMonthsToFree
);

public record DebtPaymentDto(
    Guid Id,
    decimal Amount,
    decimal BalanceAfter,
    string? Note,
    DateTime PaymentDate
);

public record DebtPaymentResultDto(
    Guid PaymentId,
    decimal NewBalance,
    decimal PercentagePaidOff,
    bool DebtPaidOff,
    int? PointsEarned
);

// ─── Handlers (pure logic, no DB) ───────────────────────────────

public class GetDebtInsightsHandler : IRequestHandler<GetDebtInsightsQuery, List<DebtInsight>>
{
    public Task<List<DebtInsight>> Handle(GetDebtInsightsQuery request, CancellationToken ct)
        => Task.FromResult(DebtPayoffCalculator.GetSAInsights());
}
