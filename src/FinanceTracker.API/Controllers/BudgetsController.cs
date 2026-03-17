using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Application.Features.Budgets.Commands;
using FinanceTracker.Application.Features.Budgets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BudgetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var result = await _mediator.Send(new GetBudgetsQuery(month, year));

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        var command = new CreateBudgetCommand(dto.Amount, dto.Month, dto.Year, dto.CategoryId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        var command = new UpdateBudgetCommand(id, dto.Amount, dto.Month, dto.Year, dto.CategoryId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteBudgetCommand(id));

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { errors = result.Errors });
    }
}