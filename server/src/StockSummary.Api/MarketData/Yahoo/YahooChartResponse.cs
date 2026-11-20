namespace StockSummary.Api.MarketData.Yahoo;

// Only the fields we read from /v8/finance/chart. Everything is nullable because Yahoo omits
// arrays for symbols with no trades in the range and returns null entries for missing bars.

internal sealed record YahooChartEnvelope(YahooChart? Chart);

internal sealed record YahooChart(IReadOnlyList<YahooChartResult>? Result, YahooChartError? Error);

internal sealed record YahooChartError(string? Code, string? Description);

internal sealed record YahooChartResult(
    YahooChartMeta? Meta,
    IReadOnlyList<long>? Timestamp,
    YahooIndicators? Indicators);

internal sealed record YahooChartMeta(string? ExchangeTimezoneName, int Gmtoffset);

internal sealed record YahooIndicators(IReadOnlyList<YahooQuote>? Quote);

internal sealed record YahooQuote(
    IReadOnlyList<decimal?>? Low,
    IReadOnlyList<decimal?>? High,
    IReadOnlyList<long?>? Volume);
