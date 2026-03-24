using MediatR;
using FinanceTracker.Domain.Services;

namespace FinanceTracker.Application.Features.Gamification;

// ─── Queries ────────────────────────────────────────────────────

public record GetUserProfileQuery(string UserId) : IRequest<UserProfileDto>;
public record GetLeaderboardQuery(int Limit = 10) : IRequest<List<LeaderboardEntryDto>>;
public record GetAchievementsQuery(string UserId) : IRequest<AchievementsDto>;
public record GetPointHistoryQuery(string UserId, int Limit = 20) : IRequest<List<PointTransactionDto>>;
public record GetDashboardQuery(string UserId) : IRequest<DashboardDto>;
public record GetFinancialHealthQuery(string UserId) : IRequest<FinancialHealthResult>;
public record GetDailyTipQuery() : IRequest<TipDto?>;

// ─── DTOs ───────────────────────────────────────────────────────

public record UserProfileDto(
    int TotalPoints,
    int Level,
    string Tier,
    int PointsToNextLevel,
    int CurrentStreak,
    int LongestStreak,
    int HealthScore,
    string HealthGrade,
    decimal SavingsRate,
    int AchievementsUnlocked,
    int TotalAchievements
);

public record LeaderboardEntryDto(
    int Rank,
    string UserId,
    string DisplayName,
    int TotalPoints,
    int Level,
    string Tier,
    int CurrentStreak
);

public record AchievementsDto(
    List<AchievementDto> Unlocked,
    List<AchievementDto> Locked
);

public record AchievementDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Icon,
    string Category,
    int PointsAwarded,
    string Difficulty,
    DateTime? UnlockedAt
);

public record PointTransactionDto(
    int Points,
    string Reason,
    string Category,
    DateTime EarnedAt
);

public record DashboardDto(
    // Quick stats
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal MonthlySavings,
    decimal SavingsRate,
    // Gamification
    int TotalPoints,
    int Level,
    string Tier,
    int CurrentStreak,
    int HealthScore,
    string HealthGrade,
    // Recent activity
    List<RecentActivityDto> RecentActivity,
    // Tip of the day
    TipDto? DailyTip,
    // Savings overview
    decimal TotalSavingsGoalProgress,
    int ActiveSavingsGoals,
    // Budget overview
    int BudgetsOnTrack,
    int TotalBudgets
);

public record RecentActivityDto(
    string Type, // "transaction", "achievement", "points", "savings"
    string Description,
    decimal? Amount,
    int? Points,
    DateTime Timestamp
);

public record TipDto(
    Guid Id,
    string Title,
    string Content,
    string Category,
    string Difficulty
);
