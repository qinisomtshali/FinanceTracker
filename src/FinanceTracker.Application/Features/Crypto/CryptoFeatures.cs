using MediatR;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Application.Features.Crypto;

// ─── Queries ────────────────────────────────────────────────────

public record GetCryptoPriceQuery(string CoinId, string VsCurrency = "usd") : IRequest<CryptoQuoteDto?>;

public record GetTopCryptoQuery(int Limit = 20, string VsCurrency = "usd") : IRequest<List<CryptoQuoteDto>>;

public record SearchCryptoQuery(string Query) : IRequest<List<CryptoSearchResultDto>>;

public record GetCryptoChartQuery(string CoinId, string VsCurrency = "usd", int Days = 30) : IRequest<CryptoMarketChartDto?>;

public record GetCryptoWatchlistQuery(string UserId) : IRequest<List<CryptoWatchlistItemDto>>;

// ─── Commands ───────────────────────────────────────────────────

public record AddCryptoToWatchlistCommand(
    string UserId,
    string CoinId,
    string Symbol,
    string Name,
    decimal? HoldingQuantity,
    decimal? AverageBuyPrice,
    string Currency,
    decimal? AlertPriceAbove,
    decimal? AlertPriceBelow,
    string? Notes
) : IRequest<Guid>;

public record RemoveCryptoFromWatchlistCommand(string UserId, Guid ItemId) : IRequest<bool>;

// ─── DTOs ───────────────────────────────────────────────────────

public record CryptoWatchlistItemDto(
    Guid Id,
    string CoinId,
    string Symbol,
    string Name,
    decimal? HoldingQuantity,
    decimal? AverageBuyPrice,
    string Currency,
    decimal? AlertPriceAbove,
    decimal? AlertPriceBelow,
    string? Notes,
    DateTime CreatedAt
);

// ─── Handlers ───────────────────────────────────────────────────

public class GetCryptoPriceHandler : IRequestHandler<GetCryptoPriceQuery, CryptoQuoteDto?>
{
    private readonly ICryptoService _cryptoService;
    public GetCryptoPriceHandler(ICryptoService cryptoService) => _cryptoService = cryptoService;

    public async Task<CryptoQuoteDto?> Handle(GetCryptoPriceQuery request, CancellationToken ct)
        => await _cryptoService.GetPriceAsync(request.CoinId, request.VsCurrency);
}

public class GetTopCryptoHandler : IRequestHandler<GetTopCryptoQuery, List<CryptoQuoteDto>>
{
    private readonly ICryptoService _cryptoService;
    public GetTopCryptoHandler(ICryptoService cryptoService) => _cryptoService = cryptoService;

    public async Task<List<CryptoQuoteDto>> Handle(GetTopCryptoQuery request, CancellationToken ct)
        => await _cryptoService.GetTopCoinsAsync(request.Limit, request.VsCurrency);
}

public class SearchCryptoHandler : IRequestHandler<SearchCryptoQuery, List<CryptoSearchResultDto>>
{
    private readonly ICryptoService _cryptoService;
    public SearchCryptoHandler(ICryptoService cryptoService) => _cryptoService = cryptoService;

    public async Task<List<CryptoSearchResultDto>> Handle(SearchCryptoQuery request, CancellationToken ct)
        => await _cryptoService.SearchAsync(request.Query);
}

public class GetCryptoChartHandler : IRequestHandler<GetCryptoChartQuery, CryptoMarketChartDto?>
{
    private readonly ICryptoService _cryptoService;
    public GetCryptoChartHandler(ICryptoService cryptoService) => _cryptoService = cryptoService;

    public async Task<CryptoMarketChartDto?> Handle(GetCryptoChartQuery request, CancellationToken ct)
        => await _cryptoService.GetMarketChartAsync(request.CoinId, request.VsCurrency, request.Days);
}
