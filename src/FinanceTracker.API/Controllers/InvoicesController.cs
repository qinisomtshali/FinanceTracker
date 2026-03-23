using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Invoices;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Create a new invoice with line items. Automatically calculates VAT and totals.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        if (request.LineItems == null || request.LineItems.Count == 0)
            return BadRequest(new { message = "At least one line item is required" });

        var userId = GetUserId();

        var command = new CreateInvoiceCommand(
            UserId: userId,
            InvoiceNumber: request.InvoiceNumber,
            FromName: request.FromName,
            FromEmail: request.FromEmail,
            FromAddress: request.FromAddress,
            FromVatNumber: request.FromVatNumber,
            ToName: request.ToName,
            ToEmail: request.ToEmail,
            ToAddress: request.ToAddress,
            ToVatNumber: request.ToVatNumber,
            Currency: request.Currency ?? "ZAR",
            VatRate: request.VatRate ?? 15m,
            DiscountPercentage: request.DiscountPercentage,
            IssueDate: request.IssueDate,
            DueDate: request.DueDate,
            BankName: request.BankName,
            AccountHolder: request.AccountHolder,
            AccountNumber: request.AccountNumber,
            BranchCode: request.BranchCode,
            Reference: request.Reference,
            Notes: request.Notes,
            LineItems: request.LineItems.Select((li, i) => new CreateLineItemDto(
                li.Description, li.Quantity, li.UnitPrice, li.SortOrder ?? i
            )).ToList()
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get an invoice by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetInvoiceByIdQuery(userId, id));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// List invoices with optional status filter and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedInvoiceResultDto), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetInvoicesQuery(userId, status, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Get invoice summary (counts by status, totals outstanding/paid)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(InvoiceSummaryDto), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetInvoiceSummaryQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Update invoice status (Draft → Sent → Paid / Overdue / Cancelled)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var validStatuses = new[] { "Draft", "Sent", "Paid", "Overdue", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

        var userId = GetUserId();
        var success = await _mediator.Send(new UpdateInvoiceStatusCommand(userId, id, request.Status, request.PaidDate));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Delete an invoice (only Draft invoices can be deleted)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var success = await _mediator.Send(new DeleteInvoiceCommand(userId, id));
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Duplicate an existing invoice (creates a new Draft copy)
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(InvoiceDto), 201)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new DuplicateInvoiceCommand(userId, id));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request Models ─────────────────────────────────────────────

public record CreateInvoiceRequest(
    string? InvoiceNumber,
    // Sender
    string FromName,
    string? FromEmail,
    string? FromAddress,
    string? FromVatNumber,
    // Recipient
    string ToName,
    string? ToEmail,
    string? ToAddress,
    string? ToVatNumber,
    // Financials
    string? Currency,
    decimal? VatRate,
    decimal? DiscountPercentage,
    // Dates
    DateTime? IssueDate,
    DateTime DueDate,
    // Banking
    string? BankName,
    string? AccountHolder,
    string? AccountNumber,
    string? BranchCode,
    string? Reference,
    string? Notes,
    // Line Items
    List<LineItemRequest> LineItems
);

public record LineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    int? SortOrder = null
);

public record UpdateStatusRequest(
    string Status,
    DateTime? PaidDate = null
);
