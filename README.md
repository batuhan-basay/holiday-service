# Holiday API

[![ci](https://github.com/batuhan-basay/holiday-service/actions/workflows/ci.yml/badge.svg)](https://github.com/batuhan-basay/holiday-service/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A REST API that answers calendar questions for Türkiye, the United States, the United
Kingdom, Germany and Spain: which days are public holidays in a given year, which weekends
stretch into long weekends, and how many **business days** sit between two dates once
weekends and holidays are taken out.

It has no database, no cache server and no upstream provider. Each country's calendar is a
set of **rules** shipped with the assembly, and the holidays for a year are computed from
them on demand.

---

## Why this exists

"When is the next holiday" is easy. The questions that actually show up in production are
the ones underneath it:

- *Payment settles in 3 business days* — which calendar day is that, in Türkiye, in a week
  that contains Kurban Bayramı?
- *This SLA runs 10 working days* — how many of those days are lost to the weekend?
- *4 July falls on a Saturday* — do the US offices close on Friday or on Monday?

Those need a holiday calendar plus day arithmetic, which is what this service exposes.

---

## The rule engine

A country is not a list of dates. It is a list of rules, and dates are what you get when
you apply them to a year. Four rule kinds cover every holiday in the five calendars:

| Kind | Produces | Example |
|---|---|---|
| `fixed` | The same calendar day every year | 29 October — Cumhuriyet Bayramı |
| `nthWeekday` | The *n*-th weekday of a month, `-1` for the last | 4th Thursday of November — Thanksgiving |
| `easter` | An offset from Easter Sunday | Easter − 2 — Good Friday |
| `hijri` | A Hijri-calendar date, optionally spanning several days | 1 Shawwal, 3 days — Ramazan Bayramı |

Easter comes from the anonymous Gregorian computus; the unit tests pin it against twelve
known dates and assert it always lands on a Sunday between 22 March and 25 April.

On top of the rules sits the part most holiday libraries get wrong — **substitution**, when
a holiday falls on a weekend:

| Policy | Behaviour | Used by |
|---|---|---|
| `none` | Nothing moves; the day is simply lost | Germany, Spain, Türkiye |
| `nextWorkday` | Always forward to the next free weekday | United Kingdom |
| `nearestWorkday` | Saturday moves **back** to Friday, Sunday forward to Monday | United States |

Those three behaviours are genuinely different, and the difference is visible in 2026: the
UK moves Boxing Day from Saturday 26 December to Monday 28 December, the US observes
Independence Day on Friday 3 July because 4 July is a Saturday, and Germany just loses
Tag der Deutschen Einheit to Saturday 3 October. Substituted days never collide — if two
holidays would land on the same Monday, the second one walks on.

### A note on religious holidays

Ramazan and Kurban Bayramı are derived from the tabular Hijri calendar. That calendar is
arithmetic, while the official dates are announced by a religious authority, so the two can
differ by a day — and the drift is not constant:

| Year | Computed Ramazan Bayramı | Officially observed |
|---|---|---|
| 2025 | 31 March | 30 March |
| 2026 | 20 March | 20 March |
| 2027 | 10 March | 9 March |

The API does not hide this. Every Hijri-derived holiday comes back with
`"isEstimated": true`, and `HolidayData:HijriDayAdjustment` shifts them globally if you need
to align with a published calendar. Fixed, weekday and Easter rules are always exact and
report `"isEstimated": false`.

---

## Architecture

Four projects, dependencies pointing inwards only:

```
HolidayService.Api              ASP.NET Core · controllers, auth, rate limiting, versioning
        ↓
HolidayService.Application      CQRS handlers, validation, contracts, query pipeline
        ↓                                           ↑
HolidayService.Domain           rules, calendars, work-day arithmetic — no dependencies
                                                    ↑
HolidayService.Infrastructure   embedded country JSON, rule mapping, caching decorator
```

`Infrastructure` depends on `Application` because it implements its interfaces, and is
referenced only by the composition root in `Program.cs`.

### Patterns, and what each one buys

| Pattern | Where | Why it is there |
|---|---|---|
| **Clean Architecture** | project layout | The rule engine and work-day maths are testable without a host, a cache or a web server. |
| **Rule engine** | `Domain/Holidays/HolidayRule.cs` | A new country is data plus zero new code, as long as its holidays fit the four rule kinds. |
| **Strategy** | `SubstitutionPolicy`, `WeekendDefinition` | Weekend days and weekend-substitution are country data, not an `if` ladder. |
| **CQRS (read side)** | `Application/*/Queries` | Every operation is a query object with one handler; no service class grows into a god object. |
| **Vertical slices** | one file per query | Query, validator and handler sit together, so a change to an endpoint touches one file. |
| **Mediator** | `Application/Messaging` | Controllers only translate HTTP to a query. Hand-written rather than taken from a library — see *Dependencies* below. |
| **Pipeline behaviors** | `Application/Behaviors` | Validation and structured logging apply to every handler without a line of handler code. |
| **Result pattern** | `Domain/Common/Result.cs` | Expected outcomes — unsupported country, bad range — are return values, not exceptions. |
| **Decorator** | `CachedHolidayProvider` | Materialised years are cached without the handlers or the rule engine knowing a cache exists. |
| **Anti-corruption layer** | `Calendars/CalendarDefinitionMapper` | The JSON shape is translated to domain rules in one place, so the file format can change freely. |
| **Options pattern** | `HolidayDataOptions`, `ApiKeyOptions`, `RateLimitOptions` | Configuration is typed and validated at startup, so a bad setting fails the boot rather than the first request. |
| **Value object** | `CountryCode` | `"tr"`, `" TR "` and `"TR"` normalise to one value, safe as a cache key and an equality key. |

### Request flow

```
HTTP  →  Controller  →  QueryDispatcher  →  LoggingBehavior  →  ValidationBehavior  →  Handler
                                                                                        ↓
                                                   CachedHolidayProvider  →  CalendarHolidayProvider
                                                             ↓ (hit)                    ↓
                                                         IMemoryCache          HolidayCalendar.ForYear
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
| `GET` | `/api/v1/countries` | The supported countries |
| `GET` | `/api/v1/countries/{countryCode}` | One country's names and weekend days |

### Holidays

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/holidays/{countryCode}/{year}` | All holidays in a year. Optional `?type=Public` |
| `GET` | `/api/v1/holidays/{countryCode}/{year}/{month}` | Holidays in one month |
| `GET` | `/api/v1/holidays/{countryCode}/range?from=&to=` | Holidays in an arbitrary range, year boundaries included |
| `GET` | `/api/v1/holidays/{countryCode}/next?count=5` | The next N holidays from today |
| `GET` | `/api/v1/holidays/{countryCode}/upcoming?days=30` | Holidays inside a look-ahead window |
| `GET` | `/api/v1/holidays/{countryCode}/long-weekends/{year}` | Consecutive days off. `?includeBridgeDays=false` to exclude one-day-off opportunities |
| `GET` | `/api/v1/holidays/{countryCode}/statistics/{year}` | Counts per month and weekday, plus how many holidays are lost to the weekend |
| `GET` | `/api/v1/holidays/{countryCode}/check?date=` | Is this date a holiday, a weekend day or a workday? |
| `GET` | `/api/v1/holidays/worldwide/upcoming` | The next holidays across every supported country |

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
  "publicHolidayDays": 4
}
```

A holiday carries the two honesty flags:

```json
{
  "date": "2026-03-20",
  "name": "Ramadan Feast",
  "localName": "Ramazan Bayramı",
  "countryCode": "TR",
  "dayOfWeek": "Friday",
  "types": ["Public"],
  "isSubstitute": false,
  "isEstimated": true
}
```

And an error:

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Country 'FR' is not covered by this service.",
  "instance": "/api/v1/holidays/FR/2026",
  "errorCode": "Country.NotSupported",
  "traceId": "0HN7G2A4F1QKL:00000001"
}
```

---

## Adding a country

Drop a JSON file into `src/HolidayService.Infrastructure/Calendars/Data/`. It is picked up
as an embedded resource at build time; no code changes, no registration.

```json
{
  "countryCode": "FR",
  "name": "France",
  "officialName": "French Republic",
  "weekend": ["Saturday", "Sunday"],
  "holidays": [
    { "kind": "fixed",  "month": 1, "day": 1, "name": "New Year's Day", "localName": "Jour de l'An" },
    { "kind": "easter", "offsetDays": 1, "name": "Easter Monday", "localName": "Lundi de Pâques" }
  ]
}
```

Optional per holiday: `types` (defaults to `["Public"]`), `substitution`
(`none` | `nextWorkday` | `nearestWorkday`), and for `hijri` rules `length` and
`dayAdjustment`.

---

## Running it

### Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Nothing else. No database, no network access, no API keys to anyone else's service.

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
HOLIDAY_API_KEY=$(openssl rand -hex 24) docker compose up --build
# → http://localhost:8080/swagger
```

### `.http` file

`src/HolidayService.Api/HolidayService.Api.http` holds a request for every endpoint, ready
to run from Visual Studio, Rider or the VS Code REST Client.

---

## Configuration

Every setting can be overridden with an environment variable using `__` as the separator,
e.g. `HolidayData__CacheDuration=06:00:00`.

| Setting | Default | Meaning |
|---|---|---|
| `HolidayData:CacheDuration` | `12:00:00` | How long a materialised holiday year stays cached |
| `HolidayData:HijriDayAdjustment` | `0` | Shifts every Hijri-derived holiday by ±1–2 days |
| `RateLimiting:PermitLimit` | `100` | Requests per window, per client |
| `RateLimiting:WindowSeconds` | `60` | Window length |
| `ApiKeys:Enabled` | `true` | Set to `false` to run the API open |
| `ApiKeys:Clients:N:Owner` / `:Key` | — | Issued keys |

**Keys are never committed.** Development uses the value in
`appsettings.Development.json`; anything else supplies keys through environment variables
or `dotnet user-secrets`.

---

## Dependencies

Everything this project depends on is free for commercial use under a permissive licence,
and that was a deliberate constraint rather than an accident:

| Package | Licence |
|---|---|
| FluentValidation | Apache-2.0 |
| Asp.Versioning.Mvc | MIT |
| Swashbuckle.AspNetCore | MIT |
| `Microsoft.Extensions.*` | MIT |
| xUnit | Apache-2.0 |
| NSubstitute, Shouldly | BSD |

**MediatR is deliberately absent.** It went commercial — current versions ask for a licence
key — so the mediator here is a ~60-line `QueryDispatcher` in `Application/Messaging`. It
resolves the handler for a query, wraps it in the registered behaviors and caches the closed
generic executor per query type. The CQRS shape, the pipeline and the handler signatures are
unchanged; the dependency is gone. Shouldly and NSubstitute are used in place of
FluentAssertions and Moq for the same reason.

---

## Tests

```bash
dotnet test
```

The suite covers the parts where the logic actually lives: Easter against twelve known
dates and two centuries of invariants; each rule kind including last-weekday and
February 29; all three substitution policies and the collision case where two substituted
holidays compete for the same Monday; business-day arithmetic across weekends, holidays and
negative shifts; long-weekend and bridge-day detection; country-specific weekends;
`CountryCode` normalisation; the `Result` contract; and the validation behavior.

The highest-value test loads the five embedded calendars and asserts their **complete 2026
output, date by date** — including the UK's Boxing Day substitute, the US Independence Day
observance and Germany's unsubstituted Saturday. A second test materialises every country
across 1990–2060 to prove no year throws and no holiday escapes its own year.

---

## Possible next steps

- **More countries** — the engine is ready; each one is a JSON file
- **Subdivision support** — German *Länder* and Spanish *comunidades* have their own days
- **Redis** behind `ICacheService` for a multi-instance deployment
- **OpenTelemetry** traces and metrics, plus Serilog structured sinks
- **`/api/v2`** returning official Diyanet dates for Türkiye instead of computed ones

---

## License

MIT — see [LICENSE](LICENSE).
