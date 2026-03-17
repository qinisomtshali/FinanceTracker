using FinanceTracker.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get monthly income vs expenses summary.
    /// GET /api/reports/monthly-summary?month=3&year=2026
    /// </summary>
    [HttpGet("monthly-summary")]
    public async Task<IActionResult> GetMonthlySummary(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var result = await _mediator.Send(new GetMonthlySummaryQuery(month, year));

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Get spending breakdown per category.
    /// GET /api/reports/category-breakdown?month=3&year=2026
    /// </summary>
    [HttpGet("category-breakdown")]
    public async Task<IActionResult> GetCategoryBreakdown(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var result = await _mediator.Send(new GetCategoryBreakdownQuery(month, year));

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Get budget vs actual spending comparison.
    /// GET /api/reports/budget-vs-actual?month=3&year=2026
    /// </summary>
    [HttpGet("budget-vs-actual")]
    public async Task<IActionResult> GetBudgetVsActual(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var result = await _mediator.Send(new GetBudgetVsActualQuery(month, year));

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }
}