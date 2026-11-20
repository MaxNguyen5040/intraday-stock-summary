using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StockSummary.Api.Domain;
using StockSummary.Api.MarketData;

namespace StockSummary.Api.Stocks;

public interface IStockSummaryService
{
    Task<IReadOnlyList<DailySummary>> GetDailySummariesAsync(StockSymbol symbol, CancellationToken cancellationToken);
}

public sealed class StockSummaryService(
    IMarketDataProvider marketData,
    IMemoryCache cache,
    IOptions<StockSummaryOptions> options) : IStockSummaryService
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(options.Value.CacheSeconds);

    public async Task<IReadOnlyList<DailySummary>> GetDailySummariesAsync(
        StockSymbol symbol,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var cacheKey = $"daily-summary:{symbol.Value}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<DailySummary>? cached) && cached is not null)
        {
            return cached;
        }

        var series = await marketData.GetIntradaySeriesAsync(symbol, cancellationToken);
        var summaries = DailySummaryAggregator.Aggregate(series.Bars, series.ExchangeTimeZone);

        if (_cacheDuration > TimeSpan.Zero)
        {
            cache.Set(cacheKey, summaries, _cacheDuration);
        }

        return summaries;
    }
}
