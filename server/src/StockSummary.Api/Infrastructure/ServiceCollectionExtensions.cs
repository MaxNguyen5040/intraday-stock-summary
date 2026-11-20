using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using StockSummary.Api.MarketData;
using StockSummary.Api.MarketData.Yahoo;
using StockSummary.Api.Stocks;

namespace StockSummary.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStockSummary(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StockSummaryOptions>()
            .Bind(configuration.GetSection(StockSummaryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddScoped<IStockSummaryService, StockSummaryService>();

        return services.AddYahooFinance(configuration);
    }

    private static IServiceCollection AddYahooFinance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<YahooFinanceOptions>()
            .Bind(configuration.GetSection(YahooFinanceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMarketDataProvider, YahooFinanceProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<YahooFinanceOptions>>().Value;
                client.BaseAddress = options.BaseUrl;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions resilience, IServiceProvider provider) =>
            {
                var options = provider.GetRequiredService<IOptions<YahooFinanceOptions>>().Value;
                var attemptTimeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);

                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                resilience.TotalRequestTimeout.Timeout = attemptTimeout * (options.MaxRetryAttempts + 1) + TimeSpan.FromSeconds(5);
                resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2 + TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
