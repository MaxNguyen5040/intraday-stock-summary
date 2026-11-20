namespace StockSummary.Api.Domain;

public sealed record DailySummary(DateOnly Day, decimal LowAverage, decimal HighAverage, long Volume);
