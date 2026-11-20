# Prompt Log

Tool: Claude Code (Claude Opus) in VS Code, unless noted otherwise.

Each entry records the prompt, why I sent it, and what I kept, changed, or rejected from the output.

---

## 1. Planning the build

**Prompt**

> Here's a take-home brief (PDF) and the recruiter's notes. AI use is allowed and graded. Before any code: (1) scan the PDF and list any requirements that are easy to miss, (2) probe the API in the brief and report anything that differs from the spec, (3) propose a stack and a 4-5 phase plan sized to 4-5 hours, with what each phase delivers and how it's tested. Don't write files until I approve the plan.

**Why**

Ordered steps, a real API call instead of assumptions, a time budget, and an explicit approval gate, in my experience, give AI more structure to develop the code quality that I'm looking for!

**Outcome** (changed)

I kept the phased plan and the approval gate. The phases became the five commits from the scaffold through the edge-case fixes, each tested before I committed it. I replaced the proposed Node/TypeScript backend after the next prompt.

## 2. Stack choice

**Prompt**

I pasted the job description and said "feel free to change your tech stack".

> Here's the job description. Compare Node/TS and C#/.NET for this backend on three points: fit with the team's stack, time to finish, and how well each shows OOP/SOLID. Recommend one and re-issue the phases.

**Why**

Named criteria get a real comparison instead of agreement, and the answer doubles as my reasoning for the decision.

**Outcome** (kept)

I went with the recommendation: an ASP.NET Core (.NET 10) API with a React/TypeScript client. The phases were re-issued for that stack and followed as planned.

## 3. Critique: exchange-date grouping fallback

**Prompt**

> Bars are grouped by the exchange's local date, with the GMT offset as a fallback when the time zone is unknown. Critique that. When would the fallback group a bar under the wrong day, and is it worth handling?

**Why**

The fallback only had a test with a +5:30 offset, which has no DST, so I'd never seen it run on data that crosses a DST change. I wanted to know whether it was a real risk or just theory.

**Outcome**

It checked the recorded TSLA fixture and found a real flaw. `gmtoffset` is the offset at request time, so in fallback mode every bar before the Nov 1 DST change is shifted by an hour. US stocks are never affected because they trade far from midnight. 24-hour instruments in DST zones (FX, futures) would lose their first four bars each day to the previous day. The realistic trigger is a container without tzdata, not Yahoo itself. It recommended resolving `America/New_York` at startup and failing loudly, and said a tradingPeriods-based fallback or NodaTime weren't worth it yet. I didn't add the startup check; noted it as future work

## 4. Critique: server cache plus HTTP cache staleness

**Prompt**

> I cache each symbol for 60s on the server and also send `max-age=60`, so data can be about 2 minutes stale. Argue for and against computing the remaining time per entry instead, and tell me whether you'd change it.

**Why**

I'd accepted the roughly 2-minute worst case while adding `Cache-Control` (manual change 2) and wanted a second opinion on whether that was lazy.

**Outcome** (kept)

It confirmed the bound (about 119s, with TanStack Query adding nothing because its refetch hits the browser cache) and recommended leaving it, since an extra minute on 15-minute bars changes nothing for a user. The useful part was the "if it ever matters" path: send an `Age` header (RFC 9111) rather than recalculating `max-age`, and add an `ETag` once entries store their fetch time. It also noted that a CDN is when this starts to matter. I kept the current behavior; the tradeoff is documented under manual change 2.

## 5. Critique: always-on partial-day note

**Prompt**

