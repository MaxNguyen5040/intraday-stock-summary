using System.ComponentModel.DataAnnotations;

namespace StockSummary.Api.Stocks;

public sealed class StockSummaryOptions
{
    public const string SectionName = "StockSummary";

    /// <summary>
    /// 15-minute bars only change every 15 minutes, so a short cache absorbs repeat lookups
    /// without serving noticeably stale data.
    /// </summary>
    [Range(0, 3600)]
    public int CacheSeconds { get; set; } = 60;
}
