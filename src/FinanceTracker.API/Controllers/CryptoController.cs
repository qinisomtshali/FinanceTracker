using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Crypto;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CryptoController : ControllerBase
{
    private readonly IMediator _mediator;
    public CryptoController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get crypto price by coin ID (e.g., bitcoin, ethereum, solana)
    /// </summary>
    [HttpGet("price/{coinId}")]
    [ProducesResponseType(typeof(Application.Interfaces.CryptoQuoteDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPrice(string coinId, [FromQuery] string vsCurrency = "usd")
    {
        var result = await _mediator.Send(new GetCryptoPriceQuery(coinId.ToLower(), vsCurrency.ToLower()));
        return result is null ? NotFound(new { message = $"Coin '{coinId}' not found" }) : Ok(result);
    }

    /// <summary>
    /// Get top cryptocurrencies by market cap
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(List<Application.Interfaces.CryptoQuoteDto>), 200)]
    public async Task<IActionResult> GetTopCoins([FromQuery] int limit = 20, [FromQuery] string vsCurrency = "usd")
    {
        if (limit < 1 || limit > 100)
            return BadRequest(new { message = "Limit must be between 1 and 100" });

        var results = await _mediator.Send(new GetTopCryptoQuery(limit, vsCurrency.ToLower()));
        return Ok(results);
    }

    /// <summary>
    /// Search for a cryptocurrency
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Application.Interfaces.CryptoSearchResultDto>), 200)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Search query is required" });

        var results = await _mediator.Send(new SearchCryptoQuery(query));
        return Ok(results);
    }

    /// <summary>
    /// Get market chart data (prices, market cap, volumes over time)
    /// </summary>
    [HttpGet("chart/{coinId}")]
    [ProducesResponseType(typeof(Application.Interfaces.CryptoMarketChartDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetChart(string coinId, [FromQuery] string vsCurrency = "usd", [FromQuery] int days = 30)
    {
        if (days < 1 || days > 365)
            return BadRequest(new { message = "Days must be between 1 and 365" });

        var result = await _mediator.Send(new GetCryptoChartQuery(coinId.ToLower(), vsCurrency.ToLower(), days));
        return result is null ? NotFound(new { message = $"Chart data for '{coinId}' not found" }) : Ok(result);
    }

    /// <summary>
    /// Get user's crypto watchlist
    /// </summary>
    [HttpGet("watchlist")]
    [ProducesResponseType(typeof(List<CryptoWatchlistItemDto>), 200)]
    public async Task<IActionResult> GetWatchlist()
    {
        var userId = GetUserId();
        var results = await _mediator.Send(new GetCryptoWatchlistQuery(userId));
        return Ok(results);
    }

    /// <summary>
    /// Add a crypto to user's watchlist with optional holdings tracking
    /// </summary>
    [HttpPost("watchlist")]
    [ProducesResponseType(typeof(object), 201)]
    public async Task<IActionResult> AddToWatchlist([FromBody] AddCryptoWatchlistRequest request)
    {
        var userId = GetUserId();
        var id = await _mediator.Send(new AddCryptoToWatchlistCommand(
            userId, request.CoinId, request.Symbol, request.Name,
            request.HoldingQuantity, request.AverageBuyPrice, request.Currency ?? "USD",
            request.AlertPriceAbove, request.AlertPriceBelow, request.Notes
        ));
        return CreatedAtAction(nameof(GetWatchlist), new { id }, new { id });
    }

    /// <summary>
    /// Remove a crypto from user's watchlist
    /// </summary>
    [HttpDelete("watchlist/{itemId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveFromWatchlist(Guid itemId)
    {
        var userId = GetUserId();
        var success = await _mediator.Send(new RemoveCryptoFromWatchlistCommand(userId, itemId));
        return success ? NoContent() : NotFound();
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public record AddCryptoWatchlistRequest(
    string CoinId,
    string Symbol,
    string Name,
    decimal? HoldingQuantity = null,
    decimal? AverageBuyPrice = null,
    string? Currency = "USD",
    decimal? AlertPriceAbove = null,
    decimal? AlertPriceBelow = null,
    string? Notes = null
);