> I always show 'the latest day may be incomplete' instead of detecting whether the market is open. Is that a cop-out? What would detecting it properly take (Yahoo's trading-period metadata?), and what would it risk?

**Why**

The note is always true, but it also shows on weekends when nothing is in progress. I wanted to know if real detection was within reach.

**Outcome**

It said "partly a cop-out": a note that's usually irrelevant gets ignored. Proper detection could use Yahoo's `currentTradingPeriod` and `tradingPeriods`, but would need a change to the response format the brief fixes. It would also have to handle half-days (the day after Thanksgiving), Tokyo's lunch break, 24-hour instruments and delayed data. It suggested a client-only middle ground: show the note only if the latest day could still be "today" somewhere on Earth (UTC-12), which never misses and hides it on weekends. 

## 6. Critique: rounding in the API

**Prompt**

> Averages are rounded to 4 decimals with midpoints away from zero, on the server. Should rounding be in the API or the client, and is away-from-zero the right rule over banker's rounding for financial display?

**Why**

.NET defaults to banker's rounding, so I'd picked away-from-zero on purpose and wanted to check the reasoning held up.

**Outcome** (kept)

It agreed on both counts. Rounding belongs in the API because the brief specifies the precision, and it happens once, after averaging at full precision. Away-from-zero is ordinary half-up for prices, and banker's rounding only helps when rounded values get summed. It also checked the fixture and found that Yahoo sends float32 prices (365.27 arrives as 365.2699890136719). For high-priced instruments like ^GSPC, that makes the 4th decimal noise. I kept the rounding as it is. One caveat: the client computes spread and percent change from already-rounded values, which is fine for display but shouldn't be built on.

---

## Manual changes (made outside AI)

The four code changes below were written by hand after the AI-assisted phases, each in its own commit so they can be reviewed separately. I used AI only to help scope them (a written spec for each) and to review the finished diffs.

### 1. Partial-day note (`79ad270`)

**What:** Added a line under the results header: "The latest day may be incomplete while its market is open."

**Why:** While the market is open, the most recent day's averages cover only the bars so far, and its volume is a running total. Without a note, today looks like a normal day with unusually low volume, which is misleading. I chose to always show the note rather than detect whether the market is open. The client doesn't know the exchange's timezone or trading hours, so a check would sometimes be wrong. The word "may" keeps the note accurate at all times.

**How:** One `<p>` in `SummaryView`, styled with `flex-basis: 100%` so it wraps under the period line in the existing flex header. The tests confirm it appears with results and not on the welcome, error or empty states.

### 2. `Cache-Control` on successful responses (`7c06a62`)

**What:** Successful API responses now send `Cache-Control: public, max-age=<CacheSeconds>`, or `no-store` when caching is turned off.

**Why:** The server already caches each symbol for 60 seconds, but browsers and CDNs couldn't reuse the response, so flipping between symbols always made a round-trip. I read the duration from the existing `StockSummaryOptions` instead of hardcoding it, so the server cache and HTTP cache can't drift apart. I made sure errors never get a `max-age`, because a cached 503 would keep showing "rate limited" after Yahoo recovered. `public` is safe because the response holds no user data.

**Tradeoff I accepted:** a response can be up to 60 seconds old in the server cache and then kept another 60 by the browser, so the worst case is about 2 minutes stale. For 15-minute bars that's fine. Tracking the remaining time per cache entry would fix it but isn't worth the complexity yet.

**How:** Set through the typed headers API in the controller, just before `Ok(...)`, so error paths never reach it. Added an optional `cacheSeconds` parameter to the test factory, plus three integration tests: cacheable when on, `no-store` when off, and no `max-age` on a 404. Updated the README's status table.

### 3. More useful quick picks (`ededa94`)

**What:** Replaced `NVDA` with `7203.T` (Toyota, Tokyo Stock Exchange) and added a hover description to each quick-pick chip.

**Why:** NVDA showed nothing TSLA didn't already show. 7203.T lets a reviewer see the exchange-timezone handling and the suffix symbol format with one click: when it's already the next day in Tokyo, its latest day is ahead of the US symbols. I kept ^GSPC for the index case and left TSLA, AAPL and MSFT alone because existing tests rely on them. The descriptions go in `title`, so the button's accessible name stays the bare symbol.

**How:** Changed `QUICK_PICKS` to `{ symbol, description }` objects. Added a test that the 7203.T chip loads results and updates the URL.

### 4. `/` keyboard shortcut (`2afdb9d`)

**What:** Pressing `/` anywhere on the page focuses the symbol input and selects its contents, with a small `/` hint next to the label.

**Why:** It's a common convention (GitHub, YouTube) and speeds up repeat lookups, which is the app's main loop. The edge cases mattered more than the feature:

- The shortcut is ignored while typing in any editable field, so `/` can still be typed.
- It's ignored when Ctrl, Cmd or Alt is held, so browser shortcuts keep working.
- It calls `preventDefault()`, so the `/` doesn't end up in the box.
- The hint is hidden on narrow screens, since phones don't have a physical keyboard.

**How:**

- a window `keydown` listener in a `useEffect`, with cleanup, because StrictMode mounts effects twice
- a ref on the input
- `aria-keyshortcuts="/"` for screen readers
- four tests in a new `SymbolSearch.test.tsx`:
  - focuses without typing the slash
  - selects existing text when pressed from a chip
  - lets `/` be typed inside the input
  - ignores Ctrl+/

### Other manual decisions

- **Stack:** after reading the job description, I switched the planned backend from Node/TypeScript to C#/.NET to match the team's core stack.
- **Repo:** I created the GitHub repo and gave it a neutral name, keeping the company name out of a public repo and the brief PDF out of version control.
- **Workflow:** I set the commit cadence to one commit per phase, ran and pushed every commit, and fixed a mangled commit message by amending it.
- **Testing:** I clicked through the running app myself before submitting.

