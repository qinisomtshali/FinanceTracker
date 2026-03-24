using MediatR;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Application.Features.Savings;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Services;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Handlers;

public class GetSavingsGoalsHandler : IRequestHandler<GetSavingsGoalsQuery, List<SavingsGoalDto>>
{
    private readonly ApplicationDbContext _db;
    public GetSavingsGoalsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<SavingsGoalDto>> Handle(GetSavingsGoalsQuery request, CancellationToken ct)
    {
        var goals = await _db.Set<SavingsGoal>()
            .Where(g => g.UserId == request.UserId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        return goals.Select(MapToDto).ToList();
    }

    internal static SavingsGoalDto MapToDto(SavingsGoal g)
    {
        var progress = g.TargetAmount > 0 ? Math.Round(g.CurrentAmount / g.TargetAmount * 100, 1) : 0;
        int? estimatedMonths = null;
        if (g.MonthlyContribution > 0 && g.CurrentAmount < g.TargetAmount)
        {
            estimatedMonths = SavingsInterestCalculator.MonthsToTarget(
                g.CurrentAmount, g.MonthlyContribution.Value, 7m, g.TargetAmount);
        }

        return new SavingsGoalDto(
            g.Id, g.Name, g.Icon, g.Color,
            g.TargetAmount, g.CurrentAmount, progress,
            g.MonthlyContribution, g.Priority, g.Status,
            g.TargetDate, g.CompletedDate, estimatedMonths, g.CreatedAt
        );
    }
}

public class GetSavingsGoalByIdHandler : IRequestHandler<GetSavingsGoalByIdQuery, SavingsGoalDto?>
{
    private readonly ApplicationDbContext _db;
    public GetSavingsGoalByIdHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsGoalDto?> Handle(GetSavingsGoalByIdQuery request, CancellationToken ct)
    {
        var goal = await _db.Set<SavingsGoal>()
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == request.UserId, ct);
        return goal == null ? null : GetSavingsGoalsHandler.MapToDto(goal);
    }
}

public class CreateSavingsGoalHandler : IRequestHandler<CreateSavingsGoalCommand, SavingsGoalDto>
{
    private readonly ApplicationDbContext _db;
    public CreateSavingsGoalHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsGoalDto> Handle(CreateSavingsGoalCommand request, CancellationToken ct)
    {
        var goal = new SavingsGoal
        {
            UserId = request.UserId,
            Name = request.Name,
            Icon = request.Icon ?? "🎯",
            Color = request.Color ?? "#8B5CF6",
            TargetAmount = request.TargetAmount,
            MonthlyContribution = request.MonthlyContribution,
            Priority = request.Priority,
            TargetDate = request.TargetDate
        };

        _db.Set<SavingsGoal>().Add(goal);
        await _db.SaveChangesAsync(ct);

        return GetSavingsGoalsHandler.MapToDto(goal);
    }
}

public class DepositToGoalHandler : IRequestHandler<DepositToGoalCommand, SavingsDepositResultDto>
{
    private readonly ApplicationDbContext _db;
    public DepositToGoalHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsDepositResultDto> Handle(DepositToGoalCommand request, CancellationToken ct)
    {
        var goal = await _db.Set<SavingsGoal>()
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == request.UserId, ct);

        if (goal == null) throw new InvalidOperationException("Savings goal not found");

        var deposit = new SavingsDeposit
        {
            SavingsGoalId = goal.Id,
            UserId = request.UserId,
            Amount = request.Amount,
            Note = request.Note
        };

        goal.CurrentAmount += request.Amount;
        goal.UpdatedAt = DateTime.UtcNow;

        bool goalCompleted = false;
        if (goal.CurrentAmount >= goal.TargetAmount && goal.Status == "Active")
        {
            goal.Status = "Completed";
            goal.CompletedDate = DateTime.UtcNow;
            goalCompleted = true;
        }

        _db.Set<SavingsDeposit>().Add(deposit);

        // Award points
        int pointsEarned = GamificationEngine.PointValues.MakeSavingsDeposit;
        if (goalCompleted) pointsEarned += GamificationEngine.PointValues.CompleteSavingsGoal;

