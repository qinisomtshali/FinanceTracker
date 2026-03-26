namespace FinanceTracker.Domain.Services;

/// <summary>
/// Debt payoff calculator — Snowball vs Avalanche strategies
/// Pure domain logic, no external dependencies.
/// </summary>
public static class DebtPayoffCalculator
{
    /// <summary>
    /// Calculate payoff plan with current payments (no strategy, just minimum/actual payments)
    /// </summary>
    public static PayoffPlanResult CalculateCurrentPlan(List<DebtInput> debts)
    {
        var timeline = new List<PayoffMilestone>();
        var working = debts.Select(d => new WorkingDebt(d)).ToList();
        int month = 0;
        decimal totalInterestPaid = 0;
        int maxMonths = 600; // 50 year cap

        while (working.Any(d => d.Balance > 0) && month < maxMonths)
        {
            month++;
            foreach (var d in working.Where(d => d.Balance > 0))
            {
                var interest = d.Balance * d.MonthlyRate;
                totalInterestPaid += interest;
                d.Balance += interest;

                var payment = Math.Min(d.ActualPayment, d.Balance);
                d.Balance -= payment;
                d.TotalPaid += payment;

                if (d.Balance <= 0.01m)
                {
                    d.Balance = 0;
                    d.PaidOffMonth = month;
                    timeline.Add(new PayoffMilestone(d.Name, month, d.TotalPaid));
                }
            }
        }

        return new PayoffPlanResult
        {
            Strategy = "Current",
            TotalMonths = month,
            TotalInterestPaid = Math.Round(totalInterestPaid, 2),
            TotalAmountPaid = Math.Round(working.Sum(d => d.TotalPaid), 2),
            DebtFreeDate = DateTime.UtcNow.AddMonths(month),
            Timeline = timeline.OrderBy(t => t.PaidOffMonth).ToList(),
            PayoffOrder = timeline.OrderBy(t => t.PaidOffMonth).Select(t => t.DebtName).ToList()
        };
    }

    /// <summary>
    /// Debt Snowball — pay smallest balance first (Dave Ramsey method)
    /// </summary>
    public static PayoffPlanResult CalculateSnowball(List<DebtInput> debts, decimal extraMonthlyPayment = 0)
    {
        return CalculateStrategy(debts, extraMonthlyPayment, "Snowball",
            remaining => remaining.OrderBy(d => d.Balance).First());
    }

    /// <summary>
    /// Debt Avalanche — pay highest interest rate first (mathematically optimal)
    /// </summary>
    public static PayoffPlanResult CalculateAvalanche(List<DebtInput> debts, decimal extraMonthlyPayment = 0)
    {
        return CalculateStrategy(debts, extraMonthlyPayment, "Avalanche",
            remaining => remaining.OrderByDescending(d => d.InterestRate).First());
    }

    private static PayoffPlanResult CalculateStrategy(
        List<DebtInput> debts,
        decimal extraMonthlyPayment,
        string strategyName,
        Func<List<WorkingDebt>, WorkingDebt> selectTarget)
    {
        var timeline = new List<PayoffMilestone>();
        var working = debts.Select(d => new WorkingDebt(d)).ToList();
        int month = 0;
        decimal totalInterestPaid = 0;
        decimal freedUpPayment = 0; // payments from paid-off debts that roll into next target
        int maxMonths = 600;

        while (working.Any(d => d.Balance > 0) && month < maxMonths)
        {
            month++;

            // Apply interest to all active debts
            foreach (var d in working.Where(d => d.Balance > 0))
            {
                var interest = d.Balance * d.MonthlyRate;
                totalInterestPaid += interest;
                d.Balance += interest;
            }

            // Pay minimum on all debts
            foreach (var d in working.Where(d => d.Balance > 0))
            {
                var minPayment = Math.Min(d.MinimumPayment, d.Balance);
                d.Balance -= minPayment;
                d.TotalPaid += minPayment;

                if (d.Balance <= 0.01m)
                {
                    d.Balance = 0;
                    d.PaidOffMonth = month;
                    freedUpPayment += d.MinimumPayment;
                    timeline.Add(new PayoffMilestone(d.Name, month, d.TotalPaid));
                }
            }

            // Apply extra payment + freed up payments to target debt
            var remaining = working.Where(d => d.Balance > 0).ToList();
            if (remaining.Any())
            {
                var target = selectTarget(remaining);
                var extraTotal = extraMonthlyPayment + freedUpPayment;
                var extraPayment = Math.Min(extraTotal, target.Balance);
                target.Balance -= extraPayment;
                target.TotalPaid += extraPayment;

                if (target.Balance <= 0.01m)
                {
                    target.Balance = 0;
                    target.PaidOffMonth = month;
                    freedUpPayment += target.MinimumPayment;
                    timeline.Add(new PayoffMilestone(target.Name, month, target.TotalPaid));
                }
            }
        }

        return new PayoffPlanResult
        {
            Strategy = strategyName,
            TotalMonths = month,
            TotalInterestPaid = Math.Round(totalInterestPaid, 2),
            TotalAmountPaid = Math.Round(working.Sum(d => d.TotalPaid), 2),
            DebtFreeDate = DateTime.UtcNow.AddMonths(month),
            Timeline = timeline.OrderBy(t => t.PaidOffMonth).ToList(),
            PayoffOrder = timeline.OrderBy(t => t.PaidOffMonth).Select(t => t.DebtName).ToList()
        };
    }

