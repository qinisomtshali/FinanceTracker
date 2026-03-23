using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Tax;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly IMediator _mediator;
    public TaxController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Calculate South African income tax for the 2025/2026 tax year.
    /// Includes PAYE, rebates, medical aid credits, and retirement fund deductions.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TaxCalculationResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Calculate([FromBody] TaxCalculateRequest request)
    {
        if (request.GrossIncome <= 0)
            return BadRequest(new { message = "Gross annual income must be greater than zero" });
        if (request.Age < 18 || request.Age > 120)
            return BadRequest(new { message = "Age must be between 18 and 120" });

        var result = await _mediator.Send(new CalculateTaxQuery(
            request.GrossIncome,
            request.Age,
            request.RetirementContributions,
            request.MedicalAidMembers
        ));

        return Ok(result);
    }

    /// <summary>
    /// Quick tax estimate from monthly salary
    /// </summary>
    [HttpGet("estimate")]
    [ProducesResponseType(typeof(TaxCalculationResultDto), 200)]
    public async Task<IActionResult> QuickEstimate(
        [FromQuery] decimal monthlySalary,
        [FromQuery] int age = 30)
    {
        if (monthlySalary <= 0)
            return BadRequest(new { message = "Monthly salary must be greater than zero" });

        var annualIncome = monthlySalary * 12;
        var result = await _mediator.Send(new CalculateTaxQuery(annualIncome, age));
        return Ok(result);
    }

    /// <summary>
    /// Compare tax across different salary scenarios
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(List<TaxComparisonItemDto>), 200)]
    public async Task<IActionResult> Compare([FromBody] TaxCompareRequest request)
    {
        if (request.Scenarios == null || request.Scenarios.Count == 0)
            return BadRequest(new { message = "At least one scenario is required" });
        if (request.Scenarios.Count > 5)
            return BadRequest(new { message = "Maximum 5 scenarios per comparison" });

        var results = new List<TaxComparisonItemDto>();

        foreach (var scenario in request.Scenarios)
        {
            var result = await _mediator.Send(new CalculateTaxQuery(
                scenario.GrossIncome,
                scenario.Age,
                scenario.RetirementContributions,
                scenario.MedicalAidMembers
            ));

            results.Add(new TaxComparisonItemDto(
                scenario.Label ?? $"R{scenario.GrossIncome:N0}",
                result
            ));
        }

        return Ok(results);
    }

    /// <summary>
    /// Get SA tax brackets and thresholds for reference
    /// </summary>
    [HttpGet("brackets")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TaxBracketsInfoDto), 200)]
    public IActionResult GetBrackets()
    {
        var info = new TaxBracketsInfoDto(
            TaxYear: "2025/2026",
            Country: "South Africa",
            Brackets: new List<BracketInfoDto>
            {
                new("R0 – R237,100", 18m),
                new("R237,101 – R370,500", 26m),
                new("R370,501 – R512,800", 31m),
                new("R512,801 – R673,000", 36m),
                new("R673,001 – R857,900", 39m),
                new("R857,901 – R1,817,000", 41m),
                new("R1,817,001+", 45m),
            },
            Rebates: new RebatesInfoDto(
                Primary: 17_235m,
                Secondary: 9_444m,
                Tertiary: 3_145m
            ),
            Thresholds: new ThresholdsInfoDto(
                Below65: 95_750m,
                Age65To74: 148_217m,
                Age75Plus: 165_689m
            ),
            VatRate: 15m,
            RetirementDeductionCap: 350_000m,
            RetirementDeductionPercentage: 27.5m
        );

        return Ok(info);
    }

    /// <summary>
    /// Get user's saved tax calculation history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<TaxHistoryDto>), 200)]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var results = await _mediator.Send(new GetTaxHistoryQuery(userId, limit));
        return Ok(results);
    }

    /// <summary>
    /// Save a tax calculation for future reference
    /// </summary>
    [HttpPost("save")]
    [ProducesResponseType(typeof(object), 201)]
    public async Task<IActionResult> SaveCalculation([FromBody] TaxCalculateRequest request)
    {
        var userId = GetUserId();

        // Calculate first
        var calcResult = await _mediator.Send(new CalculateTaxQuery(
            request.GrossIncome, request.Age,
            request.RetirementContributions, request.MedicalAidMembers
        ));

        // Then save
        var taxResult = new Domain.Services.TaxResult
        {
            TaxYear = calcResult.TaxYear,
            GrossIncome = calcResult.GrossIncome,
            TaxableIncome = calcResult.TaxableIncome,
            TaxAmount = calcResult.TaxAmount,
            EffectiveRate = calcResult.EffectiveRate,
        };

        var id = await _mediator.Send(new SaveTaxCalculationCommand(
            userId, request.GrossIncome, request.Age,
            request.RetirementContributions, request.MedicalAidMembers,
            taxResult
        ));

        return CreatedAtAction(nameof(GetHistory), new { id }, new { id, calculation = calcResult });
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request / Response Models ──────────────────────────────────

public record TaxCalculateRequest(
    decimal GrossIncome,
    int Age = 30,
    decimal RetirementContributions = 0,
    int MedicalAidMembers = 0
);

public record TaxCompareRequest(List<TaxScenarioRequest> Scenarios);

public record TaxScenarioRequest(
    decimal GrossIncome,
    int Age = 30,
    decimal RetirementContributions = 0,
    int MedicalAidMembers = 0,
    string? Label = null
);

public record TaxComparisonItemDto(string Label, TaxCalculationResultDto Result);

public record TaxBracketsInfoDto(
    string TaxYear,
    string Country,
    List<BracketInfoDto> Brackets,
    RebatesInfoDto Rebates,
    ThresholdsInfoDto Thresholds,
    decimal VatRate,
    decimal RetirementDeductionCap,
    decimal RetirementDeductionPercentage
);

public record BracketInfoDto(string Range, decimal Rate);
public record RebatesInfoDto(decimal Primary, decimal Secondary, decimal Tertiary);
public record ThresholdsInfoDto(decimal Below65, decimal Age65To74, decimal Age75Plus);
