using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using StockSummary.Api.Domain;
using StockSummary.Api.MarketData;
using StockSummary.Api.MarketData.Yahoo;
using StockSummary.Api.Tests.TestSupport;
using static StockSummary.Api.Tests.TestSupport.TestData;

namespace StockSummary.Api.Tests.MarketData;

public class YahooFinanceProviderTests
{
    private static readonly StockSymbol Tsla = Symbol("TSLA");

    [Fact]
    public async Task Requests_one_month_of_15_minute_bars_for_the_escaped_symbol()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, Fixture("yahoo-tsla-1mo-15m.json"));

        await CreateProvider(handler).GetIntradaySeriesAsync(Symbol("^GSPC"), CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://query1.finance.yahoo.com/v8/finance/chart/%5EGSPC?range=1mo&interval=15m",
            request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Parses_recorded_response_into_bars_and_exchange_time_zone()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, Fixture("yahoo-tsla-1mo-15m.json"));

        var series = await CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None);

        Assert.Equal(599, series.Bars.Count);
        Assert.Equal("America/New_York", series.ExchangeTimeZone.Id);
        Assert.Equal(363.6400146484375m, series.Bars[0].Low);
    }

    [Fact]
    public async Task Recorded_response_aggregates_to_independently_computed_values()
    {
        // Expected values computed separately in Python (Decimal + zoneinfo) from the same fixture.
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.OK, Fixture("yahoo-tsla-1mo-15m.json"));
        var series = await CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None);

        var days = DailySummaryAggregator.Aggregate(series.Bars, series.ExchangeTimeZone);

        Assert.Equal(23, days.Count);
        Assert.Equal(new DailySummary(new DateOnly(2026, 10, 20), 373.0985m, 374.9545m, 27467645), days[0]);
        Assert.Equal(new DailySummary(new DateOnly(2026, 11, 19), 377.6206m, 380.7997m, 96565515), days[^1]);
    }

    [Fact]
    public async Task Skips_bars_with_null_values()
    {
        const string body = """
            {"chart":{"result":[{"meta":{"exchangeTimezoneName":"America/New_York","gmtoffset":-18000},
            "timestamp":[1763389800,1763390700,1763391600],
            "indicators":{"quote":[{"low":[10.5,null,11.5],"high":[12.0,13.0,null],"volume":[100,200,300]}]}}],
            "error":null}}
            """;

        var series = await CreateProvider(StubHttpMessageHandler.Json(HttpStatusCode.OK, body))
            .GetIntradaySeriesAsync(Tsla, CancellationToken.None);

        var bar = Assert.Single(series.Bars);
        Assert.Equal(10.5m, bar.Low);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1763389800), bar.Timestamp);
    }

    [Fact]
    public async Task Returns_no_bars_when_symbol_has_no_trades_in_range()
    {
        const string body = """
            {"chart":{"result":[{"meta":{"exchangeTimezoneName":"America/New_York","gmtoffset":-18000},
            "indicators":{"quote":[{}]}}],"error":null}}
            """;

        var series = await CreateProvider(StubHttpMessageHandler.Json(HttpStatusCode.OK, body))
            .GetIntradaySeriesAsync(Tsla, CancellationToken.None);

        Assert.Empty(series.Bars);
    }

    [Fact]
    public async Task Falls_back_to_gmt_offset_when_time_zone_is_unknown()
    {
        const string body = """
            {"chart":{"result":[{"meta":{"exchangeTimezoneName":"Not/AZone","gmtoffset":19800},
            "timestamp":[],"indicators":{"quote":[{"low":[],"high":[],"volume":[]}]}}],"error":null}}
            """;

        var series = await CreateProvider(StubHttpMessageHandler.Json(HttpStatusCode.OK, body))
            .GetIntradaySeriesAsync(Tsla, CancellationToken.None);

        Assert.Equal(TimeSpan.FromHours(5.5), series.ExchangeTimeZone.BaseUtcOffset);
    }

    [Fact]
    public async Task Throws_symbol_not_found_for_yahoo_404()
    {
        var handler = StubHttpMessageHandler.Json(HttpStatusCode.NotFound, Fixture("yahoo-not-found.json"));

        var ex = await Assert.ThrowsAsync<SymbolNotFoundException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None));

        Assert.Equal(Tsla, ex.Symbol);
    }

    [Fact]
    public async Task Throws_rate_limited_with_retry_after_for_yahoo_429()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });

        var ex = await Assert.ThrowsAsync<MarketDataRateLimitedException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None));

        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, """{"chart":{"result":null,"error":{"code":"Internal","description":"boom"}}}""")]
    [InlineData(HttpStatusCode.OK, "<html>not json</html>")]
    [InlineData(HttpStatusCode.OK, """{"chart":{"result":[],"error":null}}""")]
    [InlineData(HttpStatusCode.OK, """{"chart":{"result":null,"error":{"code":"Bad Request","description":"Invalid input"}}}""")]
    public async Task Throws_unavailable_for_unusable_responses(HttpStatusCode status, string body)
    {
        var handler = StubHttpMessageHandler.Json(status, body);

        await Assert.ThrowsAsync<MarketDataUnavailableException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_unavailable_when_upstream_cannot_be_reached()
    {
        var handler = StubHttpMessageHandler.Throws(new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<MarketDataUnavailableException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_unavailable_when_resilience_pipeline_times_out()
    {
        var handler = StubHttpMessageHandler.Throws(new TimeoutRejectedException());

        await Assert.ThrowsAsync<MarketDataUnavailableException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, CancellationToken.None));
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = StubHttpMessageHandler.Throws(new TaskCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateProvider(handler).GetIntradaySeriesAsync(Tsla, cts.Token));
    }

    private static YahooFinanceProvider CreateProvider(HttpMessageHandler handler)
    {
        var options = new YahooFinanceOptions();
        var client = new HttpClient(handler) { BaseAddress = options.BaseUrl };
        return new YahooFinanceProvider(client, Options.Create(options), NullLogger<YahooFinanceProvider>.Instance);
    }
}
