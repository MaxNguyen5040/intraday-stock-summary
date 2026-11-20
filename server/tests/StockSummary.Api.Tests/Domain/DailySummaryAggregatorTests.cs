using StockSummary.Api.Domain;
using StockSummary.Api.Tests.TestSupport;
using static StockSummary.Api.Tests.TestSupport.TestData;

namespace StockSummary.Api.Tests.Domain;

public class DailySummaryAggregatorTests
{
    [Fact]
    public void Averages_lows_and_highs_and_sums_volume_per_day()
    {
        PriceBar[] bars =
        [
            Bar("2026-11-17T14:30:00Z", 10m, 12m, 100),
            Bar("2026-11-17T14:45:00Z", 11m, 14m, 250),
            Bar("2026-11-17T15:00:00Z", 12m, 16m, 50),
        ];

        var summary = Assert.Single(DailySummaryAggregator.Aggregate(bars, TimeZones.NewYork));

        Assert.Equal(new DateOnly(2026, 11, 17), summary.Day);
        Assert.Equal(11m, summary.LowAverage);
        Assert.Equal(14m, summary.HighAverage);
        Assert.Equal(400, summary.Volume);
    }

    [Fact]
    public void Rounds_averages_to_four_decimals_with_midpoints_away_from_zero()
    {
        PriceBar[] bars =
        [
            Bar("2026-11-17T14:30:00Z", 1.00004m, 2.00001m, 1),
            Bar("2026-11-17T14:45:00Z", 1.00006m, 2.00002m, 1),
        ];

        var summary = Assert.Single(DailySummaryAggregator.Aggregate(bars, TimeZones.NewYork));

        // Banker's rounding would give 1.0000 here.
        Assert.Equal(1.0001m, summary.LowAverage);
        Assert.Equal(2.0000m, summary.HighAverage);
    }

    [Fact]
    public void Groups_by_exchange_local_date_rather_than_utc()
    {
        // ASX opens at 10:00 Sydney time, which is 23:00 UTC the previous day in November.
        PriceBar[] bars =
        [
            Bar("2026-11-16T23:15:00Z", 10m, 11m, 1),
            Bar("2026-11-17T04:45:00Z", 20m, 21m, 1),
        ];

        var summary = Assert.Single(DailySummaryAggregator.Aggregate(bars, TimeZones.Sydney));

        Assert.Equal(new DateOnly(2026, 11, 17), summary.Day);
        Assert.Equal(15m, summary.LowAverage);
    }

    [Fact]
    public void Applies_the_offset_in_effect_on_each_date_across_a_dst_change()
    {
        // 04:30 UTC is 00:30 EDT on Oct 30 but would be 23:30 on Oct 29 at the post-DST EST offset.
        PriceBar[] bars =
        [
            Bar("2026-10-30T04:30:00Z", 1m, 1m, 1),
            Bar("2026-11-03T04:30:00Z", 2m, 2m, 1),
        ];

        var days = DailySummaryAggregator.Aggregate(bars, TimeZones.NewYork).Select(s => s.Day);

        Assert.Equal([new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 2)], days);
    }

    [Fact]
    public void Returns_days_in_ascending_order()
    {
        PriceBar[] bars =
        [
            Bar("2026-11-18T15:00:00Z", 3m, 3m, 1),
            Bar("2026-11-16T15:00:00Z", 1m, 1m, 1),
            Bar("2026-11-17T15:00:00Z", 2m, 2m, 1),
        ];

        var days = DailySummaryAggregator.Aggregate(bars, TimeZones.NewYork).Select(s => s.Day.Day);

        Assert.Equal([16, 17, 18], days);
    }

    [Fact]
    public void Returns_empty_for_no_bars()
    {
        Assert.Empty(DailySummaryAggregator.Aggregate([], TimeZones.NewYork));
    }
}
