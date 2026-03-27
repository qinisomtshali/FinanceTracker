using FinanceTracker.Application.Features.Recurring;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecurringController : ControllerBase
{
    private readonly IMediator _mediator;
    public RecurringController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all recurring transactions (debit orders, salary, subscriptions)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RecurringTransactionDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetRecurringTransactionsQuery(GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Create a recurring transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RecurringTransactionDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateRecurringRequest request)
    {
        var validFrequencies = new[] { "Monthly", "Weekly", "BiWeekly", "Quarterly", "Yearly" };
        if (!validFrequencies.Contains(request.Frequency ?? "Monthly"))
            return BadRequest(new { message = $"Invalid frequency. Valid: {string.Join(", ", validFrequencies)}" });

        var result = await _mediator.Send(new CreateRecurringTransactionCommand(
            GetUserId(), request.Name, request.Amount, request.Description,
            Enum.Parse<TransactionType>(request.Type),
            request.CategoryId, request.Frequency ?? "Monthly",
            request.DayOfMonth ?? 1, request.DayOfWeek,
            request.StartDate ?? DateTime.UtcNow, request.EndDate,
            request.AutoGenerate ?? true,
            request.NotifyBeforeDue ?? true,
            request.NotifyDaysBefore ?? 2
        ));
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    /// <summary>
    /// Update a recurring transaction
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecurringRequest request)
    {
        var success = await _mediator.Send(new UpdateRecurringTransactionCommand(
            GetUserId(), id, request.Name, request.Amount, request.Description,
            request.CategoryId, request.Frequency, request.DayOfMonth,
            request.IsActive, request.AutoGenerate,
            request.NotifyBeforeDue, request.NotifyDaysBefore
        ));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Delete a recurring transaction
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _mediator.Send(new DeleteRecurringTransactionCommand(GetUserId(), id));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Toggle active/inactive
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var success = await _mediator.Send(new ToggleRecurringTransactionCommand(GetUserId(), id));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Get upcoming bills (next X days, default 7)
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<UpcomingBillDto>), 200)]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7)
    {
        var result = await _mediator.Send(new GetUpcomingBillsQuery(GetUserId(), Math.Min(days, 90)));
        return Ok(result);
    }

    /// <summary>
    /// Get bill calendar for a specific month
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(BillCalendarDto), 200)]
    public async Task<IActionResult> GetCalendar([FromQuery] int? month, [FromQuery] int? year)
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(new GetBillCalendarQuery(
            GetUserId(), month ?? now.Month, year ?? now.Year));
        return Ok(result);
    }

    /// <summary>
    /// Get payday plan — see where your salary goes
    /// </summary>
    [HttpGet("payday-plan")]
    [ProducesResponseType(typeof(PaydayPlanResult), 200)]
    public async Task<IActionResult> GetPaydayPlan([FromQuery] int salaryDay = 25)
    {
        var result = await _mediator.Send(new GetPaydayPlanQuery(GetUserId(), salaryDay));
        return Ok(result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request Models ─────────────────────────────────────────────

public record CreateRecurringRequest(
    string Name,
    decimal Amount,
    string? Description,
    string Type, // "Income" or "Expense"
    Guid CategoryId,
    string? Frequency,
    int? DayOfMonth,
    int? DayOfWeek,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? AutoGenerate,
    bool? NotifyBeforeDue,
    int? NotifyDaysBefore
);

public record UpdateRecurringRequest(
    string? Name,
    decimal? Amount,
    string? Description,
    Guid? CategoryId,
    string? Frequency,
    int? DayOfMonth,
    bool? IsActive,
    bool? AutoGenerate,
    bool? NotifyBeforeDue,
    int? NotifyDaysBefore
);
