# Intraday Stock Summary

[![CI](https://github.com/MaxNguyen5040/intraday-stock-summary/actions/workflows/ci.yml/badge.svg)](https://github.com/MaxNguyen5040/intraday-stock-summary/actions/workflows/ci.yml)

Enter a stock symbol and see the last month of 15-minute intraday data rolled up by trading day: average low, average high, and total volume.

- `server/` ASP.NET Core Web API (.NET 10) that queries Yahoo Finance and aggregates by day
- `client/` React + TypeScript single-page app (Vite)

## Prerequisites

- .NET SDK 10.0.100 or later (`dotnet --version`)
- Node.js 24 LTS (22.22+ also works) with npm. `client/.nvmrc` pins the major version for `nvm use`.

## Running locally

Start the API (listens on http://localhost:5080):

```sh
cd server
dotnet run --project src/StockSummary.Api
```

In a second terminal, start the client and open http://localhost:5173:

```sh
cd client
npm ci
npm run dev
```

The Vite dev server proxies `/api` to the API, so no CORS setup is needed in development. Set `API_PROXY_TARGET` to point the proxy elsewhere, or `VITE_API_BASE_URL` at build time if the built client is served from a different origin than the API.

Try `TSLA`, `BRK-B`, `^GSPC`, or an invalid symbol like `NOTAREALSYM123`. The selected symbol is kept in the URL (`?symbol=TSLA`), so results can be bookmarked and the back button moves between lookups.

## Tests and checks

```sh
# API: unit, recorded-fixture and in-process HTTP tests
cd server && dotnet test

# Client: component and API-client tests (Vitest, Testing Library, MSW), plus static checks
cd client && npm test && npm run lint && npm run typecheck && npm run format:check && npm run build
```

[CI](.github/workflows/ci.yml) runs the same checks on every push and pull request, plus `dotnet format --verify-no-changes`. Dependabot opens weekly grouped updates for NuGet, npm and GitHub Actions.

## API

### `GET /api/v1/stocks/{symbol}/daily-summary`

Fetches the last month of 15-minute bars from Yahoo Finance and groups them by trading day in the exchange's time zone. Days are sorted oldest first. Averages are rounded to 4 decimals (midpoints away from zero); volume is the day's total.

```json
[
  { "day": "2026-10-20", "lowAverage": 373.0985, "highAverage": 374.9545, "volume": 27467645 }
]
```

Symbols are case-insensitive and may include `.`, `-`, `=` and a leading `^` (for example `BRK-B`, `^GSPC`, `EURUSD=X`).

| Status | When |
|---|---|
| 200 | Success. An empty array means the symbol exists but had no trades in the window. Sent with `Cache-Control: public, max-age=<CacheSeconds>` (or `no-store` when `CacheSeconds` is 0). |
| 400 | The symbol fails validation. |
| 404 | Yahoo has no data for the symbol. |
| 429 | This client exceeded the API's rate limit (30 requests/minute by default). |
| 502 | Yahoo was unreachable or returned an unusable response. |
| 503 | Yahoo is rate limiting us. `Retry-After` is passed through when Yahoo sends it. |

Errors use `application/problem+json` (RFC 9457).

Other endpoints: `GET /healthz`, and `GET /openapi/v1.json` in Development. [StockSummary.Api.http](server/src/StockSummary.Api/StockSummary.Api.http) has ready-made requests.

### Configuration

Settings live in [appsettings.json](server/src/StockSummary.Api/appsettings.json) and can be overridden with environment variables (for example `StockSummary__CacheSeconds=0`). Invalid values stop the app at startup.

| Key | Default | Purpose |
|---|---|---|
| `MarketData:Yahoo:Range` | `1mo` | Lookback window sent to Yahoo |
| `MarketData:Yahoo:Interval` | `15m` | Bar size |
| `MarketData:Yahoo:AttemptTimeoutSeconds` | `5` | Per-attempt timeout |
| `MarketData:Yahoo:MaxRetryAttempts` | `2` | Retries on transient failures |
| `StockSummary:CacheSeconds` | `60` | In-memory cache per symbol, `0` disables |
| `RateLimiting:PermitLimit` / `WindowSeconds` | `30` / `60` | Per-IP fixed window |
| `Cors:AllowedOrigins` | `[]` (dev: `http://localhost:5173`) | Origins allowed to call the API directly |

## Architecture

```
client (React)                          server (ASP.NET Core)
--------------                          ---------------------
SymbolSearch ─┐                         StocksController         validates symbol, maps to response DTO
SummaryView ──┼─ useDailySummary ──────▶   │
  stats, chart, table   (TanStack Query)   ▼
                                        StockSummaryService      caching, orchestration
                                           │            │
                                           ▼            ▼
                              IMarketDataProvider    DailySummaryAggregator
                                           │            (pure: group by exchange-local day,
                                           ▼             average, round, sum)
                                 YahooFinanceProvider
                                 (HttpClient + resilience handler)
```

```
server/
  src/StockSummary.Api/
    Domain/           StockSymbol, PriceBar, DailySummary, DailySummaryAggregator
    MarketData/       IMarketDataProvider, exceptions, Yahoo/ implementation and DTOs
    Stocks/           controller, service, response contract, options
    Infrastructure/   DI wiring, problem-details mapping, rate limiting, CORS
  tests/StockSummary.Api.Tests/
    Domain/ MarketData/ Stocks/ Api/   one folder per layer
    Fixtures/                          recorded Yahoo responses
client/src/
  api/          fetch client, error model, query hook
  components/   search, stats, chart, table, status panels
  lib/          formatting, period stats, symbol rules, URL state
  test/         MSW server, render helper, setup
```

## Decisions

**Last month means `range=1mo`.** The example URL in the brief has no `range`, and without it Yahoo returns only the current session. The range and interval are configuration, not constants, so a different window is a settings change.

**Days are grouped in the exchange's time zone.** Yahoo returns UTC timestamps plus `exchangeTimezoneName`. Grouping by UTC date happens to work for US stocks during regular hours but breaks for exchanges like the ASX, whose session starts the previous day in UTC. Using the IANA zone rather than Yahoo's fixed `gmtoffset` also keeps dates right across a DST change, which happened inside the current one-month window (Nov 1). There are tests for both cases.

**Money math uses `decimal`.** Yahoo's prices are float32 values serialized as long decimals (`363.6400146484375`). They are parsed directly into `decimal`, averaged, and rounded once to 4 places with midpoints away from zero, so results match a hand calculation. One test checks the aggregation of a recorded Yahoo response against values computed independently in Python.

**Provider behind an interface.** `IMarketDataProvider` keeps Yahoo-specific parsing and HTTP concerns out of the service. Swapping in another data source, or adding one, means a new implementation and a DI registration. The aggregation is a static pure function because it has no dependencies to inject.

**Errors are typed, then mapped once.** The provider throws `SymbolNotFoundException`, `MarketDataRateLimitedException` or `MarketDataUnavailableException`, and a single `IExceptionHandler` turns those into problem details. The client mirrors this with an `ApiError.kind` that drives what the user sees and whether a retry button is offered (it isn't for an unknown symbol). A bare 502/503/504 without a problem body is treated as "cannot reach the server", because that comes from a proxy in front of the API rather than from the API itself.

**Separate API contract.** `DailySummaryResponse` is the public shape; the domain record can change without breaking clients. The route is versioned (`/api/v1`) for the same reason.

**Protecting the upstream.** Yahoo's endpoint is unofficial and rate limited. A 60 second per-symbol cache, a per-client rate limit, and the standard resilience handler (timeouts, retries with backoff, circuit breaker) keep repeat lookups and bursts from turning into Yahoo traffic.

**Client data fetching.** TanStack Query handles caching, deduping and cancellation. The chart library is lazy-loaded since it's most of the bundle and only needed once data arrives. Trading days are formatted in UTC so `2026-10-20` never displays as Oct 19 in western time zones. Prices under 10 (FX pairs) show four decimals, and volume figures are hidden for instruments Yahoo reports no volume for.

## Testing

- **Server (49 tests, xUnit):** symbol validation; aggregation including the time zone, DST and rounding cases; the Yahoo provider against recorded responses and every failure mode (404, 429, 5xx, malformed JSON, timeouts, caller cancellation); caching; and in-process HTTP tests through `WebApplicationFactory` that assert the exact JSON shape, status codes and problem-details content type.
- **Client (42 tests, Vitest + Testing Library + MSW):** the API client's error mapping, formatting and stats helpers, table sorting, and full user flows through `<App />` (search, quick picks, URL state, back navigation, validation, each error state, retry, empty and no-volume results). MSW is set to fail on any unhandled request.
- **Manual:** run against live Yahoo with TSLA, AAPL, MSFT, SPY, BRK-B, ^GSPC, 7203.T, EURUSD=X, BTC-USD, an unknown symbol, invalid input, the API stopped, and the rate limit exceeded; checked in light and dark mode and at narrow widths.

## Limitations and future work

- **Yahoo is unofficial.** It has no SLA and can change shape or block traffic. For production I'd move to a licensed provider; the interface boundary makes that a contained change.
- **Cache is per instance.** `IMemoryCache` is fine for one node. Multiple instances would want `HybridCache` with Redis, which also adds stampede protection.
- **Rate limiting uses the connection's IP.** Behind a load balancer this needs forwarded-headers middleware so each client gets its own bucket.
- **No persistence.** Nothing needs storing yet. If history beyond Yahoo's 60-day intraday window became a requirement, the next step would be storing daily summaries in Postgres and serving from there.
- **Possible features:** comparing symbols, custom date ranges and intervals, symbol search/autocomplete, CSV export, and an end-to-end Playwright test against a stubbed provider.

## AI collaboration

See [PROMPT_LOG.md](PROMPT_LOG.md) for the prompts used, why, and what was kept, changed or rejected.
