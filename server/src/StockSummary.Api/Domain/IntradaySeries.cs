namespace StockSummary.Api.Domain;

/// <param name="ExchangeTimeZone">Used to decide which trading day a bar belongs to.</param>
public sealed record IntradaySeries(
    StockSymbol Symbol,
    TimeZoneInfo ExchangeTimeZone,
    IReadOnlyList<PriceBar> Bars);
