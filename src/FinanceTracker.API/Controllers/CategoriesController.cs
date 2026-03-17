using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Application.Features.Categories.Commands;
using FinanceTracker.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all categories for the current user.
    /// GET /api/categories
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Create a new category.
    /// POST /api/categories
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var command = new CreateCategoryCommand(dto.Name, dto.Type, dto.Icon);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Update an existing category.
    /// PUT /api/categories/{id}
    /// 
    /// NOTE: We take the ID from the URL route, not from the body.
    /// This follows REST conventions — the URL identifies the resource,
    /// the body contains the new data.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var command = new UpdateCategoryCommand(id, dto.Name, dto.Type, dto.Icon);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Delete a category.
    /// DELETE /api/categories/{id}
    /// 
    /// Returns 204 No Content on success — the standard REST response
    /// for successful deletions. There's no body to return.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { errors = result.Errors });
    }
}