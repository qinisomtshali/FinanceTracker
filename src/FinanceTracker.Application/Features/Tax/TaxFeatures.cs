using MediatR;
using FinanceTracker.Domain.Services;

namespace FinanceTracker.Application.Features.Tax;

// ─── Queries ────────────────────────────────────────────────────

public record CalculateTaxQuery(
    decimal GrossIncome,
    int Age = 30,
    decimal RetirementContributions = 0,
    int MedicalAidMembers = 0
) : IRequest<TaxCalculationResultDto>;

public record GetTaxHistoryQuery(string UserId, int Limit = 10) : IRequest<List<TaxHistoryDto>>;

// ─── Commands ───────────────────────────────────────────────────

public record SaveTaxCalculationCommand(
    string UserId,
    decimal GrossIncome,
    int Age,
    decimal RetirementContributions,
    int MedicalAidMembers,
    TaxResult Result
) : IRequest<Guid>;

// ─── DTOs ───────────────────────────────────────────────────────

public record TaxCalculationResultDto(
    string TaxYear,
    decimal GrossIncome,
    decimal TaxableIncome,
    decimal RetirementDeduction,
    decimal TaxAmount,
    decimal EffectiveRate,
    decimal Rebates,
    decimal MedicalAidCredits,
    decimal MonthlyPaye,
    decimal MonthlyNetIncome,
    List<BracketDetailDto> BracketBreakdown
);

public record BracketDetailDto(
    string BracketRange,
    decimal Rate,
    decimal TaxableInBracket,
    decimal TaxInBracket
);

public record TaxHistoryDto(
    Guid Id,
    string TaxYear,
    decimal GrossIncome,
    decimal TaxableIncome,
    decimal TaxAmount,
    decimal EffectiveRate,
    DateTime CalculatedAt
);

// ─── Handlers ───────────────────────────────────────────────────

public class CalculateTaxHandler : IRequestHandler<CalculateTaxQuery, TaxCalculationResultDto>
{
    public Task<TaxCalculationResultDto> Handle(CalculateTaxQuery request, CancellationToken ct)
    {
        var input = new TaxInput
        {
            GrossIncome = request.GrossIncome,
            Age = request.Age,
            RetirementContributions = request.RetirementContributions,
            MedicalAidMembers = request.MedicalAidMembers
        };

        var result = SouthAfricanTaxCalculator.Calculate(input);

        var dto = new TaxCalculationResultDto(
            TaxYear: result.TaxYear,
            GrossIncome: result.GrossIncome,
            TaxableIncome: result.TaxableIncome,
            RetirementDeduction: result.RetirementDeduction,
            TaxAmount: result.TaxAmount,
            EffectiveRate: result.EffectiveRate,
            Rebates: result.Rebates,
            MedicalAidCredits: result.MedicalAidCredits,
            MonthlyPaye: result.MonthlyPaye,
            MonthlyNetIncome: result.MonthlyNetIncome,
            BracketBreakdown: result.BracketBreakdown.Select(b => new BracketDetailDto(
                b.BracketRange, b.Rate, b.TaxableInBracket, b.TaxInBracket
            )).ToList()
        );

        return Task.FromResult(dto);
    }
}
