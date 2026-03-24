using MediatR;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Application.Features.Gamification;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Services;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Infrastructure.Handlers;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly ApplicationDbContext _db;
    public GetUserProfileHandler(ApplicationDbContext db) => _db = db;

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var profile = await _db.Set<UserFinancialProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (profile == null)
        {
            profile = new UserFinancialProfile { UserId = request.UserId };
            _db.Set<UserFinancialProfile>().Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        var (level, tier, pointsToNext) = GamificationEngine.CalculateLevel(profile.TotalPoints);
        var unlockedCount = await _db.Set<UserAchievement>().CountAsync(a => a.UserId == request.UserId, ct);
        var totalCount = await _db.Set<Achievement>().CountAsync(ct);

        return new UserProfileDto(
            TotalPoints: profile.TotalPoints,
            Level: level,
            Tier: tier,
            PointsToNextLevel: pointsToNext,
            CurrentStreak: profile.CurrentStreak,
            LongestStreak: profile.LongestStreak,
            HealthScore: profile.HealthScore,
            HealthGrade: profile.HealthGrade,
            SavingsRate: profile.SavingsRate,
            AchievementsUnlocked: unlockedCount,
            TotalAchievements: totalCount
        );
    }
}

public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryDto>>
{
    private readonly ApplicationDbContext _db;
    public GetLeaderboardHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<LeaderboardEntryDto>> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        var profiles = await _db.Set<UserFinancialProfile>()
            .OrderByDescending(p => p.TotalPoints)
            .Take(request.Limit)
            .ToListAsync(ct);

        return profiles.Select((p, i) =>
        {
            var (level, tier, _) = GamificationEngine.CalculateLevel(p.TotalPoints);
            return new LeaderboardEntryDto(
                Rank: i + 1,
                UserId: p.UserId,
                DisplayName: $"User {p.UserId[..8]}...",
                TotalPoints: p.TotalPoints,
                Level: level,
                Tier: tier,
                CurrentStreak: p.CurrentStreak
            );
        }).ToList();
    }
}

public class GetAchievementsHandler : IRequestHandler<GetAchievementsQuery, AchievementsDto>
{
    private readonly ApplicationDbContext _db;
    public GetAchievementsHandler(ApplicationDbContext db) => _db = db;

    public async Task<AchievementsDto> Handle(GetAchievementsQuery request, CancellationToken ct)
    {
        var allAchievements = await _db.Set<Achievement>().ToListAsync(ct);
        var userAchievements = await _db.Set<UserAchievement>()
            .Where(ua => ua.UserId == request.UserId)
            .ToListAsync(ct);

        var unlockedIds = userAchievements.ToDictionary(ua => ua.AchievementId, ua => ua.UnlockedAt);

        var unlocked = allAchievements
            .Where(a => unlockedIds.ContainsKey(a.Id))
            .Select(a => new AchievementDto(a.Id, a.Code, a.Name, a.Description, a.Icon,
                a.Category, a.PointsAwarded, a.Difficulty, unlockedIds[a.Id]))
            .OrderByDescending(a => a.UnlockedAt)
            .ToList();

        var locked = allAchievements
            .Where(a => !unlockedIds.ContainsKey(a.Id))
            .Select(a => new AchievementDto(a.Id, a.Code, a.Name, a.Description, a.Icon,
                a.Category, a.PointsAwarded, a.Difficulty, null))
            .OrderBy(a => a.Difficulty)
            .ToList();

        return new AchievementsDto(unlocked, locked);
    }
}

