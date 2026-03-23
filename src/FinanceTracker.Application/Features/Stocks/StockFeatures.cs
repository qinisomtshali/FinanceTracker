using MediatR;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.Application.Features.Stocks;

// ─── Queries ────────────────────────────────────────────────────

public record GetStockQuoteQuery(string Symbol) : IRequest<StockQuoteDto?>;

public record SearchStocksQuery(string Query) : IRequest<List<StockSearchResultDto>>;

public record GetStockHistoryQuery(string Symbol, int Days = 30) : IRequest<StockHistoryDto?>;

public record GetWatchlistQuery(string UserId) : IRequest<List<WatchlistItemDto>>;

// ─── Commands ───────────────────────────────────────────────────

public record AddToWatchlistCommand(
    string UserId,
    string Symbol,
    string Exchange,
    string Name,
    decimal? AlertPriceAbove,
    decimal? AlertPriceBelow,
    string? Notes
) : IRequest<Guid>;

public record RemoveFromWatchlistCommand(string UserId, Guid ItemId) : IRequest<bool>;

// ─── DTOs ───────────────────────────────────────────────────────

public record WatchlistItemDto(
    Guid Id,
    string Symbol,
    string Exchange,
    string Name,
    decimal? AlertPriceAbove,
    decimal? AlertPriceBelow,
    string? Notes,
    DateTime CreatedAt
);

// ─── Handlers ───────────────────────────────────────────────────

public class GetStockQuoteHandler : IRequestHandler<GetStockQuoteQuery, StockQuoteDto?>
{
    private readonly IStockMarketService _stockService;
    public GetStockQuoteHandler(IStockMarketService stockService) => _stockService = stockService;

    public async Task<StockQuoteDto?> Handle(GetStockQuoteQuery request, CancellationToken ct)
        => await _stockService.GetQuoteAsync(request.Symbol);
}

public class SearchStocksHandler : IRequestHandler<SearchStocksQuery, List<StockSearchResultDto>>
{
    private readonly IStockMarketService _stockService;
    public SearchStocksHandler(IStockMarketService stockService) => _stockService = stockService;

    public async Task<List<StockSearchResultDto>> Handle(SearchStocksQuery request, CancellationToken ct)
        => await _stockService.SearchAsync(request.Query);
}

public class GetStockHistoryHandler : IRequestHandler<GetStockHistoryQuery, StockHistoryDto?>
{
    private readonly IStockMarketService _stockService;
    public GetStockHistoryHandler(IStockMarketService stockService) => _stockService = stockService;

    public async Task<StockHistoryDto?> Handle(GetStockHistoryQuery request, CancellationToken ct)
        => await _stockService.GetDailyHistoryAsync(request.Symbol, request.Days);
}
