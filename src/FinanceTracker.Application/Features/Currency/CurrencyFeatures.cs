using MediatR;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Application.Features.Currency;

// ─── Queries ────────────────────────────────────────────────────

public record GetExchangeRateQuery(string FromCurrency, string ToCurrency) : IRequest<ExchangeRateDto?>;

public record GetAllRatesQuery(string BaseCurrency = "ZAR") : IRequest<Dictionary<string, decimal>>;

public record ConvertCurrencyQuery(string From, string To, decimal Amount) : IRequest<CurrencyConversionResultDto>;

public record GetSupportedCurrenciesQuery() : IRequest<List<string>>;

public record GetConversionHistoryQuery(string UserId, int Limit = 20) : IRequest<List<ConversionHistoryDto>>;

// ─── DTOs ───────────────────────────────────────────────────────

public record ConversionHistoryDto(
    Guid Id,
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal ConvertedAmount,
    decimal ExchangeRate,
    DateTime ConvertedAt
);

// ─── Handlers ───────────────────────────────────────────────────

public class GetExchangeRateHandler : IRequestHandler<GetExchangeRateQuery, ExchangeRateDto?>
{
    private readonly ICurrencyExchangeService _currencyService;
    public GetExchangeRateHandler(ICurrencyExchangeService currencyService) => _currencyService = currencyService;

    public async Task<ExchangeRateDto?> Handle(GetExchangeRateQuery request, CancellationToken ct)
        => await _currencyService.GetExchangeRateAsync(request.FromCurrency, request.ToCurrency);
}

public class ConvertCurrencyHandler : IRequestHandler<ConvertCurrencyQuery, CurrencyConversionResultDto>
{
    private readonly ICurrencyExchangeService _currencyService;
    public ConvertCurrencyHandler(ICurrencyExchangeService currencyService) => _currencyService = currencyService;

    public async Task<CurrencyConversionResultDto> Handle(ConvertCurrencyQuery request, CancellationToken ct)
        => await _currencyService.ConvertAsync(request.From, request.To, request.Amount);
}

public class GetSupportedCurrenciesHandler : IRequestHandler<GetSupportedCurrenciesQuery, List<string>>
{
    private readonly ICurrencyExchangeService _currencyService;
    public GetSupportedCurrenciesHandler(ICurrencyExchangeService currencyService) => _currencyService = currencyService;

    public async Task<List<string>> Handle(GetSupportedCurrenciesQuery request, CancellationToken ct)
        => await _currencyService.GetSupportedCurrenciesAsync();
}
