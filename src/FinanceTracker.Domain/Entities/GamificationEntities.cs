namespace FinanceTracker.Domain.Entities;

// ─── User Financial Profile & Gamification ──────────────────────

public class UserFinancialProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    
    // Points & Levels
    public int TotalPoints { get; set; }
    public int Level { get; set; } = 1;
    public string Tier { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum, Diamond
    
    // Streaks
    public int CurrentStreak { get; set; } // consecutive days logging transactions
    public int LongestStreak { get; set; }
    public DateTime? LastActivityDate { get; set; }
    
    // Financial Health Score (0-100, inspired by ClearScore)
    public int HealthScore { get; set; }
    public string HealthGrade { get; set; } = "Needs Work"; // Needs Work, Fair, Good, Great, Excellent
    
    // Insights
    public decimal MonthlyBudgetAdherence { get; set; } // percentage
    public decimal SavingsRate { get; set; } // percentage of income saved
    public decimal DebtToIncomeRatio { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Achievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // unique identifier e.g. "FIRST_BUDGET"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏆"; // emoji or icon name
    public string Category { get; set; } = string.Empty; // Budgeting, Saving, Tracking, Investing, Streak
    public int PointsAwarded { get; set; }
    public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard, Epic, Legendary
}

public class UserAchievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    
    public Achievement Achievement { get; set; } = null!;
}

public class PointTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty; // "Logged transaction", "Budget created", "7-day streak"
    public string Category { get; set; } = string.Empty; // Tracking, Budgeting, Saving, Achievement, Streak
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

// ─── Savings ────────────────────────────────────────────────────

public class SavingsGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // e.g. "Emergency Fund", "MacBook Pro", "Holiday"
    public string? Icon { get; set; } = "🎯";
    public string? Color { get; set; } = "#8B5CF6"; // purple default
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal? MonthlyContribution { get; set; }
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Status { get; set; } = "Active"; // Active, Paused, Completed, Cancelled
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SavingsDeposit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SavingsGoalId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime DepositDate { get; set; } = DateTime.UtcNow;
    
    public SavingsGoal SavingsGoal { get; set; } = null!;
}

public class SavingsChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "30-day", "52-week", "no-spend", "round-up"
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Completed, Failed, Paused
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public int CurrentDay { get; set; } // or current week
    public int TotalDays { get; set; } // or total weeks
    public string? ProgressData { get; set; } // JSON array of daily/weekly amounts
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ─── Financial Tips ─────────────────────────────────────────────

public class FinancialTip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Budgeting, Saving, Investing, Debt, Tax
    public string Difficulty { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
    public string? SourceUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
