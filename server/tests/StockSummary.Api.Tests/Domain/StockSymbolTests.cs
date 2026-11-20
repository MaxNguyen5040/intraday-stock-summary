using StockSummary.Api.Domain;

namespace StockSummary.Api.Tests.Domain;

public class StockSymbolTests
{
    [Theory]
    [InlineData("TSLA", "TSLA")]
    [InlineData(" tsla ", "TSLA")]
    [InlineData("BRK-B", "BRK-B")]
    [InlineData("BF.B", "BF.B")]
    [InlineData("^GSPC", "^GSPC")]
    [InlineData("EURUSD=X", "EURUSD=X")]
    [InlineData("7203.T", "7203.T")]
    public void TryParse_accepts_and_normalizes_valid_symbols(string input, string expected)
    {
        Assert.True(StockSymbol.TryParse(input, out var symbol));
        Assert.Equal(expected, symbol.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TS LA")]
    [InlineData("TSLA/../x")]
    [InlineData("-TSLA")]
    [InlineData("TSLA^")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("<script>")]
    public void TryParse_rejects_invalid_symbols(string? input)
    {
        Assert.False(StockSymbol.TryParse(input, out var symbol));
        Assert.Null(symbol);
    }
}
