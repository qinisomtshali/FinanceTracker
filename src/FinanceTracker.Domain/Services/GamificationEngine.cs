namespace FinanceTracker.Domain.Services;

/// <summary>
/// Core gamification engine — calculates points, levels, tiers, streaks, and financial health scores.
/// Pure domain logic, no external dependencies.
/// </summary>
public static class GamificationEngine
{
    // ─── Points System ──────────────────────────────────────────

    public static class PointValues
    {
        public const int LogTransaction = 5;
        public const int CreateBudget = 20;
        public const int StayUnderBudget = 50; // per budget per month
        public const int CreateSavingsGoal = 15;
        public const int MakeSavingsDeposit = 10;
        public const int CompleteSavingsGoal = 100;
        public const int StartChallenge = 10;
        public const int CompleteChallenge = 200;
        public const int DailyLogin = 2;
        public const int WeekStreak = 25; // 7-day streak bonus
        public const int MonthStreak = 100; // 30-day streak bonus
        public const int CreateInvoice = 10;
        public const int InvoicePaid = 15;
        public const int FirstTransaction = 50; // one-time bonus
        public const int CalculateTax = 5;
    }

    // ─── Level Thresholds ───────────────────────────────────────

    private static readonly (int MinPoints, int Level, string Tier)[] LevelTable =
    {
        (0, 1, "Bronze"),
        (100, 2, "Bronze"),
        (250, 3, "Bronze"),
        (500, 4, "Silver"),
        (1000, 5, "Silver"),
        (2000, 6, "Silver"),
        (3500, 7, "Gold"),
        (5000, 8, "Gold"),
        (7500, 9, "Gold"),
        (10000, 10, "Platinum"),
        (15000, 11, "Platinum"),
        (20000, 12, "Platinum"),
        (30000, 13, "Diamond"),
        (50000, 14, "Diamond"),
        (75000, 15, "Diamond"),
    };

    public static (int Level, string Tier, int PointsToNextLevel) CalculateLevel(int totalPoints)
    {
        var current = LevelTable[0];
        var next = LevelTable.Length > 1 ? LevelTable[1] : current;

        for (int i = LevelTable.Length - 1; i >= 0; i--)
        {
            if (totalPoints >= LevelTable[i].MinPoints)
            {
                current = LevelTable[i];
                next = i < LevelTable.Length - 1 ? LevelTable[i + 1] : current;
                break;
            }
        }

        var pointsToNext = next.MinPoints - totalPoints;
        if (pointsToNext < 0) pointsToNext = 0;

        return (current.Level, current.Tier, pointsToNext);
    }

    // ─── Financial Health Score (0–100) ─────────────────────────
    // Inspired by ClearScore / Experian credit score concept
    // but based on actual financial behavior, not credit bureau data.

