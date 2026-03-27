namespace FinanceTracker.Domain.Services;

/// <summary>
/// Calculates next due dates, upcoming bills, and payday planning.
/// Pure domain logic.
/// </summary>
public static class BillScheduleCalculator
{
    /// <summary>
    /// Calculate the next occurrence date based on frequency and day
    /// </summary>
    public static DateTime CalculateNextDueDate(string frequency, int dayOfMonth, int? dayOfWeek, DateTime fromDate)
    {
        var today = fromDate.Date;

        return frequency switch
        {
            "Monthly" => GetNextMonthlyDate(dayOfMonth, today),
            "Weekly" => GetNextWeeklyDate(dayOfWeek ?? 1, today),
            "BiWeekly" => GetNextBiWeeklyDate(dayOfWeek ?? 1, today),
            "Quarterly" => GetNextQuarterlyDate(dayOfMonth, today),
            "Yearly" => GetNextYearlyDate(dayOfMonth, today),
            _ => GetNextMonthlyDate(dayOfMonth, today)
        };
    }

    private static DateTime GetNextMonthlyDate(int day, DateTime today)
    {
        var daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var adjustedDay = Math.Min(day, daysInCurrentMonth);
        var thisMonth = new DateTime(today.Year, today.Month, adjustedDay, 0, 0, 0, DateTimeKind.Utc);

        if (thisMonth > today)
            return thisMonth;

        var nextMonth = today.AddMonths(1);
        var daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        adjustedDay = Math.Min(day, daysInNextMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, adjustedDay, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime GetNextWeeklyDate(int targetDow, DateTime today)
    {
        var currentDow = (int)today.DayOfWeek;
        var daysUntil = (targetDow - currentDow + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return today.AddDays(daysUntil);
    }

    private static DateTime GetNextBiWeeklyDate(int targetDow, DateTime today)
    {
        var next = GetNextWeeklyDate(targetDow, today);
        return next.AddDays(7); // every 2 weeks
    }

    private static DateTime GetNextQuarterlyDate(int day, DateTime today)
    {
        var nextQuarterMonth = ((today.Month - 1) / 3 + 1) * 3 + 1;
        var nextQuarterYear = today.Year;
        if (nextQuarterMonth > 12)
        {
            nextQuarterMonth -= 12;
            nextQuarterYear++;
        }

        var daysInMonth = DateTime.DaysInMonth(nextQuarterYear, nextQuarterMonth);
        var adjustedDay = Math.Min(day, daysInMonth);
        var quarterDate = new DateTime(nextQuarterYear, nextQuarterMonth, adjustedDay, 0, 0, 0, DateTimeKind.Utc);

        if (quarterDate <= today)
            return CalculateNextDueDate("Quarterly", day, null, quarterDate.AddDays(1));

        return quarterDate;
    }

    private static DateTime GetNextYearlyDate(int day, DateTime today)
    {
        var thisYear = new DateTime(today.Year, today.Month, Math.Min(day, DateTime.DaysInMonth(today.Year, today.Month)), 0, 0, 0, DateTimeKind.Utc);
        return thisYear > today ? thisYear : thisYear.AddYears(1);
    }

    /// <summary>
    /// Calculate the payday plan — what's left after all bills
    /// </summary>
    public static PaydayPlanResult CalculatePaydayPlan(
        decimal monthlyIncome,
        int salaryDay,
        List<BillItem> bills)
    {
        var totalBills = bills.Sum(b => b.Amount);
        var totalDebtPayments = bills.Where(b => b.IsDebtPayment).Sum(b => b.Amount);
        var totalSavings = bills.Where(b => b.IsSavingsTransfer).Sum(b => b.Amount);
        var totalEssentials = bills.Where(b => !b.IsDebtPayment && !b.IsSavingsTransfer).Sum(b => b.Amount);
        var spendingMoney = monthlyIncome - totalBills;

        // Group bills by week of month
        var weeklyBreakdown = new List<WeekBreakdown>();
        for (int week = 1; week <= 5; week++)
        {
            var weekStart = (week - 1) * 7 + 1;
            var weekEnd = Math.Min(week * 7, 31);
            var weekBills = bills.Where(b => b.DueDay >= weekStart && b.DueDay <= weekEnd).ToList();
            weeklyBreakdown.Add(new WeekBreakdown
            {
                Week = week,
                Bills = weekBills,
                TotalDue = weekBills.Sum(b => b.Amount)
            });
        }

        return new PaydayPlanResult
        {
            MonthlyIncome = monthlyIncome,
            SalaryDay = salaryDay,
            TotalBills = totalBills,
            TotalDebtPayments = totalDebtPayments,
            TotalSavings = totalSavings,
            TotalEssentials = totalEssentials,
            SpendingMoney = spendingMoney,
            SpendingMoneyPerDay = spendingMoney > 0 ? Math.Round(spendingMoney / 30, 2) : 0,
            SpendingMoneyPerWeek = spendingMoney > 0 ? Math.Round(spendingMoney / 4, 2) : 0,
            WeeklyBreakdown = weeklyBreakdown,
            BillsPercentage = monthlyIncome > 0 ? Math.Round(totalBills / monthlyIncome * 100, 1) : 0
        };
    }
}

// ─── Types ──────────────────────────────────────────────────────

public class BillItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DueDay { get; set; }
    public bool IsDebtPayment { get; set; }
    public bool IsSavingsTransfer { get; set; }
}

public class PaydayPlanResult
{
    public decimal MonthlyIncome { get; set; }
    public int SalaryDay { get; set; }
    public decimal TotalBills { get; set; }
    public decimal TotalDebtPayments { get; set; }
    public decimal TotalSavings { get; set; }
    public decimal TotalEssentials { get; set; }
    public decimal SpendingMoney { get; set; }
    public decimal SpendingMoneyPerDay { get; set; }
    public decimal SpendingMoneyPerWeek { get; set; }
    public decimal BillsPercentage { get; set; }
    public List<WeekBreakdown> WeeklyBreakdown { get; set; } = new();
}

public class WeekBreakdown
{
    public int Week { get; set; }
    public List<BillItem> Bills { get; set; } = new();
    public decimal TotalDue { get; set; }
}
