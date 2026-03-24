using MediatR;
using FinanceTracker.Domain.Services;

namespace FinanceTracker.Application.Features.Savings;

// ─── Queries ────────────────────────────────────────────────────

public record GetSavingsGoalsQuery(string UserId) : IRequest<List<SavingsGoalDto>>;
public record GetSavingsGoalByIdQuery(string UserId, Guid GoalId) : IRequest<SavingsGoalDto?>;
public record GetSavingsDepositsQuery(string UserId, Guid GoalId) : IRequest<List<SavingsDepositDto>>;
public record GetSavingsChallengesQuery(string UserId) : IRequest<List<SavingsChallengeDto>>;
public record GetSavingsSummaryQuery(string UserId) : IRequest<SavingsSummaryDto>;
public record CalculateInterestQuery(decimal InitialAmount, decimal MonthlyContribution, decimal AnnualRate, int Months) : IRequest<SavingsProjection>;
public record GetBankRatesQuery() : IRequest<Dictionary<string, decimal>>;

// ─── Commands ───────────────────────────────────────────────────

public record CreateSavingsGoalCommand(
    string UserId,
    string Name,
    string? Icon,
    string? Color,
    decimal TargetAmount,
    decimal? MonthlyContribution,
    string Priority,
    DateTime? TargetDate
) : IRequest<SavingsGoalDto>;

public record DepositToGoalCommand(
    string UserId,
    Guid GoalId,
    decimal Amount,
    string? Note
) : IRequest<SavingsDepositResultDto>;

public record UpdateSavingsGoalCommand(
    string UserId,
    Guid GoalId,
    string? Name,
    decimal? TargetAmount,
    decimal? MonthlyContribution,
    string? Priority,
    string? Status,
    DateTime? TargetDate
) : IRequest<bool>;

public record DeleteSavingsGoalCommand(string UserId, Guid GoalId) : IRequest<bool>;

public record StartChallengeCommand(
    string UserId,
    string Type // "30-day", "52-week", "no-spend"
) : IRequest<SavingsChallengeDto>;

public record LogChallengeProgressCommand(
    string UserId,
    Guid ChallengeId,
    decimal Amount
) : IRequest<SavingsChallengeDto>;

// ─── DTOs ───────────────────────────────────────────────────────

public record SavingsGoalDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPercentage,
    decimal? MonthlyContribution,
    string Priority,
    string Status,
    DateTime? TargetDate,
    DateTime? CompletedDate,
    int? EstimatedMonthsToGoal,
    DateTime CreatedAt
);

public record SavingsDepositDto(
    Guid Id,
    decimal Amount,
    string? Note,
    DateTime DepositDate
);

public record SavingsDepositResultDto(
    Guid DepositId,
    decimal NewBalance,
    decimal ProgressPercentage,
    bool GoalCompleted,
    int? PointsEarned
);

public record SavingsChallengeDto(
    Guid Id,
    string Type,
    string Name,
    string Status,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPercentage,
    int CurrentDay,
    int TotalDays,
    DateTime StartDate,
    DateTime? EndDate
);

public record SavingsSummaryDto(
    decimal TotalSaved,
    decimal TotalTargets,
    decimal OverallProgress,
    int ActiveGoals,
    int CompletedGoals,
    int ActiveChallenges,
    decimal EmergencyFundBalance,
    decimal EmergencyFundTarget
);

// ─── Handlers (pure logic, no DB) ───────────────────────────────

public class CalculateInterestHandler : IRequestHandler<CalculateInterestQuery, SavingsProjection>
{
    public Task<SavingsProjection> Handle(CalculateInterestQuery request, CancellationToken ct)
    {
        var result = SavingsInterestCalculator.Calculate(
            request.InitialAmount,
            request.MonthlyContribution,
            request.AnnualRate,
            request.Months
        );
        return Task.FromResult(result);
    }
}

public class GetBankRatesHandler : IRequestHandler<GetBankRatesQuery, Dictionary<string, decimal>>
{
    public Task<Dictionary<string, decimal>> Handle(GetBankRatesQuery request, CancellationToken ct)
        => Task.FromResult(SavingsInterestCalculator.BankRates);
}
