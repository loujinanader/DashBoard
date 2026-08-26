# KPI Dashboard

ASP.NET Core (.NET 10) Web API that syncs IT helpdesk ticket data from a
[GLPI](https://glpi-project.org/) instance into a local SQL Server database,
to power an IT team KPI dashboard.

## Project layout

- `DashBoard/Controllers/DashboardController.cs` — the read API: `GET /tickets`,
  `GET /total`, `GET /tickets/{id}`, `GET /tickets/user/{userId}`,
  `GET /tickets/status/{statusId}`, `GET /tickets/user/{userId}/totaldetails`,
  `GET /tickets/users/totaldetails` (per-person breakdown for everyone with at
  least one assigned ticket), and `POST /sync` to trigger a sync on demand.
- `web/` — the React + Vite + TypeScript dashboard SPA (no login; open to
  anyone). `npm run dev` (proxies API calls to port 5276) or `npm run build`.
- `DashBoard/Service/DashboardServices/` — reads `TicketEntity` rows via
  `ITicketRepository` and reshapes them into `Ticket`/`DashboardSummary` DTOs.
  **Never calls GLPI directly.**
- `DashBoard/Service/GlpiServices/GLPIService.cs` — the sync path: pulls
  tickets from GLPI via `IGLPIBroker` and upserts them into the `Tickets` table.
- `DashBoard/Service/BackgroundServices/TicketSyncBackgroundService.cs` — runs
  `GLPIService.SyncTicketsAsync()` on a timer (every 5 minutes).
- `DashBoard/ApiBroker/Glpi/GLPIBroker.cs` — the only thing that talks to
  GLPI's REST API and OAuth2 token endpoint.
- `DashBoard/Repository/TicketRepository.cs` — EF Core access to the `Tickets`
  table (`DashboardDbContext`, SQL Server).
- `DashBoard/Models/Glpi/` — GLPI-shaped DTOs: `Ticket`, `Status`, `TeamMember`.
- `DashBoard/Models/Database/TicketEntity.cs` — the persisted row shape.

## How it fits together

`GET /tickets` (and the other read endpoints) **read from the local
database**, not from GLPI. The database is kept up to date by
`TicketSyncBackgroundService`, which calls the same sync logic exposed via
`POST /sync`. So a dashboard read is only ever as fresh as the last sync —
if GLPI is unreachable or misconfigured, existing endpoints still serve
whatever was last synced; only `POST /sync` (and the background timer) fail.

## How GLPI authentication works

This app authenticates to GLPI's OAuth2 API using the `password` grant —
it trades a technical/service GLPI account's username and password directly
for an `access_token`, with no browser or consent step. Every sync run,
`GLPIBroker` requests a fresh `access_token` from GLPI's token endpoint.
There's no `refresh_token` with this grant and nothing is persisted to disk.

The resulting token acts as that GLPI user, so it inherits that account's
entities/profile and ticket-view rights. (`client_credentials` was tried
first since it's the more conventional machine-to-machine grant, but this
GLPI instance's high-level API doesn't accept it as a valid security scheme
at all — see the Changelog below.)

`GetTicketsAsync()` also pages through GLPI's `Assistance/Ticket` results
(GLPI defaults to 100 items per page) using `start`/`limit` query params
until every ticket has been collected, rather than returning just the first
page.

## What you need to do before this works

1. **In GLPI**, have a technical/service user account with rights to view
   the relevant tickets, and confirm the OAuth client (Setup → OAuth
   clients) has the **Password** grant type enabled.

2. **A reachable SQL Server instance.** Set `ConnectionStrings:DashboardDatabase`
   (in `appsettings.json` or an environment-specific override) and apply
   migrations:

   ```
   dotnet ef database update --project DashBoard
   ```

3. **Configure secrets.** `appsettings.json` and `appsettings.Development.json`
   have `GLPI:ClientId` / `GLPI:ClientSecret` / `GLPI:Username` / `GLPI:Password`
   entries so the required shape is visible — but **never put real values
   directly in those files**, even though they're currently gitignored
   (`Dashboard/appsettings.*` — note this pattern's casing doesn't match the
   actual `DashBoard/` folder; it only works today because Windows/git are
   case-insensitive here by default, so treat it as fragile, not a guarantee).
   Fill in the four values via `dotnet user-secrets` instead, which stores
   them outside the repo entirely:

   ```
   dotnet user-secrets set "GLPI:ClientId" "..." --project DashBoard
   dotnet user-secrets set "GLPI:ClientSecret" "..." --project DashBoard
   dotnet user-secrets set "GLPI:Username" "..." --project DashBoard
   dotnet user-secrets set "GLPI:Password" "..." --project DashBoard
   ```

   `GLPI:ApiBaseUrl` and `GLPI:TokenUrl` are already set in `appsettings.json`.

4. **Run the app**:

   ```
   dotnet run --project DashBoard
   ```

5. The background service syncs 10 seconds after startup and every 5 minutes
   after that; `POST /sync` triggers the same sync immediately. `GET /tickets`
   returns whatever is in the database at that point (empty until the first
   sync completes). If sync fails, check that the OAuth client has the
   `password` grant enabled and that the configured GLPI account can
   actually view tickets.

## Running

```
dotnet run --project DashBoard
```

Swagger UI is available in development at `/swagger`.

## Running the web dashboard

`web/` is a React + Vite + TypeScript SPA — no login, open to anyone, reads
the API above.

```
cd web
npm install
npm run dev
```

Opens at `http://localhost:5173`. Its dev server proxies `/tickets`, `/total`,
and `/sync` straight through to `http://localhost:5276` (see `vite.config.ts`),
so the backend needs to already be running and there's no CORS setup needed
in `Program.cs`. `npm run build` produces a static `dist/` (not yet wired up
to be served by the API — today the two run as separate processes).

## Changelog

### Switched from `authorization_code` + `refresh_token` to `client_credentials`

The app originally authenticated as a specific GLPI *user*: a human visited
`/auth/glpi/login`, logged into GLPI, and consented once via the
`authorization_code` grant. That produced a `refresh_token`, which was
persisted to `DashBoard/App_Data/glpi-token.json` and exchanged for a new
`access_token` (and a newly-rotated `refresh_token`) on every `GET /tickets`
call, guarded by a semaphore so two concurrent requests couldn't both try to
spend the same refresh token.

That flow needed a person to be present at least once, and to be present
again any time GLPI revoked or expired the stored refresh token — not
practical for a service that's meant to answer `GET /tickets` unattended.
Since this dashboard doesn't need to act *as* any particular person, it was
switched to the OAuth2 `client_credentials` grant instead, a machine-to-machine
flow where the client authenticates as itself.

**What changed, file by file:**

- **`DashBoard/Service/GLPIService.cs`**
  - Removed: `GetAuthorizationUrl()`, `ExchangeAuthorizationCodeAsync(code)`,
    `ReadStoredRefreshTokenAsync()`, `WriteStoredRefreshTokenAsync(...)`, the
    `StoredToken` record, and the `_tokenFilePath` / `IWebHostEnvironment`
    dependency used to locate `App_Data/glpi-token.json`.
  - Added: `GetAccessTokenAsync()`, which POSTs
    `{ grant_type: "client_credentials", client_id, client_secret, scope: "api" }`
    to `GLPI:TokenUrl` and caches the returned `access_token` **in memory**
    (a static field, not a file) until ~30 seconds before its `expires_in`
    elapses, then transparently fetches a new one.
  - The `SemaphoreSlim` (renamed `_refreshLock` → `_tokenLock`) now guards
    against firing duplicate token requests when several calls land after the
    cached token has expired, rather than guarding refresh-token rotation
    (`client_credentials` responses carry no `refresh_token` at all — there's
    nothing to rotate).
  - `GetTicketsAsync()` itself is unchanged aside from calling
    `GetAccessTokenAsync()` instead of the old `GetFreshAccessTokenAsync()`.

- **`DashBoard/Service/IGLPIService.cs`** — interface shrunk to just
  `GetTicketsAsync()`; `GetAuthorizationUrl()` and
  `ExchangeAuthorizationCodeAsync(code)` are gone since there's no
  authorization step to expose anymore.

- **`DashBoard/Controllers/GlpiAuthController.cs`** — **deleted**. Its two
  endpoints (`GET /auth/glpi/login`, `GET /auth/glpi/callback`) only existed
  to drive the `authorization_code` consent flow, which no longer exists.

- **`DashBoard/Service/GlpiNotAuthorizedException.cs`** — **deleted**. It
  represented "no refresh token stored yet / GLPI rejected the stored one" —
  a state that can't occur under `client_credentials`, since there's no
  stored, revocable, per-user token to be missing.

- **`DashBoard/Controllers/DashboardController.cs`** — `GET /tickets` no
  longer catches `GlpiNotAuthorizedException` to return a `401` pointing at
  `/auth/glpi/login`. A token/config failure now just surfaces as an
  unhandled exception (`500`), which is the right shape for what's now a
  config/permissions problem on the service account, not something a
  particular caller can fix by logging in.

- **`DashBoard/appsettings.json` / `appsettings.Development.json`** —
  removed `GLPI:RedirectUri` and `GLPI:AuthorizationUrl` (both were only
  needed for the browser-redirect leg of `authorization_code`).
  `GLPI:ClientId`, `GLPI:ClientSecret`, `GLPI:ApiBaseUrl`, and
  `GLPI:TokenUrl` are unchanged and still used.

- **`.gitignore`** — removed the `DashBoard/App_Data/` entry (added
  specifically to keep the now-nonexistent `glpi-token.json` out of git).

- **`DashBoard/body.json`** — **deleted**. This was an untracked scratch
  file holding a raw `client_credentials` token-request payload (same
  `client_id`/`client_secret` as `appsettings.json`) used to manually verify
  the grant against GLPI before it was wired into the service. It wasn't
  covered by any `.gitignore` rule, so it was a live-secret-in-repo risk if
  anyone ever ran `git add -A`.

**Practical effect:** no more `/auth/glpi/login` bootstrap step, no more
`App_Data/glpi-token.json` file, no more `401` from `/tickets` pointing at a
login URL. `GET /tickets` works the moment the app starts, provided the GLPI
OAuth client has the **Client credentials** grant enabled and is linked to a
GLPI user with rights to view tickets.

### Bug fix: `Content-Type` charset broke the token request

While testing the `client_credentials` switch, `GLPIService.GetAccessTokenAsync()`
was getting back `{"error":"unsupported_grant_type", ...}` from GLPI even
though the same JSON payload sent manually (outside the app) worked. Root
cause: `StringContent(json, Encoding.UTF8, "application/json")` sets
`Content-Type: application/json; charset=utf-8`, and GLPI's token endpoint
only parses the body as JSON when the header is the bare string
`application/json` — with the `charset` suffix it treats the body as empty,
so `grant_type` reads as missing/unrecognized.

Fixed by building the request without the encoding-inferred charset:

```csharp
request.Content = new StringContent(JsonSerializer.Serialize(payload));
request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
```

After the fix, a request against the real GLPI instance correctly reached
grant-type validation and returned `unauthorized_client` (the OAuth client
doesn't have `client_credentials` enabled yet on the GLPI side) instead of
`unsupported_grant_type` — confirming the app-side request is now correct
and the only remaining blocker is the GLPI-side setup step above.

### `client_credentials` turned out to be unsupported — switched to `password`

After the `client_credentials` grant was enabled on the OAuth client, token
requests started succeeding — a valid JWT came back with `scopes: ["api"]`.
But every call to `GET /Assistance/Ticket` using that token still failed
with `401 ERROR_UNAUTHENTICATED` ("Authorization header is missing or
invalid"), including when replayed manually outside the app.

Decoding the JWT explained why: its `sub` (subject) claim was the OAuth
client's own ID, not a GLPI user — `client_credentials` has no login step,
so there's no user identity to attach. This GLPI instance's admin UI has no
field to link an OAuth client to a user for that grant (checked directly:
Name / Client ID / Client Secret / Grants / Scopes / redirect URIs / IP
restrictions — no such field). A user-scoped route like `Assistance/Ticket`
has no user to authorize against, hence the rejection.

This was confirmed definitively by fetching the instance's own OpenAPI
schema (`GET /api.php/doc.json`) and inspecting `components.securitySchemes`:
it declares exactly two valid OAuth2 flows — `authorizationCode` and
`password`. **`clientCredentials` isn't declared at all**, even though the
OAuth client admin screen lets you enable it as a "grant" — the flow is
simply never wired up to authenticate against these routes.

A personal per-user API token (regenerated from the GLPI user's profile)
was tried too, as a non-OAuth alternative. It also failed, with a *different*
error (`ERROR_INVALID_PARAMETER: "The JWT string must have two dots"`) —
this API's `Authorization` header parser unconditionally treats whatever
it's given as an OAuth JWT and tries to decode it as one. Personal tokens
belong to GLPI's older, separate `apirest.php` API, not this one.

That left `password` as the only grant, of the two GLPI actually supports
here, that doesn't require an interactive browser session — so
`GLPIService.GetAccessTokenAsync()` was switched to send
`{ grant_type: "password", client_id, client_secret, username, password, scope: "api" }`,
and `GLPI:Username` / `GLPI:Password` were added alongside `ClientId` /
`ClientSecret` in configuration. This was verified end-to-end against the
real GLPI instance: `GET /tickets` returned `200 OK` with real ticket data
(names, statuses, team members all populated correctly).

### Pagination: `GET /tickets` was silently returning only 100 of 830 tickets

Once auth worked, the live test above returned exactly 100 tickets — a
suspiciously round number for what's supposed to be a full ticket list.
Inspecting GLPI's raw response confirmed it: `206 Partial Content` with
`Content-Range: 0-99/830`. GLPI's `Assistance/Ticket` endpoint paginates at
100 items per page by default, and `GetTicketsAsync()` was just returning
whatever single page came back — silently dropping the other 730 tickets,
which would have badly skewed any KPI built on top of it.

The standard HTTP `Range` request header turned out to be ignored entirely
by this endpoint (tried `Range: 100-199` — got the same first page back
every time). The OpenAPI schema showed the actual mechanism: `start` and
`limit` query parameters (`limit` defaults to 100). Passing a single large
`limit` (e.g. `1000`) does work on this instance today, but that's a ticking
time bomb against future ticket growth or a lowered instance-side cap, so
`GetTicketsAsync()` was changed to loop instead: request pages of 500 via
`?start={n}&limit=500`, read the real total off the `Content-Range` header
after each page, and keep going until every ticket is collected.

Verified against the real instance: `GET /tickets` now returns all **830**
tickets, all unique IDs, matching GLPI's own reported total exactly, in
~2.2 seconds (2 page requests).

### Added the web dashboard, and a per-person ticket breakdown

The API had no frontend at all before this — `web/` (React + Vite + TS, see
"Running the web dashboard" above) is the first one, deliberately with no
login, matching the backend, which has never had auth on it either.

**New backend endpoint**: `GET /tickets/users/totaldetails`
(`TicketRepository.GetSummaryByUserAsync()`) groups tickets by
`AssignedUserId` in one SQL query (conditional `COUNT(CASE WHEN ...)` per
status, not N+1 per-user queries) and returns a `UserTicketSummary` per
person. That's a new model, not a reuse of `DashboardSummary`: every
existing `DashboardSummary` consumer has exactly one implicit scope (the
grand total, or the one `userId` already in its URL), so it has no identity
field. This endpoint returns a *list* — every row needs its own
`UserId`/`UserName` to tell people apart — and it adds `Other` (`Total` minus
the five tracked statuses, as a computed property), which the other two
`DashboardSummary` endpoints don't need and shouldn't have to carry just
because this one does.

`Other` exists because `Total` and the five named status counts don't
actually agree — GLPI has ticket statuses (e.g. id `3`, between `Processing`
and `Pending`) this app has never tracked (see the stringly-typed-status note
in `CLAUDE.md`). Without `Other`, a per-person stacked bar's segments
wouldn't add up to the labeled total, which is worse than not showing the gap
at all.

**Frontend** (`web/src/features/dashboard/TeamBreakdown.tsx`): a horizontal
stacked bar per person, one person per row (not several people side by side —
an earlier version's CSS put each person's name/bar/total into a 3-column
*grid* on the outer container while each person was already its own wrapper
`div`, so the grid placed three people's wrapper-divs across the row instead
of one; fixed by making the outer container a plain vertical flex stack and
giving each person's own row its internal 3-column layout instead), capped to
the top 10 by ticket count (all of them are one Ctrl-click away in "View as
table", which is unfiltered). Colored with an ordinal ramp — one hue,
monotone lightness — because ticket status is a lifecycle sequence
(New → Closed), not an arbitrary category; validated light/dark variants
against `--color-surface` with the dataviz skill's `validate_palette.js
--ordinal`. The first validated version's adjacent steps (e.g. Solved vs.
Closed) were too close together to tell apart at the size a stacked-bar
segment actually renders at, so the ramp was re-stepped wider (still
validated) and legend swatches got a 1px border ring so the palest step
doesn't disappear against a dark surface.
