using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Features.Currency;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly IMediator _mediator;
    public CurrencyController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Convert an amount between two currencies
    /// </summary>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(Application.Interfaces.CurrencyConversionResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Convert(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest(new { message = "Both 'from' and 'to' currency codes are required" });
        if (amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero" });

        try
        {
            var result = await _mediator.Send(new ConvertCurrencyQuery(from, to, amount));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get exchange rate between two currencies
    /// </summary>
    [HttpGet("rate")]
    [ProducesResponseType(typeof(Application.Interfaces.ExchangeRateDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRate([FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest(new { message = "Both 'from' and 'to' currency codes are required" });

        var result = await _mediator.Send(new GetExchangeRateQuery(from, to));
        return result is null
            ? NotFound(new { message = $"Rate not found for {from}/{to}" })
            : Ok(result);
    }

    /// <summary>
    /// Get all exchange rates for a base currency (default: ZAR)
    /// </summary>
    [HttpGet("rates")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), 200)]
    public async Task<IActionResult> GetAllRates([FromQuery] string baseCurrency = "ZAR")
    {
        var results = await _mediator.Send(new GetAllRatesQuery(baseCurrency));
        return Ok(new { baseCurrency = baseCurrency.ToUpper(), rates = results });
    }

    /// <summary>
    /// Get list of supported currency codes
    /// </summary>
    [HttpGet("supported")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetSupportedCurrencies()
    {
        var results = await _mediator.Send(new GetSupportedCurrenciesQuery());
        return Ok(results);
    }

    /// <summary>
    /// Bulk convert: convert one amount to multiple target currencies at once
    /// </summary>
    [HttpPost("bulk-convert")]
    [ProducesResponseType(typeof(BulkConversionResponse), 200)]
    public async Task<IActionResult> BulkConvert([FromBody] BulkConversionRequest request)
    {
        if (request.TargetCurrencies == null || request.TargetCurrencies.Count == 0)
            return BadRequest(new { message = "At least one target currency is required" });
        if (request.TargetCurrencies.Count > 20)
            return BadRequest(new { message = "Maximum 20 target currencies per request" });

        var conversions = new List<Application.Interfaces.CurrencyConversionResultDto>();

        foreach (var target in request.TargetCurrencies)
        {
            try
            {
                var result = await _mediator.Send(
                    new ConvertCurrencyQuery(request.FromCurrency, target, request.Amount));
                conversions.Add(result);
            }
            catch
            {
                // Skip failed conversions
            }
        }

        return Ok(new BulkConversionResponse(
            request.FromCurrency.ToUpper(),
            request.Amount,
            conversions
        ));
    }
}

public record BulkConversionRequest(
    string FromCurrency,
    decimal Amount,
    List<string> TargetCurrencies
);

public record BulkConversionResponse(
    string FromCurrency,
    decimal OriginalAmount,
    List<Application.Interfaces.CurrencyConversionResultDto> Conversions
);
