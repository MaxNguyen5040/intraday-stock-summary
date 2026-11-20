using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockSummary.Api.MarketData;

namespace StockSummary.Api.Infrastructure;

/// <summary>Maps market data failures to RFC 9457 problem responses.</summary>
public sealed class MarketDataExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = exception switch
        {
            SymbolNotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Symbol not found",
                Detail = $"No intraday data was found for '{notFound.Symbol}'. Check the ticker and try again.",
            },
            MarketDataRateLimitedException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Market data temporarily unavailable",
                Detail = "The market data provider is rate limiting requests. Try again shortly.",
            },
            MarketDataUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Market data provider error",
                Detail = "The market data provider could not be reached or returned an invalid response.",
            },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        if (exception is MarketDataRateLimitedException { RetryAfter: { } retryAfter })
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
