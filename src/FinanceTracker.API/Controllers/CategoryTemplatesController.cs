using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Categories;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoryTemplatesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all available category templates, grouped by type (Essentials, Lifestyle, etc.)
    /// </summary>
    [HttpGet("templates")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryTemplateGroupDto>), 200)]
    public async Task<IActionResult> GetTemplates()
    {
        var result = await _mediator.Send(new GetCategoryTemplatesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get preset packs — predefined category sets for quick setup
    /// </summary>
    [HttpGet("templates/packs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetPacks()
    {
        return Ok(new
        {
            starter = new { name = "Starter Pack", description = "9 essential categories to get started", categories = CategoryTemplates.StarterPack },
            complete = new { name = "Complete Pack", description = "All 45 categories — full coverage", categories = CategoryTemplates.CompletePack },
            freelancer = new { name = "Freelancer Pack", description = "15 categories for self-employed & side hustlers", categories = CategoryTemplates.FreelancerPack },
            student = new { name = "Student Pack", description = "14 categories for university students", categories = CategoryTemplates.StudentPack }
        });
    }

    /// <summary>
    /// Apply selected category templates to user's account.
    /// Skips any categories the user already has (by name).
    /// </summary>
    [HttpPost("templates/apply")]
    [Authorize]
    [ProducesResponseType(typeof(ApplyTemplatesResultDto), 200)]
    public async Task<IActionResult> ApplyTemplates([FromBody] ApplyTemplatesRequest request)
    {
        if (request.Categories == null || request.Categories.Count == 0)
            return BadRequest(new { message = "Select at least one category" });

        var userId = GetUserId();
        var result = await _mediator.Send(new ApplyCategoryTemplatesCommand(userId, request.Categories));
        return Ok(result);
    }

    /// <summary>
    /// Apply a preset pack (starter, complete, freelancer, student)
    /// </summary>
    [HttpPost("templates/apply-pack/{packName}")]
    [Authorize]
    [ProducesResponseType(typeof(ApplyTemplatesResultDto), 200)]
    public async Task<IActionResult> ApplyPack(string packName)
    {
        var categories = packName.ToLower() switch
        {
            "starter" => CategoryTemplates.StarterPack,
            "complete" => CategoryTemplates.CompletePack,
            "freelancer" => CategoryTemplates.FreelancerPack,
            "student" => CategoryTemplates.StudentPack,
            _ => null
        };

        if (categories == null)
            return BadRequest(new { message = "Invalid pack. Options: starter, complete, freelancer, student" });

        var userId = GetUserId();
        var result = await _mediator.Send(new ApplyCategoryTemplatesCommand(userId, categories));
        return Ok(result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public record ApplyTemplatesRequest(List<string> Categories);
