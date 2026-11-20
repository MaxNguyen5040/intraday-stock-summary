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

To be documented.

## Architecture and decisions

To be documented.