    public static FinancialHealthResult CalculateHealthScore(HealthScoreInput input)
    {
        var scores = new List<(string Category, int Score, int MaxScore, string Status, string Tip)>();

        // 1. Budget Adherence (0-25 points)
        int budgetScore = 0;
        string budgetStatus;
        string budgetTip;
        if (input.TotalBudgets == 0)
        {
            budgetScore = 0;
            budgetStatus = "No budgets set";
            budgetTip = "Create your first budget to start tracking your spending habits.";
        }
        else
        {
            var adherence = input.BudgetsUnderLimit / (decimal)input.TotalBudgets * 100;
            budgetScore = (int)(adherence / 100 * 25);
            budgetStatus = adherence >= 80 ? "Excellent" : adherence >= 60 ? "Good" : adherence >= 40 ? "Needs Work" : "Poor";
            budgetTip = adherence >= 80 
                ? "You're staying within your budgets. Keep it up!" 
                : "Try reducing spending in your over-budget categories.";
        }
        scores.Add(("Budgeting", budgetScore, 25, budgetStatus, budgetTip));

        // 2. Savings Rate (0-25 points)
        int savingsScore = 0;
        string savingsStatus;
        string savingsTip;
        if (input.MonthlyIncome <= 0)
        {
            savingsScore = 0;
            savingsStatus = "No income logged";
            savingsTip = "Log your income to track your savings rate.";
        }
        else
        {
            var savingsRate = input.MonthlySavings / input.MonthlyIncome * 100;
            savingsScore = savingsRate switch
            {
                >= 20 => 25,
                >= 15 => 20,
                >= 10 => 15,
                >= 5 => 10,
                > 0 => 5,
                _ => 0
            };
            savingsStatus = savingsRate >= 20 ? "Excellent" : savingsRate >= 10 ? "Good" : savingsRate > 0 ? "Needs Work" : "Not saving";
            savingsTip = savingsRate >= 20 
                ? $"Amazing! You're saving {savingsRate:F0}% of your income." 
                : "Aim to save at least 20% of your income. Start with small automatic transfers.";
        }
        scores.Add(("Savings", savingsScore, 25, savingsStatus, savingsTip));

        // 3. Emergency Fund (0-20 points)
        int emergencyScore;
        string emergencyStatus;
        string emergencyTip;
        if (input.MonthlyExpenses <= 0)
        {
            emergencyScore = 0;
            emergencyStatus = "Unknown";
            emergencyTip = "Track your expenses to assess your emergency fund needs.";
        }
        else
        {
            var monthsCovered = input.EmergencyFundBalance / input.MonthlyExpenses;
            emergencyScore = monthsCovered switch
            {
                >= 6 => 20,
                >= 3 => 15,
                >= 1 => 10,
                > 0 => 5,
                _ => 0
            };
            emergencyStatus = monthsCovered >= 6 ? "Fully funded" : monthsCovered >= 3 ? "Good progress" : monthsCovered >= 1 ? "Building" : "Not started";
            emergencyTip = monthsCovered >= 6 
                ? $"Your emergency fund covers {monthsCovered:F1} months of expenses. Excellent!" 
                : $"Your fund covers {monthsCovered:F1} months. Aim for 3-6 months of expenses.";
        }
        scores.Add(("Emergency Fund", emergencyScore, 20, emergencyStatus, emergencyTip));

        // 4. Tracking Consistency (0-15 points)
        int trackingScore;
        string trackingStatus;
        string trackingTip;
        var daysActive = input.DaysActiveThisMonth;
        trackingScore = daysActive switch
        {
            >= 25 => 15,
            >= 20 => 12,
            >= 15 => 10,
            >= 10 => 7,
            >= 5 => 4,
            > 0 => 2,
            _ => 0
        };
        trackingStatus = daysActive >= 20 ? "Excellent" : daysActive >= 10 ? "Good" : daysActive > 0 ? "Inconsistent" : "Inactive";
        trackingTip = daysActive >= 20 
            ? "You're logging transactions consistently. Great habit!" 
            : "Try to log your expenses daily. Consistency is key to financial awareness.";
        scores.Add(("Tracking", trackingScore, 15, trackingStatus, trackingTip));

        // 5. Spending Diversity (0-15 points) — are they overspending in one category?
        int diversityScore;
        string diversityStatus;
        string diversityTip;
        if (input.TopCategoryPercentage <= 0)
        {
            diversityScore = 0;
            diversityStatus = "No data";
            diversityTip = "Start categorizing your transactions to see spending patterns.";
        }
        else
        {
            diversityScore = input.TopCategoryPercentage switch
            {
                <= 30 => 15, // well diversified
                <= 40 => 12,
                <= 50 => 8,
                <= 70 => 4,
                _ => 0 // one category dominates
            };
            diversityStatus = input.TopCategoryPercentage <= 30 ? "Well balanced" : input.TopCategoryPercentage <= 50 ? "Moderate" : "Concentrated";
            diversityTip = input.TopCategoryPercentage <= 30 
                ? "Your spending is well diversified across categories." 
                : $"Your top category is {input.TopCategoryPercentage:F0}% of spending. Consider if this aligns with your priorities.";
        }
        scores.Add(("Spending Balance", diversityScore, 15, diversityStatus, diversityTip));

        // Total
        var totalScore = scores.Sum(s => s.Score);
        var maxScore = scores.Sum(s => s.MaxScore);
        var grade = totalScore switch
        {
            >= 85 => "Excellent",
            >= 70 => "Great",
            >= 55 => "Good",
            >= 35 => "Fair",
            _ => "Needs Work"
        };

        return new FinancialHealthResult
        {
            TotalScore = totalScore,
            MaxScore = maxScore,
            Grade = grade,
            Categories = scores.Select(s => new HealthCategory
            {
                Name = s.Category,
                Score = s.Score,
                MaxScore = s.MaxScore,
                Status = s.Status,
                Tip = s.Tip
            }).ToList()
        };
    }

