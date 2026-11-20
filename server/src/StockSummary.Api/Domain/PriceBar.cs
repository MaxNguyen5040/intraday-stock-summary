namespace StockSummary.Api.Domain;

/// <summary>One complete intraday bar. Bars with missing values are dropped before this point.</summary>
public sealed record PriceBar(DateTimeOffset Timestamp, decimal Low, decimal High, long Volume);
