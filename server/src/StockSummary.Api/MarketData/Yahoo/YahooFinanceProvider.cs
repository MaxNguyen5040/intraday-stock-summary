using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using StockSummary.Api.Domain;

namespace StockSummary.Api.MarketData.Yahoo;

public sealed partial class YahooFinanceProvider(
    HttpClient httpClient,
    IOptions<YahooFinanceOptions> options,
    ILogger<YahooFinanceProvider> logger) : IMarketDataProvider
{
    private const string NotFoundErrorCode = "Not Found";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly YahooFinanceOptions _options = options.Value;

    public async Task<IntradaySeries> GetIntradaySeriesAsync(
        StockSymbol symbol,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        using var response = await SendAsync(BuildRequestUri(symbol), cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new MarketDataRateLimitedException(response.Headers.RetryAfter?.Delta);
        }

        var envelope = await ReadEnvelopeAsync(response, cancellationToken);
        var chart = envelope?.Chart;

        if (response.StatusCode == HttpStatusCode.NotFound || chart?.Error?.Code == NotFoundErrorCode)
        {
            throw new SymbolNotFoundException(symbol);
        }

        if (!response.IsSuccessStatusCode || chart?.Error is not null)
        {
            LogUpstreamError(logger, symbol.Value, (int)response.StatusCode, chart?.Error?.Description);
            throw new MarketDataUnavailableException(
                $"Market data provider returned {(int)response.StatusCode}.");
        }

        if (chart?.Result is not [var result, ..])
        {
            throw new MarketDataUnavailableException("Market data provider returned no chart result.");
        }

        return new IntradaySeries(symbol, ResolveTimeZone(result.Meta, symbol), ToPriceBars(result));
    }

    private string BuildRequestUri(StockSymbol symbol) =>
        $"v8/finance/chart/{Uri.EscapeDataString(symbol.Value)}" +
        $"?range={Uri.EscapeDataString(_options.Range)}&interval={Uri.EscapeDataString(_options.Interval)}";

    private async Task<HttpResponseMessage> SendAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw new MarketDataUnavailableException("Market data provider could not be reached.", ex);
        }
    }

    // Caller cancellation (client disconnected) should propagate as-is; everything else here
    // is the upstream being unreachable, timing out, or the circuit breaker being open.
    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or ExecutionRejectedException
        || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private static async Task<YahooChartEnvelope?> ReadEnvelopeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<YahooChartEnvelope>(SerializerOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new MarketDataUnavailableException("Market data provider returned malformed JSON.", ex);
        }
    }

    private static List<PriceBar> ToPriceBars(YahooChartResult result)
    {
        var timestamps = result.Timestamp;
        var quote = result.Indicators?.Quote is [var first, ..] ? first : null;
        if (timestamps is null || quote?.Low is null || quote.High is null || quote.Volume is null)
        {
            // Valid symbol with no trades in the range.
            return [];
        }

        var count = new[] { timestamps.Count, quote.Low.Count, quote.High.Count, quote.Volume.Count }.Min();
        var bars = new List<PriceBar>(count);
        for (var i = 0; i < count; i++)
        {
            if (quote.Low[i] is { } low && quote.High[i] is { } high && quote.Volume[i] is { } volume)
            {
                bars.Add(new PriceBar(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]), low, high, volume));
            }
        }

        return bars;
    }

    private TimeZoneInfo ResolveTimeZone(YahooChartMeta? meta, StockSymbol symbol)
    {
        var name = meta?.ExchangeTimezoneName;
        if (!string.IsNullOrEmpty(name) && TimeZoneInfo.TryFindSystemTimeZoneById(name, out var timeZone))
        {
            return timeZone;
        }

        // gmtoffset is the offset right now, so it ignores DST. Close enough as a fallback.
        var offset = TimeSpan.FromSeconds(meta?.Gmtoffset ?? 0);
        LogTimeZoneFallback(logger, symbol.Value, name, offset);
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var id = string.Create(CultureInfo.InvariantCulture, $"UTC{sign}{offset.Duration():hh\\:mm}");
        return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Yahoo returned {StatusCode} for {Symbol}: {Description}")]
    private static partial void LogUpstreamError(ILogger logger, string symbol, int statusCode, string? description);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Unknown exchange time zone '{TimeZone}' for {Symbol}; using fixed offset {Offset}")]
    private static partial void LogTimeZoneFallback(ILogger logger, string symbol, string? timeZone, TimeSpan offset);
}