    /// <summary>
    /// Compare all three strategies side by side
    /// </summary>
    public static StrategyComparisonResult CompareStrategies(List<DebtInput> debts, decimal extraMonthlyPayment = 0)
    {
        var current = CalculateCurrentPlan(debts);
        var snowball = CalculateSnowball(debts, extraMonthlyPayment);
        var avalanche = CalculateAvalanche(debts, extraMonthlyPayment);

        var bestStrategy = avalanche.TotalInterestPaid <= snowball.TotalInterestPaid ? "Avalanche" : "Snowball";

        return new StrategyComparisonResult
        {
            ExtraMonthlyPayment = extraMonthlyPayment,
            CurrentPlan = current,
            Snowball = snowball,
            Avalanche = avalanche,
            RecommendedStrategy = bestStrategy,
            MonthsSavedVsCurrent = current.TotalMonths - Math.Min(snowball.TotalMonths, avalanche.TotalMonths),
            InterestSavedVsCurrent = current.TotalInterestPaid - Math.Min(snowball.TotalInterestPaid, avalanche.TotalInterestPaid),
            Summary = GenerateSummary(current, snowball, avalanche, extraMonthlyPayment)
        };
    }

    private static string GenerateSummary(PayoffPlanResult current, PayoffPlanResult snowball, PayoffPlanResult avalanche, decimal extra)
    {
        var best = avalanche.TotalInterestPaid <= snowball.TotalInterestPaid ? avalanche : snowball;
        var monthsSaved = current.TotalMonths - best.TotalMonths;
        var interestSaved = current.TotalInterestPaid - best.TotalInterestPaid;

        if (extra > 0)
            return $"By paying R{extra:N0} extra per month using the {best.Strategy} method, " +
                   $"you'll be debt-free {monthsSaved} months sooner and save R{interestSaved:N0} in interest.";
        else
            return $"Using the {best.Strategy} method (same total payments), " +
                   $"you'll be debt-free {monthsSaved} months sooner and save R{interestSaved:N0} in interest.";
    }

    /// <summary>
    /// SA-specific debt insights and typical interest rates
    /// </summary>
    public static List<DebtInsight> GetSAInsights() => new()
    {
        new("Credit Cards", "18-22%", 20m, "Pay more than the minimum. At 20% interest, a R10,000 balance with minimum payments takes over 7 years to pay off and costs R8,000+ in interest."),
        new("Store Cards", "18-21%", 19.5m, "Woolworths, Edgars, Mr Price, Truworths — store cards have some of the highest interest rates. Avoid buying on credit if you can save and pay cash."),
        new("Personal Loans", "15-28%", 21m, "Shop around. Banks like Capitec, TymeBank, and African Bank offer different rates. A 1% difference on R50,000 saves you R2,500+ over 3 years."),
        new("Car Finance", "10-15%", 12m, "Consider a shorter loan term (48 vs 72 months). You'll pay more monthly but save tens of thousands in interest. A R300,000 car at 12% over 72 months costs R432,000 total."),
        new("Home Loans", "11-13%", 11.75m, "Making one extra payment per year can cut 3-4 years off a 20-year bond. On a R1M bond at 11.75%, that saves R380,000+ in interest."),
        new("Student Loans (NSFAS)", "0%", 0m, "NSFAS converted to a bursary for qualifying students since 2018. If you have an older NSFAS loan, you only start paying when you earn above a threshold. Don't ignore it — it affects your credit score."),
        new("Debt Counselling", "", 0m, "If your debt payments exceed 50% of your income, consider NCR-registered debt counselling. It's not a loan — it restructures your payments and protects you from legal action. Contact the NCR at 0860 627 627."),
        new("National Credit Act", "", 0m, "Under the NCA, creditors must assess your ability to repay before lending. If you were given credit recklessly, you may have grounds to challenge the debt. Consult a debt counsellor or legal aid.")
    };

    // ─── Internal Working Class ─────────────────────────────────

    private class WorkingDebt
    {
        public string Name { get; }
        public decimal Balance { get; set; }
        public decimal MinimumPayment { get; }
        public decimal ActualPayment { get; }
        public decimal InterestRate { get; }
        public decimal MonthlyRate { get; }
        public decimal TotalPaid { get; set; }
        public int PaidOffMonth { get; set; }

        public WorkingDebt(DebtInput input)
        {
            Name = input.Name;
            Balance = input.CurrentBalance;
            MinimumPayment = input.MinimumPayment;
            ActualPayment = input.ActualPayment > 0 ? input.ActualPayment : input.MinimumPayment;
            InterestRate = input.InterestRate;
            MonthlyRate = input.InterestRate / 100 / 12;
            TotalPaid = 0;
            PaidOffMonth = 0;
        }
    }
}

// ─── Input / Output Types ───────────────────────────────────────

public class DebtInput
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MinimumPayment { get; set; }
    public decimal ActualPayment { get; set; }
}

public class PayoffPlanResult
{
    public string Strategy { get; set; } = string.Empty;
    public int TotalMonths { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal TotalAmountPaid { get; set; }
    public DateTime DebtFreeDate { get; set; }
    public List<PayoffMilestone> Timeline { get; set; } = new();
    public List<string> PayoffOrder { get; set; } = new();
}

public record PayoffMilestone(string DebtName, int PaidOffMonth, decimal TotalPaid);

public class StrategyComparisonResult
{
    public decimal ExtraMonthlyPayment { get; set; }
    public PayoffPlanResult CurrentPlan { get; set; } = null!;
    public PayoffPlanResult Snowball { get; set; } = null!;
    public PayoffPlanResult Avalanche { get; set; } = null!;
    public string RecommendedStrategy { get; set; } = string.Empty;
    public int MonthsSavedVsCurrent { get; set; }
    public decimal InterestSavedVsCurrent { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public record DebtInsight(string DebtType, string TypicalRate, decimal AverageRate, string Tip);
