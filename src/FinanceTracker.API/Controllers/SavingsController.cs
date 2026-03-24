using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Savings;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SavingsController(IMediator mediator) => _mediator = mediator;

    // ─── Goals ──────────────────────────────────────────────────

    /// <summary>
    /// Get all savings goals for the user
    /// </summary>
    [HttpGet("goals")]
    [ProducesResponseType(typeof(List<SavingsGoalDto>), 200)]
    public async Task<IActionResult> GetGoals()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetSavingsGoalsQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Get a specific savings goal
    /// </summary>
    [HttpGet("goals/{goalId:guid}")]
    [ProducesResponseType(typeof(SavingsGoalDto), 200)]
    public async Task<IActionResult> GetGoal(Guid goalId)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetSavingsGoalByIdQuery(userId, goalId));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Create a new savings goal
    /// </summary>
    [HttpPost("goals")]
    [ProducesResponseType(typeof(SavingsGoalDto), 201)]
    public async Task<IActionResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new CreateSavingsGoalCommand(
            userId, request.Name, request.Icon, request.Color,
            request.TargetAmount, request.MonthlyContribution,
            request.Priority ?? "Medium", request.TargetDate
        ));
        return CreatedAtAction(nameof(GetGoal), new { goalId = result.Id }, result);
    }

    /// <summary>
    /// Update a savings goal
    /// </summary>
    [HttpPatch("goals/{goalId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateGoal(Guid goalId, [FromBody] UpdateGoalRequest request)
    {
        var userId = GetUserId();
        var success = await _mediator.Send(new UpdateSavingsGoalCommand(
            userId, goalId, request.Name, request.TargetAmount,
            request.MonthlyContribution, request.Priority, request.Status, request.TargetDate
        ));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Delete a savings goal
    /// </summary>
    [HttpDelete("goals/{goalId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteGoal(Guid goalId)
    {
        var userId = GetUserId();
        var success = await _mediator.Send(new DeleteSavingsGoalCommand(userId, goalId));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Deposit money into a savings goal
    /// </summary>
    [HttpPost("goals/{goalId:guid}/deposit")]
    [ProducesResponseType(typeof(SavingsDepositResultDto), 200)]
    public async Task<IActionResult> Deposit(Guid goalId, [FromBody] DepositRequest request)
    {
        if (request.Amount <= 0) return BadRequest(new { message = "Amount must be positive" });

        var userId = GetUserId();
        var result = await _mediator.Send(new DepositToGoalCommand(userId, goalId, request.Amount, request.Note));
        return Ok(result);
    }

    /// <summary>
    /// Get deposit history for a savings goal
    /// </summary>
    [HttpGet("goals/{goalId:guid}/deposits")]
    [ProducesResponseType(typeof(List<SavingsDepositDto>), 200)]
    public async Task<IActionResult> GetDeposits(Guid goalId)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetSavingsDepositsQuery(userId, goalId));
        return Ok(result);
    }

    // ─── Summary ────────────────────────────────────────────────

    /// <summary>
    /// Get savings overview — total saved, progress, active goals
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SavingsSummaryDto), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetSavingsSummaryQuery(userId));
        return Ok(result);
    }

    // ─── Challenges ─────────────────────────────────────────────

    /// <summary>
    /// Get active savings challenges
    /// </summary>
    [HttpGet("challenges")]
    [ProducesResponseType(typeof(List<SavingsChallengeDto>), 200)]
    public async Task<IActionResult> GetChallenges()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetSavingsChallengesQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Start a new savings challenge (30-day, 52-week, no-spend)
    /// </summary>
    [HttpPost("challenges")]
    [ProducesResponseType(typeof(SavingsChallengeDto), 201)]
    public async Task<IActionResult> StartChallenge([FromBody] StartChallengeRequest request)
    {
        var validTypes = new[] { "30-day", "52-week", "no-spend" };
        if (!validTypes.Contains(request.Type))
            return BadRequest(new { message = $"Invalid type. Valid: {string.Join(", ", validTypes)}" });

        var userId = GetUserId();
        var result = await _mediator.Send(new StartChallengeCommand(userId, request.Type));
        return CreatedAtAction(nameof(GetChallenges), null, result);
    }

    /// <summary>
    /// Log progress on a savings challenge
    /// </summary>
    [HttpPost("challenges/{challengeId:guid}/progress")]
    [ProducesResponseType(typeof(SavingsChallengeDto), 200)]
    public async Task<IActionResult> LogProgress(Guid challengeId, [FromBody] LogProgressRequest request)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new LogChallengeProgressCommand(userId, challengeId, request.Amount));
        return Ok(result);
    }

    // ─── Interest Calculator ────────────────────────────────────

    /// <summary>
    /// Calculate savings growth with compound interest
    /// </summary>
    [HttpGet("interest/calculate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Domain.Services.SavingsProjection), 200)]
    public async Task<IActionResult> CalculateInterest(
        [FromQuery] decimal initialAmount = 0,
        [FromQuery] decimal monthlyContribution = 1000,
        [FromQuery] decimal annualRate = 8,
        [FromQuery] int months = 60)
    {
        if (months < 1 || months > 600) return BadRequest(new { message = "Months must be 1-600" });
        var result = await _mediator.Send(new CalculateInterestQuery(initialAmount, monthlyContribution, annualRate, months));
        return Ok(result);
    }

    /// <summary>
    /// Get current SA bank savings account rates
    /// </summary>
    [HttpGet("interest/rates")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
    public async Task<IActionResult> GetBankRates()
    {
        var result = await _mediator.Send(new GetBankRatesQuery());
        return Ok(result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request Models ─────────────────────────────────────────────

public record CreateGoalRequest(
    string Name,
    string? Icon,
    string? Color,
    decimal TargetAmount,
    decimal? MonthlyContribution,
    string? Priority,
    DateTime? TargetDate
);

public record UpdateGoalRequest(
    string? Name,
    decimal? TargetAmount,
    decimal? MonthlyContribution,
    string? Priority,
    string? Status,
    DateTime? TargetDate
);

public record DepositRequest(decimal Amount, string? Note);
public record StartChallengeRequest(string Type);
public record LogProgressRequest(decimal Amount);
