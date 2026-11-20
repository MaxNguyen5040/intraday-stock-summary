namespace StockSummary.Api.Infrastructure;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "client";

    /// <summary>Origins allowed to call the API directly. Empty means same-origin only.</summary>
    public IReadOnlyList<string> AllowedOrigins { get; set; } = [];
}
