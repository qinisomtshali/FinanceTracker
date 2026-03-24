using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Gamification;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get enhanced dashboard data — stats, gamification, tips, and health score
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), 200)]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetDashboardQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Get user's gamification profile — points, level, tier, streak
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetUserProfileQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Get financial health score breakdown (ClearScore-inspired)
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(Domain.Services.FinancialHealthResult), 200)]
    public async Task<IActionResult> GetHealthScore()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetFinancialHealthQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Get leaderboard — top users by points
    /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(List<LeaderboardEntryDto>), 200)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 10)
    {
        var result = await _mediator.Send(new GetLeaderboardQuery(Math.Min(limit, 50)));
        return Ok(result);
    }

    /// <summary>
    /// Get user's achievements — unlocked and locked
    /// </summary>
    [HttpGet("achievements")]
    [ProducesResponseType(typeof(AchievementsDto), 200)]
    public async Task<IActionResult> GetAchievements()
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetAchievementsQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Get user's point earning history
    /// </summary>
    [HttpGet("points")]
    [ProducesResponseType(typeof(List<PointTransactionDto>), 200)]
    public async Task<IActionResult> GetPointHistory([FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetPointHistoryQuery(userId, limit));
        return Ok(result);
    }

    /// <summary>
    /// Get daily financial tip
    /// </summary>
    [HttpGet("tip")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TipDto), 200)]
    public async Task<IActionResult> GetDailyTip()
    {
        var result = await _mediator.Send(new GetDailyTipQuery());
        return result is null ? NotFound() : Ok(result);
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}
