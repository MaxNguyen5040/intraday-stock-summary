# Intraday Stock Summary

Enter a stock symbol and see the last month of 15-minute intraday data rolled up by trading day: average low, average high, and total volume.

- `server/` ASP.NET Core Web API (.NET 10) that queries Yahoo Finance and aggregates by day
- `client/` React + TypeScript single-page app (Vite)

## Prerequisites

- .NET SDK 10.0.100 or later (`dotnet --version`)
- Node.js 20.19+ or 22.12+ with npm

## Running locally

Start the API (listens on http://localhost:5080):

```sh
cd server
dotnet run --project src/StockSummary.Api
```

In a second terminal, start the client (http://localhost:5173):

```sh
cd client
npm install
npm run dev
```

The Vite dev server proxies `/api` to the API, so no CORS setup is needed in development. Set `API_PROXY_TARGET` to point it elsewhere.

## Tests and checks

```sh
cd server && dotnet test
cd client && npm run lint && npm run typecheck && npm run build
```

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
| 200 | Success. An empty array means the symbol exists but had no trades in the window. |
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

## Architecture and decisions

To be documented.
