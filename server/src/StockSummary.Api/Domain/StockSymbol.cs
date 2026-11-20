using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace StockSummary.Api.Domain;

/// <summary>
/// A normalized ticker symbol. Covers equities (TSLA), share classes (BRK-B, BF.B),
/// indices (^GSPC) and currency pairs (EURUSD=X).
/// </summary>
public sealed partial record StockSymbol
{
    public const int MaxLength = 15;

    private StockSymbol(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? input, [NotNullWhen(true)] out StockSymbol? symbol)
    {
        symbol = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().ToUpperInvariant();
        if (normalized.Length > MaxLength || !SymbolPattern().IsMatch(normalized))
        {
            return false;
        }

        symbol = new StockSymbol(normalized);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^\^?[A-Z0-9][A-Z0-9.\-=]*$")]
    private static partial Regex SymbolPattern();
}
