using StockSummary.Api.Domain;

namespace StockSummary.Api.MarketData;

public interface IMarketDataProvider
{
    /// <summary>Fetches the last month of intraday bars for a symbol.</summary>
    /// <exception cref="SymbolNotFoundException">The provider has no data for the symbol.</exception>
    /// <exception cref="MarketDataRateLimitedException">The provider is throttling requests.</exception>
    /// <exception cref="MarketDataUnavailableException">The provider failed or returned an unusable response.</exception>
    Task<IntradaySeries> GetIntradaySeriesAsync(StockSymbol symbol, CancellationToken cancellationToken);
}
