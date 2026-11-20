using StockSummary.Api.Domain;

namespace StockSummary.Api.Tests.TestSupport;

internal static class TimeZones
{
    public static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    public static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");
}

internal static class TestData
{
    public static StockSymbol Symbol(string value) =>
        StockSymbol.TryParse(value, out var symbol)
            ? symbol
            : throw new ArgumentException($"Invalid test symbol '{value}'.", nameof(value));

    public static PriceBar Bar(string utcTimestamp, decimal low, decimal high, long volume) =>
        new(DateTimeOffset.Parse(utcTimestamp, System.Globalization.CultureInfo.InvariantCulture), low, high, volume);

    public static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
