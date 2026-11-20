using StockSummary.Api.Domain;

namespace StockSummary.Api.MarketData;

public abstract class MarketDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class SymbolNotFoundException(StockSymbol symbol)
    : MarketDataException($"No market data found for symbol '{symbol}'.")
{
    public StockSymbol Symbol { get; } = symbol;
}

public sealed class MarketDataRateLimitedException(TimeSpan? retryAfter)
    : MarketDataException("The market data provider is rate limiting requests.")
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

public sealed class MarketDataUnavailableException(string message, Exception? innerException = null)
    : MarketDataException(message, innerException);