        var profile = await _db.Set<UserFinancialProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
        if (profile != null)
        {
            profile.TotalPoints += pointsEarned;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        _db.Set<PointTransaction>().Add(new PointTransaction
        {
            UserId = request.UserId,
            Points = pointsEarned,
            Reason = goalCompleted ? $"Completed savings goal: {goal.Name}" : $"Deposit to: {goal.Name}",
            Category = "Saving"
        });

        await _db.SaveChangesAsync(ct);

        var progress = goal.TargetAmount > 0 ? Math.Round(goal.CurrentAmount / goal.TargetAmount * 100, 1) : 100;

        return new SavingsDepositResultDto(deposit.Id, goal.CurrentAmount, progress, goalCompleted, pointsEarned);
    }
}

public class UpdateSavingsGoalHandler : IRequestHandler<UpdateSavingsGoalCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public UpdateSavingsGoalHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateSavingsGoalCommand request, CancellationToken ct)
    {
        var goal = await _db.Set<SavingsGoal>()
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == request.UserId, ct);
        if (goal == null) return false;

        if (request.Name != null) goal.Name = request.Name;
        if (request.TargetAmount.HasValue) goal.TargetAmount = request.TargetAmount.Value;
        if (request.MonthlyContribution.HasValue) goal.MonthlyContribution = request.MonthlyContribution.Value;
        if (request.Priority != null) goal.Priority = request.Priority;
        if (request.Status != null) goal.Status = request.Status;
        if (request.TargetDate.HasValue) goal.TargetDate = request.TargetDate.Value;
        goal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteSavingsGoalHandler : IRequestHandler<DeleteSavingsGoalCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public DeleteSavingsGoalHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteSavingsGoalCommand request, CancellationToken ct)
    {
        var goal = await _db.Set<SavingsGoal>()
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == request.UserId, ct);
        if (goal == null) return false;

        _db.Set<SavingsGoal>().Remove(goal);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class GetSavingsDepositsHandler : IRequestHandler<GetSavingsDepositsQuery, List<SavingsDepositDto>>
{
    private readonly ApplicationDbContext _db;
    public GetSavingsDepositsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<SavingsDepositDto>> Handle(GetSavingsDepositsQuery request, CancellationToken ct)
    {
        return await _db.Set<SavingsDeposit>()
            .Where(d => d.SavingsGoalId == request.GoalId && d.UserId == request.UserId)
            .OrderByDescending(d => d.DepositDate)
            .Select(d => new SavingsDepositDto(d.Id, d.Amount, d.Note, d.DepositDate))
            .ToListAsync(ct);
    }
}

public class GetSavingsChallengesHandler : IRequestHandler<GetSavingsChallengesQuery, List<SavingsChallengeDto>>
{
    private readonly ApplicationDbContext _db;
    public GetSavingsChallengesHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<SavingsChallengeDto>> Handle(GetSavingsChallengesQuery request, CancellationToken ct)
    {
        var challenges = await _db.Set<SavingsChallenge>()
            .Where(c => c.UserId == request.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return challenges.Select(c => new SavingsChallengeDto(
            c.Id, c.Type, c.Name, c.Status,
            c.TargetAmount, c.CurrentAmount,
            c.TargetAmount > 0 ? Math.Round(c.CurrentAmount / c.TargetAmount * 100, 1) : 0,
            c.CurrentDay, c.TotalDays, c.StartDate, c.EndDate
        )).ToList();
    }
}

public class StartChallengeHandler : IRequestHandler<StartChallengeCommand, SavingsChallengeDto>
{
    private readonly ApplicationDbContext _db;
    public StartChallengeHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsChallengeDto> Handle(StartChallengeCommand request, CancellationToken ct)
    {
        var template = GamificationEngine.GetChallengeTemplate(request.Type);

        var challenge = new SavingsChallenge
        {
            UserId = request.UserId,
            Type = request.Type,
            Name = template.Name,
            TargetAmount = template.TargetAmount,
            TotalDays = template.TotalDays,
            StartDate = DateTime.UtcNow
        };

        _db.Set<SavingsChallenge>().Add(challenge);

        // Award points for starting
        var profile = await _db.Set<UserFinancialProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
        if (profile != null)
        {
            profile.TotalPoints += GamificationEngine.PointValues.StartChallenge;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        _db.Set<PointTransaction>().Add(new PointTransaction
        {
            UserId = request.UserId,
            Points = GamificationEngine.PointValues.StartChallenge,
            Reason = $"Started challenge: {template.Name}",
            Category = "Saving"
        });

        await _db.SaveChangesAsync(ct);

        return new SavingsChallengeDto(
            challenge.Id, challenge.Type, challenge.Name, challenge.Status,
            challenge.TargetAmount, challenge.CurrentAmount, 0,
            challenge.CurrentDay, challenge.TotalDays, challenge.StartDate, challenge.EndDate
        );
    }
}

public class LogChallengeProgressHandler : IRequestHandler<LogChallengeProgressCommand, SavingsChallengeDto>
{
    private readonly ApplicationDbContext _db;
    public LogChallengeProgressHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsChallengeDto> Handle(LogChallengeProgressCommand request, CancellationToken ct)
    {
        var challenge = await _db.Set<SavingsChallenge>()
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId && c.UserId == request.UserId, ct);

