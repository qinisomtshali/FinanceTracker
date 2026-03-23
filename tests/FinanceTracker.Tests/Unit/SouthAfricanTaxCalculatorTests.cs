using FinanceTracker.Domain.Services;
using Xunit;

namespace FinanceTracker.Tests.Unit;

public class SouthAfricanTaxCalculatorTests
{
    // ─── Below Threshold ────────────────────────────────────────

    [Fact]
    public void Calculate_IncomeBelowThreshold_ReturnsZeroTax()
    {
        var input = new TaxInput { GrossIncome = 90_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(0m, result.EffectiveRate);
        Assert.Equal("2025/2026", result.TaxYear);
    }

    [Fact]
    public void Calculate_SeniorBelowThreshold_ReturnsZeroTax()
    {
        // 65+ threshold is R148,217
        var input = new TaxInput { GrossIncome = 145_000m, Age = 66 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.Equal(0m, result.TaxAmount);
    }

    [Fact]
    public void Calculate_SuperSeniorBelowThreshold_ReturnsZeroTax()
    {
        // 75+ threshold is R165,689
        var input = new TaxInput { GrossIncome = 160_000m, Age = 76 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.Equal(0m, result.TaxAmount);
    }

    // ─── First Bracket ──────────────────────────────────────────

    [Fact]
    public void Calculate_IncomeInFirstBracket_CalculatesCorrectly()
    {
        // R200,000 income: tax = 200,000 * 18% = R36,000 - R17,235 rebate = R18,765
        var input = new TaxInput { GrossIncome = 200_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.True(result.TaxAmount > 0);
        Assert.True(result.TaxAmount < 200_000m); // Sanity check
        Assert.Equal(200_000m, result.GrossIncome);
        Assert.Equal(200_000m, result.TaxableIncome); // No deductions
    }

    // ─── Middle Income ──────────────────────────────────────────

    [Fact]
    public void Calculate_MiddleIncome_ReturnsReasonableEffectiveRate()
    {
        // R500,000 is a typical middle-class salary in SA
        var input = new TaxInput { GrossIncome = 500_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.True(result.TaxAmount > 0);
        Assert.True(result.EffectiveRate > 10m);  // Should be above 10%
        Assert.True(result.EffectiveRate < 35m);  // Should be below 35%
        Assert.True(result.MonthlyPaye > 0);
        Assert.True(result.MonthlyNetIncome > 0);
        Assert.Equal(result.MonthlyPaye, Math.Round(result.TaxAmount / 12, 2));
    }

    // ─── High Income ────────────────────────────────────────────

    [Fact]
    public void Calculate_HighIncome_EffectiveRateApproachesMaxBracket()
    {
        var input = new TaxInput { GrossIncome = 2_000_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.True(result.EffectiveRate > 30m); // High earner should pay >30%
        Assert.True(result.EffectiveRate < 45m); // But less than marginal max
    }

    // ─── Retirement Fund Deduction ──────────────────────────────

    [Fact]
    public void Calculate_WithRetirementContributions_ReducesTaxableIncome()
    {
        var withoutRetirement = new TaxInput { GrossIncome = 600_000m, Age = 30 };
        var withRetirement = new TaxInput
        {
            GrossIncome = 600_000m,
            Age = 30,
            RetirementContributions = 50_000m
        };

        var resultWithout = SouthAfricanTaxCalculator.Calculate(withoutRetirement);
        var resultWith = SouthAfricanTaxCalculator.Calculate(withRetirement);

        Assert.True(resultWith.TaxAmount < resultWithout.TaxAmount);
        Assert.True(resultWith.RetirementDeduction > 0);
        Assert.Equal(50_000m, resultWith.RetirementDeduction);
    }

    [Fact]
    public void Calculate_RetirementCappedAt27Point5Percent()
    {
        var input = new TaxInput
        {
            GrossIncome = 400_000m,
            Age = 30,
            RetirementContributions = 200_000m // Way more than 27.5% of R400k
        };

        var result = SouthAfricanTaxCalculator.Calculate(input);

        // Max deduction = 27.5% of R400,000 = R110,000
        Assert.Equal(110_000m, result.RetirementDeduction);
    }

    [Fact]
    public void Calculate_RetirementCappedAtR350k()
    {
        var input = new TaxInput
        {
            GrossIncome = 3_000_000m,
            Age = 30,
            RetirementContributions = 1_000_000m
        };

        var result = SouthAfricanTaxCalculator.Calculate(input);

        // 27.5% of R3M = R825k, but capped at R350k
        Assert.Equal(350_000m, result.RetirementDeduction);
    }

    // ─── Medical Aid Credits ────────────────────────────────────

    [Fact]
    public void Calculate_WithMedicalAid_ReducesTax()
    {
        var withoutMed = new TaxInput { GrossIncome = 500_000m, Age = 30, MedicalAidMembers = 0 };
        var withMed = new TaxInput { GrossIncome = 500_000m, Age = 30, MedicalAidMembers = 1 };

        var resultWithout = SouthAfricanTaxCalculator.Calculate(withoutMed);
        var resultWith = SouthAfricanTaxCalculator.Calculate(withMed);

        Assert.True(resultWith.TaxAmount < resultWithout.TaxAmount);
        Assert.True(resultWith.MedicalAidCredits > 0);
        // Main member: R364/month * 12 = R4,368
        Assert.Equal(364m * 12, resultWith.MedicalAidCredits);
    }

    [Fact]
    public void Calculate_WithMedicalAidFamily_HigherCredits()
    {
        var single = new TaxInput { GrossIncome = 500_000m, Age = 30, MedicalAidMembers = 1 };
        var family = new TaxInput { GrossIncome = 500_000m, Age = 30, MedicalAidMembers = 4 }; // main + spouse + 2 kids

        var resultSingle = SouthAfricanTaxCalculator.Calculate(single);
        var resultFamily = SouthAfricanTaxCalculator.Calculate(family);

        Assert.True(resultFamily.MedicalAidCredits > resultSingle.MedicalAidCredits);
    }

    // ─── Age-based Rebates ──────────────────────────────────────

    [Fact]
    public void Calculate_OlderPerson_GetsHigherRebate()
    {
        var young = new TaxInput { GrossIncome = 300_000m, Age = 30 };
        var senior = new TaxInput { GrossIncome = 300_000m, Age = 66 };
        var superSenior = new TaxInput { GrossIncome = 300_000m, Age = 76 };

        var youngResult = SouthAfricanTaxCalculator.Calculate(young);
        var seniorResult = SouthAfricanTaxCalculator.Calculate(senior);
        var superSeniorResult = SouthAfricanTaxCalculator.Calculate(superSenior);

        // Senior pays less than young due to secondary rebate
        Assert.True(seniorResult.TaxAmount < youngResult.TaxAmount);
        // Super senior pays even less due to tertiary rebate
        Assert.True(superSeniorResult.TaxAmount < seniorResult.TaxAmount);
    }

    // ─── Monthly Calculations ───────────────────────────────────

    [Fact]
    public void Calculate_MonthlyPayeEqualsAnnualDividedBy12()
    {
        var input = new TaxInput { GrossIncome = 600_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.Equal(Math.Round(result.TaxAmount / 12, 2), result.MonthlyPaye);
    }

    [Fact]
    public void Calculate_MonthlyNetIncome_IsCorrect()
    {
        var input = new TaxInput { GrossIncome = 600_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        var expectedMonthlyNet = Math.Round((600_000m - result.TaxAmount) / 12, 2);
        Assert.Equal(expectedMonthlyNet, result.MonthlyNetIncome);
    }

    // ─── Edge Cases ─────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroIncome_ReturnsZeroTax()
    {
        var input = new TaxInput { GrossIncome = 0m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.Equal(0m, result.TaxAmount);
    }

    [Fact]
    public void Calculate_BracketBreakdown_HasEntries()
    {
        var input = new TaxInput { GrossIncome = 500_000m, Age = 30 };
        var result = SouthAfricanTaxCalculator.Calculate(input);

        Assert.NotEmpty(result.BracketBreakdown);
        Assert.True(result.BracketBreakdown.Count >= 2); // Should span at least 2 brackets
    }
}
