namespace StockSummary.Api.Domain;

public static class DailySummaryAggregator
{
    public const int PriceDecimals = 4;

    /// <summary>
    /// Groups bars by trading day in the exchange's local time and averages the lows and highs.
    /// Grouping by UTC would split sessions that cross midnight UTC (e.g. ASX, which opens at
    /// 23:00 UTC), and a fixed UTC offset would be wrong on either side of a DST change.
    /// </summary>
    public static IReadOnlyList<DailySummary> Aggregate(
        IEnumerable<PriceBar> bars,
        TimeZoneInfo exchangeTimeZone)
    {
        ArgumentNullException.ThrowIfNull(bars);
        ArgumentNullException.ThrowIfNull(exchangeTimeZone);

        return bars
            .GroupBy(bar => DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(bar.Timestamp, exchangeTimeZone).DateTime))
            .OrderBy(day => day.Key)
            .Select(day => new DailySummary(
                day.Key,
                RoundPrice(day.Average(bar => bar.Low)),
                RoundPrice(day.Average(bar => bar.High)),
                day.Sum(bar => bar.Volume)))
            .ToList();
    }

    private static decimal RoundPrice(decimal value) =>
        Math.Round(value, PriceDecimals, MidpointRounding.AwayFromZero);
}
