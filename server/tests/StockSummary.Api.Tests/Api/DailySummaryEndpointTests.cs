using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockSummary.Api.MarketData;
using StockSummary.Api.Tests.TestSupport;
using static StockSummary.Api.Tests.TestSupport.TestData;

namespace StockSummary.Api.Tests.Api;

public sealed class DailySummaryEndpointTests : IDisposable
{
    private readonly FakeMarketDataProvider _provider = new();
    private readonly WebApplicationFactory<Program> _factory;

    public DailySummaryEndpointTests()
    {
        _factory = CreateFactory(_provider, permitLimit: 1000);
    }

    [Fact]
    public async Task Returns_daily_summaries_in_the_documented_json_shape()
    {
        _provider.Returns(
            Bar("2026-11-17T14:30:00Z", 40.29581m, 49.75341m, 49_000_000),
            Bar("2026-11-17T14:45:00Z", 40.29581m, 49.75341m, 73_348));

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/stocks/tsla/daily-summary", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            """[{"day":"2026-11-17","lowAverage":40.2958,"highAverage":49.7534,"volume":49073348}]""",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Returns_empty_array_when_symbol_has_no_bars()
    {
        _provider.Returns();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/stocks/TSLA/daily-summary", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Returns_400_problem_for_invalid_symbol_without_calling_provider()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/stocks/BAD%20SYM/daily-summary", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("symbol", problem!.Errors.Keys);
        Assert.Equal(0, _provider.CallCount);
    }

    [Fact]
    public async Task Returns_404_problem_when_symbol_is_unknown()
    {
        _provider.Throws(new SymbolNotFoundException(Symbol("NOPE")));

        var problem = await GetProblemAsync("/api/v1/stocks/NOPE/daily-summary", HttpStatusCode.NotFound);

        Assert.Equal("Symbol not found", problem.Title);
        Assert.Contains("NOPE", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Returns_503_with_retry_after_when_upstream_rate_limits()
    {
        _provider.Throws(new MarketDataRateLimitedException(TimeSpan.FromSeconds(30)));

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/stocks/TSLA/daily-summary", UriKind.Relative));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Returns_502_problem_when_upstream_fails()
    {
        _provider.Throws(new MarketDataUnavailableException("down"));

        var problem = await GetProblemAsync("/api/v1/stocks/TSLA/daily-summary", HttpStatusCode.BadGateway);

        Assert.Equal("Market data provider error", problem.Title);
    }

    [Fact]
    public async Task Returns_429_problem_when_client_exceeds_rate_limit()
    {
        using var factory = CreateFactory(new FakeMarketDataProvider(), permitLimit: 2);
        using var client = factory.CreateClient();
        var uri = new Uri("/api/v1/stocks/TSLA/daily-summary", UriKind.Relative);

        await client.GetAsync(uri);
        await client.GetAsync(uri);
        var response = await client.GetAsync(uri);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/healthz", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    public void Dispose() => _factory.Dispose();

    private async Task<ProblemDetails> GetProblemAsync(string path, HttpStatusCode expectedStatus)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        return problem;
    }

    private static WebApplicationFactory<Program> CreateFactory(IMarketDataProvider provider, int permitLimit) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StockSummary:CacheSeconds", "0");
            builder.UseSetting("RateLimiting:PermitLimit", permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMarketDataProvider>();
                services.AddSingleton(provider);
            });
        });
}
