using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Debts;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DebtsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DebtsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all debts for the user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DebtDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetDebtsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Get a specific debt
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DebtDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDebtByIdQuery(GetUserId(), id));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Add a new debt
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DebtDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateDebtRequest request)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new CreateDebtCommand(
            userId, request.Name, request.Type ?? "Other", request.Lender,
            request.OriginalAmount, request.CurrentBalance,
            request.InterestRate, request.MinimumPayment,
            request.ActualPayment, request.DueDay ?? 1,
            request.StartDate ?? DateTime.UtcNow, request.Notes
        ));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a debt
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDebtRequest request)
    {
        var success = await _mediator.Send(new UpdateDebtCommand(
            GetUserId(), id, request.Name, request.Type, request.Lender,
            request.CurrentBalance, request.InterestRate,
            request.MinimumPayment, request.ActualPayment,
            request.DueDay, request.Status, request.Notes
        ));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Delete a debt
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _mediator.Send(new DeleteDebtCommand(GetUserId(), id));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Get debt summary — total debt, monthly payments, debt-free date, progress
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DebtSummaryDto), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _mediator.Send(new GetDebtSummaryQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Log a payment against a debt
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(DebtPaymentResultDto), 200)]
    public async Task<IActionResult> LogPayment(Guid id, [FromBody] LogPaymentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Payment amount must be positive" });

        var result = await _mediator.Send(new LogDebtPaymentCommand(GetUserId(), id, request.Amount, request.Note));
        return Ok(result);
    }

    /// <summary>
    /// Get payment history for a debt
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [ProducesResponseType(typeof(List<DebtPaymentDto>), 200)]
    public async Task<IActionResult> GetPayments(Guid id)
    {
        var result = await _mediator.Send(new GetDebtPaymentsQuery(GetUserId(), id));
        return Ok(result);
    }

    /// <summary>
    /// Calculate payoff plan — Snowball vs Avalanche vs Current, with optional extra payment
    /// </summary>
    [HttpGet("payoff-plan")]
    [ProducesResponseType(typeof(Domain.Services.StrategyComparisonResult), 200)]
    public async Task<IActionResult> GetPayoffPlan([FromQuery] decimal extraPayment = 0)
    {
        var result = await _mediator.Send(new GetPayoffPlanQuery(GetUserId(), extraPayment));
        return Ok(result);
    }

    /// <summary>
    /// Compare strategies with a specific extra monthly payment
    /// </summary>
    [HttpPost("compare-strategies")]
    [ProducesResponseType(typeof(Domain.Services.StrategyComparisonResult), 200)]
    public async Task<IActionResult> CompareStrategies([FromBody] CompareStrategiesRequest request)
    {
        var result = await _mediator.Send(new GetPayoffPlanQuery(GetUserId(), request.ExtraMonthlyPayment));
        return Ok(result);
    }

    /// <summary>
    /// Get SA-specific debt insights and tips
    /// </summary>
    [HttpGet("insights")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<Domain.Services.DebtInsight>), 200)]
    public async Task<IActionResult> GetInsights()
    {
        var result = await _mediator.Send(new GetDebtInsightsQuery());
        return Ok(result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request Models ─────────────────────────────────────────────

public record CreateDebtRequest(
    string Name,
    string? Type,
    string? Lender,
    decimal OriginalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    decimal MinimumPayment,
    decimal ActualPayment,
    int? DueDay,
    DateTime? StartDate,
    string? Notes
);

public record UpdateDebtRequest(
    string? Name,
    string? Type,
    string? Lender,
    decimal? CurrentBalance,
    decimal? InterestRate,
    decimal? MinimumPayment,
    decimal? ActualPayment,
    int? DueDay,
    string? Status,
    string? Notes
);

public record LogPaymentRequest(decimal Amount, string? Note);
public record CompareStrategiesRequest(decimal ExtraMonthlyPayment);
