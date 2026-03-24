namespace FinanceTracker.Domain.Services;

/// <summary>
/// South African savings interest calculator
/// Uses current SA bank savings account rates (2025/2026)
/// </summary>
public static class SavingsInterestCalculator
{
    // Common SA savings rates (approximate, updated periodically)
    public static readonly Dictionary<string, decimal> BankRates = new()
    {
        { "FNB Money Maximiser", 7.25m },
        { "Capitec Flexible Savings", 7.00m },
        { "TymeBank GoalSave", 8.50m },
        { "African Bank Notice Deposit (32-day)", 9.25m },
        { "Discovery Bank Savings", 7.50m },
        { "Nedbank Just Invest", 7.75m },
        { "Standard Bank PureSave", 6.50m },
        { "Absa Reward Saver", 6.75m },
        { "Old Mutual Money Account", 7.85m },
        { "SA Retail Bonds (2-year)", 9.75m },
    };

    /// <summary>
    /// Calculate future value with compound interest
    /// </summary>
    public static SavingsProjection Calculate(
        decimal initialAmount,
        decimal monthlyContribution,
        decimal annualRate,
        int months)
    {
        var monthlyRate = annualRate / 100 / 12;
        var currentBalance = initialAmount;
        var totalContributions = initialAmount;
        var projections = new List<MonthlyProjection>();

        for (int month = 1; month <= months; month++)
        {
            var interest = currentBalance * monthlyRate;
            currentBalance += interest + monthlyContribution;
            totalContributions += monthlyContribution;

            if (month % 3 == 0 || month == months || month == 1) // quarterly + first + last
            {
                projections.Add(new MonthlyProjection
                {
                    Month = month,
                    Balance = Math.Round(currentBalance, 2),
                    TotalContributions = Math.Round(totalContributions, 2),
                    TotalInterest = Math.Round(currentBalance - totalContributions, 2)
                });
            }
        }

        return new SavingsProjection
        {
            FinalBalance = Math.Round(currentBalance, 2),
            TotalContributions = Math.Round(totalContributions, 2),
            TotalInterest = Math.Round(currentBalance - totalContributions, 2),
            EffectiveRate = annualRate,
            Months = months,
            Projections = projections
        };
    }

    /// <summary>
    /// How many months to reach a target with given contribution
    /// </summary>
    public static int MonthsToTarget(decimal currentAmount, decimal monthlyContribution, decimal annualRate, decimal targetAmount)
    {
        if (monthlyContribution <= 0 && currentAmount >= targetAmount) return 0;
        if (monthlyContribution <= 0) return -1; // never

        var monthlyRate = annualRate / 100 / 12;
        var balance = currentAmount;
        int months = 0;

        while (balance < targetAmount && months < 600) // cap at 50 years
        {
            balance += balance * monthlyRate + monthlyContribution;
            months++;
        }

        return months;
    }
}

public class SavingsProjection
{
    public decimal FinalBalance { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal EffectiveRate { get; set; }
    public int Months { get; set; }
    public List<MonthlyProjection> Projections { get; set; } = new();
}

public class MonthlyProjection
{
    public int Month { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal TotalInterest { get; set; }
}
