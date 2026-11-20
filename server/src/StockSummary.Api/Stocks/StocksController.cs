using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StockSummary.Api.Domain;
using StockSummary.Api.Infrastructure;

namespace StockSummary.Api.Stocks;

[ApiController]
[Route("api/v1/stocks")]
[EnableRateLimiting(RateLimitingSetup.ApiPolicy)]
public sealed class StocksController(IStockSummaryService stockSummaryService) : ControllerBase
{
    /// <summary>Last month of intraday data for a symbol, grouped by trading day.</summary>
    [HttpGet("{symbol}/daily-summary")]
    [ProducesResponseType<IReadOnlyList<DailySummaryResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<DailySummaryResponse>>> GetDailySummary(
        string symbol,
        CancellationToken cancellationToken)
    {
        if (!StockSymbol.TryParse(symbol, out var stockSymbol))
        {
            ModelState.AddModelError(
                nameof(symbol),
                $"'{symbol}' is not a valid ticker symbol. Use 1-{StockSymbol.MaxLength} letters, digits, '.', '-', '=' or a leading '^'.");
            return ValidationProblem(ModelState);
        }

        var summaries = await stockSummaryService.GetDailySummariesAsync(stockSymbol, cancellationToken);
        return Ok(summaries.Select(DailySummaryResponse.From).ToList());
    }
}
