using System.ComponentModel.DataAnnotations;

namespace StockSummary.Api.MarketData.Yahoo;

public sealed class YahooFinanceOptions
{
    public const string SectionName = "MarketData:Yahoo";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://query1.finance.yahoo.com/");

    /// <summary>
    /// Lookback window. Without it Yahoo returns only the current session, so "last month"
    /// depends on this being set.
    /// </summary>
    [Required]
    public string Range { get; set; } = "1mo";

    [Required]
    public string Interval { get; set; } = "15m";

    /// <summary>Yahoo rejects or throttles requests without a browser-like User-Agent.</summary>
    [Required]
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

    [Range(1, 60)]
    public int AttemptTimeoutSeconds { get; set; } = 5;

    [Range(0, 5)]
    public int MaxRetryAttempts { get; set; } = 2;
}
