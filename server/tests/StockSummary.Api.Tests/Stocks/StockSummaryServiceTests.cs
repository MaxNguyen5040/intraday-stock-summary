using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StockSummary.Api.MarketData;
using StockSummary.Api.Stocks;
using StockSummary.Api.Tests.TestSupport;
using static StockSummary.Api.Tests.TestSupport.TestData;

namespace StockSummary.Api.Tests.Stocks;

public sealed class StockSummaryServiceTests : IDisposable
{
    private readonly FakeMarketDataProvider _provider = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task Aggregates_provider_bars_into_daily_summaries()
    {
        _provider.Returns(
            Bar("2026-11-17T15:00:00Z", 10m, 12m, 100),
            Bar("2026-11-18T15:00:00Z", 20m, 22m, 200));

        var summaries = await CreateService().GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None);

        Assert.Equal(2, summaries.Count);
        Assert.Equal(200, summaries[1].Volume);
    }

    [Fact]
    public async Task Serves_repeat_requests_for_a_symbol_from_cache()
    {
        var service = CreateService();

        await service.GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None);
        await service.GetDailySummariesAsync(Symbol("tsla"), CancellationToken.None);
        await service.GetDailySummariesAsync(Symbol("AAPL"), CancellationToken.None);

        Assert.Equal(2, _provider.CallCount);
    }

    [Fact]
    public async Task Does_not_cache_when_cache_duration_is_zero()
    {
        var service = CreateService(cacheSeconds: 0);

        await service.GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None);
        await service.GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None);

        Assert.Equal(2, _provider.CallCount);
    }

    [Fact]
    public async Task Does_not_cache_failures()
    {
        var service = CreateService();
        _provider.Throws(new MarketDataUnavailableException("down"));

        await Assert.ThrowsAsync<MarketDataUnavailableException>(
            () => service.GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None));

        _provider.Returns(Bar("2026-11-17T15:00:00Z", 1m, 1m, 1));
        var summaries = await service.GetDailySummariesAsync(Symbol("TSLA"), CancellationToken.None);

        Assert.Single(summaries);
    }

    public void Dispose() => _cache.Dispose();

    private StockSummaryService CreateService(int cacheSeconds = 60) =>
        new(_provider, _cache, Options.Create(new StockSummaryOptions { CacheSeconds = cacheSeconds }));
}