        if (challenge == null) throw new InvalidOperationException("Challenge not found");

        challenge.CurrentAmount += request.Amount;
        challenge.CurrentDay++;
        challenge.UpdatedAt = DateTime.UtcNow;

        if (challenge.CurrentDay >= challenge.TotalDays || challenge.CurrentAmount >= challenge.TargetAmount)
        {
            challenge.Status = "Completed";
            challenge.EndDate = DateTime.UtcNow;

            var profile = await _db.Set<UserFinancialProfile>()
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
            if (profile != null)
            {
                profile.TotalPoints += GamificationEngine.PointValues.CompleteChallenge;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            _db.Set<PointTransaction>().Add(new PointTransaction
            {
                UserId = request.UserId,
                Points = GamificationEngine.PointValues.CompleteChallenge,
                Reason = $"Completed challenge: {challenge.Name}",
                Category = "Saving"
            });
        }

        await _db.SaveChangesAsync(ct);

        var progress = challenge.TargetAmount > 0
            ? Math.Round(challenge.CurrentAmount / challenge.TargetAmount * 100, 1) : 0;

        return new SavingsChallengeDto(
            challenge.Id, challenge.Type, challenge.Name, challenge.Status,
            challenge.TargetAmount, challenge.CurrentAmount, progress,
            challenge.CurrentDay, challenge.TotalDays, challenge.StartDate, challenge.EndDate
        );
    }
}

public class GetSavingsSummaryHandler : IRequestHandler<GetSavingsSummaryQuery, SavingsSummaryDto>
{
    private readonly ApplicationDbContext _db;
    public GetSavingsSummaryHandler(ApplicationDbContext db) => _db = db;

    public async Task<SavingsSummaryDto> Handle(GetSavingsSummaryQuery request, CancellationToken ct)
    {
        var goals = await _db.Set<SavingsGoal>()
            .Where(g => g.UserId == request.UserId)
            .ToListAsync(ct);

        var challenges = await _db.Set<SavingsChallenge>()
            .Where(c => c.UserId == request.UserId && c.Status == "Active")
            .CountAsync(ct);

        var emergencyGoal = goals.FirstOrDefault(g =>
            g.Name.ToLower().Contains("emergency") && g.Status == "Active");

        return new SavingsSummaryDto(
            TotalSaved: goals.Sum(g => g.CurrentAmount),
            TotalTargets: goals.Where(g => g.Status == "Active").Sum(g => g.TargetAmount),
            OverallProgress: goals.Any(g => g.Status == "Active" && g.TargetAmount > 0)
                ? Math.Round(goals.Where(g => g.Status == "Active").Average(g =>
                    g.TargetAmount > 0 ? Math.Min(g.CurrentAmount / g.TargetAmount * 100, 100) : 0), 1)
                : 0,
            ActiveGoals: goals.Count(g => g.Status == "Active"),
            CompletedGoals: goals.Count(g => g.Status == "Completed"),
            ActiveChallenges: challenges,
            EmergencyFundBalance: emergencyGoal?.CurrentAmount ?? 0,
            EmergencyFundTarget: emergencyGoal?.TargetAmount ?? 0
        );
    }
}
