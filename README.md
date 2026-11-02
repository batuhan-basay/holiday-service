# Holiday API

[![ci](https://github.com/batuhan-basay/holiday-service/actions/workflows/ci.yml/badge.svg)](https://github.com/batuhan-basay/holiday-service/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A REST API that answers calendar questions for 100+ countries: which days are public
holidays in a given year, which weekends stretch into long weekends, and how many
**business days** sit between two dates once weekends and holidays are taken out.

It is built as a stateless read API over a public upstream provider
([Nager.Date](https://date.nager.at/)), with caching, resilience, API-key authentication,
rate limiting and versioning around it — the parts that turn a wrapper into a service you
can actually put in front of other teams.

---

## Why this exists

"When is the next holiday" is easy. The questions that actually show up in production are
the ones underneath it:

- *Payment settles in 3 business days* — which calendar day is that, in Turkey, in a week
  that contains a religious holiday?
- *This SLA runs 10 working days* — how many of those days are lost to the weekend?
- *Our Saudi branch rests on Friday and Saturday* — whose weekend are we counting?

Those need a holiday calendar plus day arithmetic, which is what this service exposes.

---

## Architecture

Four projects, dependencies pointing inwards only:

```
HolidayService.Api              ASP.NET Core · controllers, auth, rate limiting, versioning
        ↓
HolidayService.Application      CQRS handlers, validation, contracts, pipeline behaviors
        ↓                                           ↑
HolidayService.Domain           holidays, countries, work calendar — no dependencies
                                                    ↑
HolidayService.Infrastructure   Nager.Date client, resilience, caching decorator
```

`Infrastructure` depends on `Application` (it implements its interfaces) and is referenced
only by the composition root in `Program.cs`. Nothing in `Application` or `Domain` knows
that Nager.Date, HTTP or a cache exist.

### Patterns, and what each one buys

| Pattern | Where | Why it is there |
|---|---|---|
| **Clean Architecture** | project layout | Domain rules are testable without a host, a cache or a network. |
| **CQRS (read side)** | `Application/*/Queries` | Every operation is a query object with one handler; no service class grows into a god object. |
| **Vertical slices** | one file per query | Query, validator and handler sit together, so a change to an endpoint touches one file. |
| **Mediator** | MediatR | Controllers only translate HTTP to a query; cross-cutting concerns attach as pipeline behaviors. |
| **Pipeline behaviors** | `Application/Behaviors` | Validation and structured logging apply to every handler without a line of handler code. |
| **Result pattern** | `Domain/Common/Result.cs` | Expected outcomes — unknown country, provider down — are return values, not exceptions. Exceptions stay for genuine faults. |
| **Decorator** | `CachedHolidayProvider` | Caching wraps the provider instead of leaking into handlers; the raw client stays cache-free and testable. |
| **Anti-corruption layer** | `Nager/NagerMapper` | The upstream payload is mapped to domain types in one place, so an upstream rename changes one file. |
| **Options pattern** | `NagerDateOptions`, `ApiKeyOptions`, `RateLimitOptions` | Configuration is typed and validated at startup (`ValidateOnStart`), so a bad setting fails the boot, not the first request. |
| **Value object** | `CountryCode` | `"tr"`, `" TR "` and `"TR"` normalise to one value, which is then safe as a cache key and an equality key. |
| **Strategy** | `WeekendDefinition` | The weekend is data, not a hard-coded Saturday/Sunday — Friday/Saturday countries are a lookup, not an `if`. |

### Request flow

```
HTTP  →  Controller  →  MediatR  →  LoggingBehavior  →  ValidationBehavior  →  Handler
                                                                                 ↓
                                              CachedHolidayProvider  →  NagerHolidayProvider
                                                      ↓ (hit)                   ↓
                                                  IMemoryCache          HTTP + Polly retry,
                                                                        timeout, breaker
```

Results travel back as `Result<T>` and are mapped to a status code in exactly one place
(`ApiResults`), so no controller picks a status code by hand.

---

## Endpoints

All routes are versioned (`/api/v1/...`), return `application/json`, and require an
`X-API-Key` header. Errors are [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem
details carrying an `errorCode`.

### Countries

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/countries` | Every supported country, sorted by name |
| `GET` | `/api/v1/countries/{countryCode}` | One country's profile and land borders |

### Holidays

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/holidays/{countryCode}/{year}` | All holidays in a year. Optional `?county=DE-BY&type=Public` |
| `GET` | `/api/v1/holidays/{countryCode}/{year}/{month}` | Holidays in one month |
| `GET` | `/api/v1/holidays/{countryCode}/range?from=&to=` | Holidays in an arbitrary range, year boundaries included |
| `GET` | `/api/v1/holidays/{countryCode}/next?count=5` | The next N holidays from today |
| `GET` | `/api/v1/holidays/{countryCode}/upcoming?days=30` | Holidays inside a look-ahead window |
| `GET` | `/api/v1/holidays/{countryCode}/long-weekends/{year}` | Consecutive days off. `?includeBridgeDays=false` to exclude one-day-off opportunities |
| `GET` | `/api/v1/holidays/{countryCode}/statistics/{year}` | Counts per month and weekday, plus how many holidays are lost to the weekend |
| `GET` | `/api/v1/holidays/{countryCode}/check?date=` | Is this date a holiday, a weekend day or a workday? |
| `GET` | `/api/v1/holidays/worldwide/upcoming` | The next holidays across every country |

### Workdays

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/workdays/{countryCode}/count?from=&to=` | Splits a range into workdays, weekend days and holidays |
| `GET` | `/api/v1/workdays/{countryCode}/shift?startDate=&workdays=` | Moves a date by N business days; negative walks backwards |
| `GET` | `/api/v1/workdays/{countryCode}/status?date=` | Is this date a workday? |

### Meta

| Method | Route | Description |
|---|---|---|
| `GET` | `/health` | Liveness probe, no API key required |
| `GET` | `/swagger` | Swagger UI |
| `GET` | `/swagger/v1/swagger.json` | OpenAPI document |

### Example

```bash
curl -H "X-API-Key: dev-local-key" \
  "http://localhost:5080/api/v1/workdays/TR/count?from=2026-01-01&to=2026-03-31"
```

```json
{
  "countryCode": "TR",
  "from": "2026-01-01",
  "to": "2026-03-31",
  "totalDays": 90,
  "workdays": 62,
  "weekendDays": 26,
  "publicHolidayDays": 2
}
```

An error looks like this:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource not found",
  "status": 404,
  "detail": "Country 'ZZ' is not supported by the upstream holiday provider.",
  "instance": "/api/v1/holidays/ZZ/2026",
  "errorCode": "Country.NotSupported",
  "traceId": "0HN7G2A4F1QKL:00000001"
}
```

---

## Running it

### Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Outbound HTTPS to `date.nager.at`
- No database. The service is stateless; the only state is an in-process cache.

### Local

```bash
git clone https://github.com/batuhan-basay/holiday-service.git
cd holiday-service

dotnet restore
dotnet build
dotnet test

dotnet run --project src/HolidayService.Api
# → http://localhost:5080/swagger
```

The development API key is `dev-local-key` (`appsettings.Development.json`).

### Docker

```bash
docker compose up --build
# → http://localhost:8080/swagger
```

Set your own key first:

```bash
HOLIDAY_API_KEY=$(openssl rand -hex 24) docker compose up --build
```

### `.http` file

`src/HolidayService.Api/HolidayService.Api.http` holds a request for every endpoint, ready
to run from Visual Studio, Rider or the VS Code REST Client.

---

## Configuration

Every setting below can be overridden with an environment variable using `__` as the
separator, e.g. `NagerDate__CacheDuration=06:00:00`.

| Setting | Default | Meaning |
|---|---|---|
| `NagerDate:BaseAddress` | `https://date.nager.at/` | Upstream provider |
| `NagerDate:Timeout` | `00:00:10` | Per-attempt timeout |
| `NagerDate:RetryCount` | `3` | Retry attempts on transient failures |
| `NagerDate:CacheDuration` | `12:00:00` | Cache lifetime for the current and future years |
| `NagerDate:HistoricalCacheDuration` | `7.00:00:00` | Cache lifetime for past years, which no longer change |
| `RateLimiting:PermitLimit` | `100` | Requests per window, per client |
| `RateLimiting:WindowSeconds` | `60` | Window length |
| `ApiKeys:Enabled` | `true` | Set to `false` to run the API open |
| `ApiKeys:Clients:N:Owner` / `:Key` | — | Issued keys |

**Keys are never committed.** Development uses the value in
`appsettings.Development.json`; anything else supplies keys through environment variables
or `dotnet user-secrets`.

---

## Design decisions

**No database, by design.** The upstream provider is the source of truth and holiday data
is immutable once published, so the useful state is a cache, not a table. Adding Postgres
here would buy a migration pipeline and a second thing to keep in sync, and nothing else.
The cache sits behind `ICacheService`, so swapping `IMemoryCache` for Redis is one
registration — nothing else moves.

**Caching lives in a decorator, not a MediatR behavior.** Every query — year, month, range,
next, upcoming, long weekends, workday maths — is composed from *whole-year* holiday sets.
Caching at the provider means one upstream call per country-year serves all of them. A
caching behavior keyed on the query would cache thirteen different shapes of the same data
and still miss on the fourteenth.

**Only successful results are cached.** A provider outage must not be remembered for twelve
hours.

**Long weekends are computed here, not taken from upstream.** The upstream endpoint assumes
a Saturday/Sunday weekend everywhere. `WorkCalendar` honours the country's actual weekend,
which is what makes the answer correct for Saudi Arabia or Israel.

**Validation throws, business outcomes return.** A malformed query string is a caller
mistake and becomes a `ValidationAppException` in the pipeline, translated to a 400 once.
"This country does not exist upstream" is an outcome, and travels as `Result.Failure` to a
404. Mixing the two is what makes error handling drift.

**Shouldly and NSubstitute** rather than FluentAssertions and Moq: both are permissively
licensed with no commercial tier, which matters for a repository that anyone can clone.

---

## Tests

```bash
dotnet test
```

`tests/HolidayService.UnitTests` covers the parts where the logic actually lives:
business-day arithmetic across weekends, holidays and negative shifts; long-weekend and
bridge-day detection; country-specific weekends; `CountryCode` normalisation; the `Result`
contract; the validation behavior; and the upstream mapper against every payload shape it
can receive, including malformed ones.

---

## Possible next steps

Deliberately left out of this cut, in rough priority order:

- **Redis** behind `ICacheService` for a multi-instance deployment
- **OpenTelemetry** traces and metrics, plus Serilog structured sinks
- **Webhooks** so callers are notified when a country's calendar changes
- **A second provider** behind `IHolidayProvider`, with failover between them
- **`/api/v2`** adding subdivision-aware workday maths

---

## License

MIT — see [LICENSE](LICENSE).
