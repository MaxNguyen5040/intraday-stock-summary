using StockSummary.Api.Domain;
using StockSummary.Api.MarketData;

namespace StockSummary.Api.Tests.TestSupport;

internal sealed class FakeMarketDataProvider : IMarketDataProvider
{
    private Func<StockSymbol, IntradaySeries> _respond = symbol => Series(symbol);

    public int CallCount { get; private set; }

    public void Returns(params PriceBar[] bars) => _respond = symbol => Series(symbol, bars);

    public void Throws(Exception exception) => _respond = _ => throw exception;

    public Task<IntradaySeries> GetIntradaySeriesAsync(StockSymbol symbol, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_respond(symbol));
    }

    private static IntradaySeries Series(StockSymbol symbol, params PriceBar[] bars) =>
        new(symbol, TimeZones.NewYork, bars);
}