public class GetPointHistoryHandler : IRequestHandler<GetPointHistoryQuery, List<PointTransactionDto>>
{
    private readonly ApplicationDbContext _db;
    public GetPointHistoryHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<PointTransactionDto>> Handle(GetPointHistoryQuery request, CancellationToken ct)
    {
        return await _db.Set<PointTransaction>()
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.EarnedAt)
            .Take(request.Limit)
            .Select(p => new PointTransactionDto(p.Points, p.Reason, p.Category, p.EarnedAt))
            .ToListAsync(ct);
    }
}

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly ApplicationDbContext _db;
    public GetDashboardHandler(ApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Financial stats this month
        var transactions = await _db.Transactions
            .Where(t => t.UserId == Guid.Parse(request.UserId) && t.Date >= monthStart)
            .ToListAsync(ct);

        var monthlyIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var monthlyExpenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var monthlySavings = monthlyIncome - monthlyExpenses;
        var savingsRate = monthlyIncome > 0 ? Math.Round(monthlySavings / monthlyIncome * 100, 1) : 0;

        // Gamification profile
        var profile = await _db.Set<UserFinancialProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (profile == null)
        {
            profile = new UserFinancialProfile { UserId = request.UserId };
            _db.Set<UserFinancialProfile>().Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        var (level, tier, _) = GamificationEngine.CalculateLevel(profile.TotalPoints);

        // Budget overview
        var budgets = await _db.Budgets
            .Where(b => b.UserId == Guid.Parse(request.UserId))
            .ToListAsync(ct);
        var totalBudgets = budgets.Count;

        // Calculate spent per budget by matching category transactions this month
        var budgetsOnTrack = 0;
        foreach (var b in budgets)
        {
            var spent = transactions.Where(t => t.Type == TransactionType.Expense && t.CategoryId == b.CategoryId).Sum(t => t.Amount);
            if (spent <= b.Amount) budgetsOnTrack++;
        }

        // Savings goals
        var savingsGoals = await _db.Set<SavingsGoal>()
            .Where(g => g.UserId == request.UserId && g.Status == "Active")
            .ToListAsync(ct);
        var totalSavingsProgress = savingsGoals.Count > 0
            ? savingsGoals.Average(g => g.TargetAmount > 0 ? Math.Min(g.CurrentAmount / g.TargetAmount * 100, 100) : 0)
            : 0;

        // Recent activity
        var recentPoints = await _db.Set<PointTransaction>()
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.EarnedAt)
            .Take(5)
            .Select(p => new RecentActivityDto("points", p.Reason, null, p.Points, p.EarnedAt))
            .ToListAsync(ct);

        // Daily tip
        var tipCount = await _db.Set<FinancialTip>().CountAsync(t => t.IsActive, ct);
        TipDto? dailyTip = null;
        if (tipCount > 0)
        {
            var dayOfYear = now.DayOfYear;
            var tip = await _db.Set<FinancialTip>()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Id)
                .Skip(dayOfYear % tipCount)
                .Take(1)
                .FirstOrDefaultAsync(ct);

            if (tip != null)
                dailyTip = new TipDto(tip.Id, tip.Title, tip.Content, tip.Category, tip.Difficulty);
        }

        return new DashboardDto(
            MonthlyIncome: monthlyIncome,
            MonthlyExpenses: monthlyExpenses,
            MonthlySavings: monthlySavings,
            SavingsRate: savingsRate,
            TotalPoints: profile.TotalPoints,
            Level: level,
            Tier: tier,
            CurrentStreak: profile.CurrentStreak,
            HealthScore: profile.HealthScore,
            HealthGrade: profile.HealthGrade,
            RecentActivity: recentPoints,
            DailyTip: dailyTip,
            TotalSavingsGoalProgress: Math.Round(totalSavingsProgress, 1),
            ActiveSavingsGoals: savingsGoals.Count,
            BudgetsOnTrack: budgetsOnTrack,
            TotalBudgets: totalBudgets
        );
    }
}

public class GetFinancialHealthHandler : IRequestHandler<GetFinancialHealthQuery, FinancialHealthResult>
{
    private readonly ApplicationDbContext _db;
    public GetFinancialHealthHandler(ApplicationDbContext db) => _db = db;

    public async Task<FinancialHealthResult> Handle(GetFinancialHealthQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = Guid.Parse(request.UserId);

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= monthStart)
            .ToListAsync(ct);

        var monthlyIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var monthlyExpenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var budgets = await _db.Budgets.Where(b => b.UserId == userId).ToListAsync(ct);
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense).ToList();
        var budgetsUnder = budgets.Count(b => expenseTransactions.Where(t => t.CategoryId == b.CategoryId).Sum(t => t.Amount) <= b.Amount);

        // Emergency fund
        var emergencyGoal = await _db.Set<SavingsGoal>()
            .FirstOrDefaultAsync(g => g.UserId == request.UserId
                && g.Name.ToLower().Contains("emergency") && g.Status == "Active", ct);

        // Days active this month
        var daysActive = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= monthStart)
            .Select(t => t.Date.Date)
            .Distinct()
            .CountAsync(ct);

        // Top category percentage
        decimal topCategoryPct = 0;
        if (monthlyExpenses > 0)
        {
            var expensesByCategory = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.CategoryId)
                .Select(g => g.Sum(t => t.Amount))
                .ToList();

            if (expensesByCategory.Any())
                topCategoryPct = expensesByCategory.Max() / monthlyExpenses * 100;
        }

        var input = new HealthScoreInput
        {
            TotalBudgets = budgets.Count,
            BudgetsUnderLimit = budgetsUnder,
            MonthlyIncome = monthlyIncome,
            MonthlySavings = monthlyIncome - monthlyExpenses,
            MonthlyExpenses = monthlyExpenses,
            EmergencyFundBalance = emergencyGoal?.CurrentAmount ?? 0,
            DaysActiveThisMonth = daysActive,
            TopCategoryPercentage = topCategoryPct
        };

        return GamificationEngine.CalculateHealthScore(input);
    }
}

public class GetDailyTipHandler : IRequestHandler<GetDailyTipQuery, TipDto?>
{
    private readonly ApplicationDbContext _db;
    public GetDailyTipHandler(ApplicationDbContext db) => _db = db;

    public async Task<TipDto?> Handle(GetDailyTipQuery request, CancellationToken ct)
    {
        var tipCount = await _db.Set<FinancialTip>().CountAsync(t => t.IsActive, ct);
        if (tipCount == 0) return null;

        var dayOfYear = DateTime.UtcNow.DayOfYear;
        var tip = await _db.Set<FinancialTip>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Skip(dayOfYear % tipCount)
            .Take(1)
            .FirstOrDefaultAsync(ct);

        if (tip == null) return null;
        return new TipDto(tip.Id, tip.Title, tip.Content, tip.Category, tip.Difficulty);
    }
}