    // ─── Streak Logic ───────────────────────────────────────────

    public static (int NewStreak, bool IsNewDay, int BonusPoints) UpdateStreak(
        int currentStreak, DateTime? lastActivityDate)
    {
        var today = DateTime.UtcNow.Date;
        
        if (lastActivityDate == null)
            return (1, true, PointValues.DailyLogin);

        var lastDate = lastActivityDate.Value.Date;

        if (lastDate == today)
            return (currentStreak, false, 0); // already logged today

        if (lastDate == today.AddDays(-1))
        {
            // consecutive day
            var newStreak = currentStreak + 1;
            var bonus = PointValues.DailyLogin;
            
            if (newStreak % 7 == 0) bonus += PointValues.WeekStreak;
            if (newStreak % 30 == 0) bonus += PointValues.MonthStreak;

            return (newStreak, true, bonus);
        }

        // streak broken
        return (1, true, PointValues.DailyLogin);
    }

    // ─── Savings Challenge Templates ────────────────────────────

    public static SavingsChallengeTemplate GetChallengeTemplate(string type) => type switch
    {
        "30-day" => new SavingsChallengeTemplate
        {
            Name = "30-Day Savings Sprint",
            Description = "Save an increasing amount each day for 30 days. Day 1 = R1, Day 2 = R2... Day 30 = R30.",
            TotalDays = 30,
            TargetAmount = 465m, // sum of 1 to 30
            DailyAmounts = Enumerable.Range(1, 30).Select(d => (decimal)d).ToList()
        },
        "52-week" => new SavingsChallengeTemplate
        {
            Name = "52-Week Money Challenge",
            Description = "Save an increasing amount each week. Week 1 = R10, Week 2 = R20... Week 52 = R520.",
            TotalDays = 364,
            TargetAmount = 13780m, // sum of 10,20,30...520
            DailyAmounts = Enumerable.Range(1, 52).Select(w => w * 10m).ToList()
        },
        "no-spend" => new SavingsChallengeTemplate
        {
            Name = "No-Spend Weekend Challenge",
            Description = "Go an entire weekend (Sat-Sun) without spending any money. Track 4 weekends.",
            TotalDays = 28,
            TargetAmount = 0m,
            DailyAmounts = new List<decimal>()
        },
        _ => throw new ArgumentException($"Unknown challenge type: {type}")
    };
}

// ─── Supporting Types ───────────────────────────────────────────

public class HealthScoreInput
{
    public int TotalBudgets { get; set; }
    public int BudgetsUnderLimit { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlySavings { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal EmergencyFundBalance { get; set; }
    public int DaysActiveThisMonth { get; set; }
    public decimal TopCategoryPercentage { get; set; } // percentage of spend in biggest category
}

public class FinancialHealthResult
{
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public List<HealthCategory> Categories { get; set; } = new();
}

public class HealthCategory
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
}

public class SavingsChallengeTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public decimal TargetAmount { get; set; }
    public List<decimal> DailyAmounts { get; set; } = new();
}
