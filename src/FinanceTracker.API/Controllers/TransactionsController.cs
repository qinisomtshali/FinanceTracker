using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Features.Transactions.Commands;
using FinanceTracker.Application.Features.Transactions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get transactions with optional filtering and pagination.
    /// GET /api/transactions?page=1&pageSize=20&startDate=2026-03-01&categoryId=xxx
    /// 
    /// QUERY PARAMETERS vs ROUTE PARAMETERS:
    ///   Route params ({id}) = identifying a specific resource
    ///   Query params (?page=1) = filtering, sorting, pagination
    /// This follows REST conventions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTransactionsQuery(startDate, endDate, categoryId, page, pageSize);
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Create a new transaction.
    /// POST /api/transactions
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var command = new CreateTransactionCommand(
            dto.Amount, dto.Description, dto.Date, dto.Type, dto.CategoryId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Update a transaction.
    /// PUT /api/transactions/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionDto dto)
    {
        var command = new UpdateTransactionCommand(
            id, dto.Amount, dto.Description, dto.Date, dto.Type, dto.CategoryId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Delete a transaction.
    /// DELETE /api/transactions/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteTransactionCommand(id));

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { errors = result.Errors });
    }
}