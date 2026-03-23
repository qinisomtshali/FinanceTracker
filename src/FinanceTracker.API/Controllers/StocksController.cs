using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Stocks;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StocksController : ControllerBase
{
    private readonly IMediator _mediator;
    public StocksController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get real-time stock quote by symbol (e.g., AAPL, MSFT, SOL.JO)
    /// </summary>
    [HttpGet("quote/{symbol}")]
    [ProducesResponseType(typeof(Application.Interfaces.StockQuoteDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetQuote(string symbol)
    {
        var result = await _mediator.Send(new GetStockQuoteQuery(symbol.ToUpper()));
        return result is null ? NotFound(new { message = $"No quote found for symbol '{symbol}'" }) : Ok(result);
    }

    /// <summary>
    /// Search for stocks by keyword
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Application.Interfaces.StockSearchResultDto>), 200)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Search query is required" });

        var results = await _mediator.Send(new SearchStocksQuery(query));
        return Ok(results);
    }

    /// <summary>
    /// Get daily price history for a stock
    /// </summary>
    [HttpGet("history/{symbol}")]
    [ProducesResponseType(typeof(Application.Interfaces.StockHistoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHistory(string symbol, [FromQuery] int days = 30)
    {
        if (days < 1 || days > 365)
            return BadRequest(new { message = "Days must be between 1 and 365" });

        var result = await _mediator.Send(new GetStockHistoryQuery(symbol.ToUpper(), days));
        return result is null ? NotFound(new { message = $"No history found for symbol '{symbol}'" }) : Ok(result);
    }

    /// <summary>
    /// Get user's stock watchlist
    /// </summary>
    [HttpGet("watchlist")]
    [ProducesResponseType(typeof(List<WatchlistItemDto>), 200)]
    public async Task<IActionResult> GetWatchlist()
    {
        var userId = GetUserId();
        var results = await _mediator.Send(new GetWatchlistQuery(userId));
        return Ok(results);
    }

    /// <summary>
    /// Add a stock to user's watchlist
    /// </summary>
    [HttpPost("watchlist")]
    [ProducesResponseType(typeof(object), 201)]
    public async Task<IActionResult> AddToWatchlist([FromBody] AddToWatchlistRequest request)
    {
        var userId = GetUserId();
        var id = await _mediator.Send(new AddToWatchlistCommand(
            userId, request.Symbol, request.Exchange, request.Name,
            request.AlertPriceAbove, request.AlertPriceBelow, request.Notes
        ));
        return CreatedAtAction(nameof(GetWatchlist), new { id }, new { id });
    }

    /// <summary>
    /// Remove a stock from user's watchlist
    /// </summary>
    [HttpDelete("watchlist/{itemId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveFromWatchlist(Guid itemId)
    {
        var userId = GetUserId();
        var success = await _mediator.Send(new RemoveFromWatchlistCommand(userId, itemId));
        return success ? NoContent() : NotFound();
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

// ─── Request Models ─────────────────────────────────────────────

public record AddToWatchlistRequest(
    string Symbol,
    string Exchange,
    string Name,
    decimal? AlertPriceAbove = null,
    decimal? AlertPriceBelow = null,
    string? Notes = null
);
