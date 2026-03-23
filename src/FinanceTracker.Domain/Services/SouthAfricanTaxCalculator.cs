using System.Text.Json;

namespace FinanceTracker.Domain.Services;

/// <summary>
/// South African Income Tax Calculator
/// Based on SARS tax tables for the 2025/2026 tax year (1 March 2025 – 28 February 2026)
/// </summary>
public static class SouthAfricanTaxCalculator
{
    // 2025/2026 Tax Brackets
    private static readonly (decimal LowerBound, decimal UpperBound, decimal Rate, decimal BaseAmount)[] TaxBrackets2025 =
    {
        (0m,        237_100m,   0.18m, 0m),
        (237_101m,  370_500m,   0.26m, 42_678m),
        (370_501m,  512_800m,   0.31m, 77_362m),
        (512_801m,  673_000m,   0.36m, 121_475m),
        (673_001m,  857_900m,   0.39m, 179_147m),
        (857_901m,  1_817_000m, 0.41m, 251_258m),
        (1_817_001m, decimal.MaxValue, 0.45m, 644_489m),
    };

    // Tax Rebates 2025/2026
    private static readonly decimal PrimaryRebate = 17_235m;      // All taxpayers
    private static readonly decimal SecondaryRebate = 9_444m;     // 65 and older
    private static readonly decimal TertiaryRebate = 3_145m;      // 75 and older

    // Tax Thresholds 2025/2026
    private static readonly decimal ThresholdBelow65 = 95_750m;
    private static readonly decimal Threshold65To74 = 148_217m;
    private static readonly decimal Threshold75Plus = 165_689m;

    // Medical Aid Credits (per month)
    private static readonly decimal MedicalCreditMainMember = 364m;
    private static readonly decimal MedicalCreditFirstDependant = 364m;
    private static readonly decimal MedicalCreditAdditionalDependant = 246m;

    public static TaxResult Calculate(TaxInput input)
    {
        var result = new TaxResult
        {
            TaxYear = "2025/2026",
            GrossIncome = input.GrossIncome
        };

        // Step 1: Calculate taxable income
        decimal retirementDeduction = CalculateRetirementDeduction(input);
        result.RetirementDeduction = retirementDeduction;

        decimal taxableIncome = input.GrossIncome - retirementDeduction;
        if (taxableIncome < 0) taxableIncome = 0;
        result.TaxableIncome = taxableIncome;

        // Step 2: Check threshold
        decimal threshold = input.Age switch
        {
            >= 75 => Threshold75Plus,
            >= 65 => Threshold65To74,
            _ => ThresholdBelow65
        };

        if (taxableIncome <= threshold)
        {
            result.TaxAmount = 0;
            result.EffectiveRate = 0;
            result.BracketBreakdown = new List<BracketDetail>();
            return result;
        }

        // Step 3: Calculate tax from brackets
        decimal grossTax = CalculateFromBrackets(taxableIncome, out var bracketDetails);
        result.BracketBreakdown = bracketDetails;

        // Step 4: Subtract rebates
        decimal totalRebate = PrimaryRebate;
        if (input.Age >= 65) totalRebate += SecondaryRebate;
        if (input.Age >= 75) totalRebate += TertiaryRebate;
        result.Rebates = totalRebate;

        // Step 5: Medical aid credits
        decimal medicalCredits = 0;
        if (input.MedicalAidMembers > 0)
        {
            medicalCredits = MedicalCreditMainMember * 12;
            if (input.MedicalAidMembers >= 2)
                medicalCredits += MedicalCreditFirstDependant * 12;
            if (input.MedicalAidMembers > 2)
                medicalCredits += (input.MedicalAidMembers - 2) * MedicalCreditAdditionalDependant * 12;
        }
        result.MedicalAidCredits = medicalCredits;

        // Step 6: Final tax
        decimal finalTax = grossTax - totalRebate - medicalCredits;
        if (finalTax < 0) finalTax = 0;

        result.TaxAmount = Math.Round(finalTax, 2);
        result.EffectiveRate = taxableIncome > 0
            ? Math.Round(finalTax / taxableIncome * 100, 2)
            : 0;
        result.MonthlyPaye = Math.Round(finalTax / 12, 2);
        result.MonthlyNetIncome = Math.Round((input.GrossIncome - finalTax) / 12, 2);

        return result;
    }

    private static decimal CalculateRetirementDeduction(TaxInput input)
    {
        if (input.RetirementContributions <= 0) return 0;

        // Max 27.5% of the greater of remuneration or taxable income, capped at R350,000
        decimal maxDeduction = Math.Min(
            input.GrossIncome * 0.275m,
            350_000m
        );

        return Math.Min(input.RetirementContributions, maxDeduction);
    }

    private static decimal CalculateFromBrackets(decimal taxableIncome, out List<BracketDetail> details)
    {
        details = new List<BracketDetail>();
        decimal tax = 0;

        for (int i = 0; i < TaxBrackets2025.Length; i++)
        {
            var bracket = TaxBrackets2025[i];
            if (taxableIncome >= bracket.LowerBound)
            {
                decimal upperLimit = Math.Min(taxableIncome, bracket.UpperBound);
                decimal taxableInBracket = upperLimit - bracket.LowerBound + (i == 0 ? 0 : 1);
                
                if (i == 0)
                {
                    taxableInBracket = Math.Min(taxableIncome, bracket.UpperBound);
                }
                else
                {
                    taxableInBracket = Math.Min(taxableIncome, bracket.UpperBound) - bracket.LowerBound + 1;
                }

                decimal taxInBracket;
                if (i == 0)
                {
                    taxInBracket = taxableInBracket * bracket.Rate;
                }
                else
                {
                    decimal excessOverLower = taxableIncome - bracket.LowerBound + 1;
                    if (taxableIncome > bracket.UpperBound)
                        excessOverLower = bracket.UpperBound - bracket.LowerBound + 1;
                    taxInBracket = excessOverLower * bracket.Rate;
                }

                details.Add(new BracketDetail
                {
                    BracketRange = $"R{bracket.LowerBound:N0} – R{(bracket.UpperBound == decimal.MaxValue ? "+" : bracket.UpperBound.ToString("N0"))}",
                    Rate = bracket.Rate * 100,
                    TaxableInBracket = Math.Round(taxableInBracket, 2),
                    TaxInBracket = Math.Round(taxInBracket, 2)
                });

                if (taxableIncome <= bracket.UpperBound)
                {
                    // Use SARS formula: base amount + rate * (taxable income - lower bound)
                    if (i == 0)
                        tax = taxableIncome * bracket.Rate;
                    else
                        tax = bracket.BaseAmount + bracket.Rate * (taxableIncome - bracket.LowerBound + 1);
                    break;
                }
            }
        }

        return Math.Round(tax, 2);
    }
}

public class TaxInput
{
    public decimal GrossIncome { get; set; }
    public int Age { get; set; } = 30;
    public decimal RetirementContributions { get; set; }
    public int MedicalAidMembers { get; set; } // 0 = none, 1 = main only, 2+ = with dependants
}

public class TaxResult
{
    public string TaxYear { get; set; } = string.Empty;
    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal RetirementDeduction { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal EffectiveRate { get; set; }
    public decimal Rebates { get; set; }
    public decimal MedicalAidCredits { get; set; }
    public decimal MonthlyPaye { get; set; }
    public decimal MonthlyNetIncome { get; set; }
    public List<BracketDetail> BracketBreakdown { get; set; } = new();
}

public class BracketDetail
{
    public string BracketRange { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal TaxableInBracket { get; set; }
    public decimal TaxInBracket { get; set; }
}
