using StockSummary.Api.Domain;

namespace StockSummary.Api.Stocks;

/// <summary>
/// Public API contract, kept separate from the domain record so the domain can change
/// without breaking clients.
/// </summary>
public sealed record DailySummaryResponse(DateOnly Day, decimal LowAverage, decimal HighAverage, long Volume)
{
    public static DailySummaryResponse From(DailySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new(summary.Day, summary.LowAverage, summary.HighAverage, summary.Volume);
    }
}
