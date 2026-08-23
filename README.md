# KPI Dashboard

ASP.NET Core (.NET 10) Web API that pulls IT helpdesk ticket data from a
[GLPI](https://glpi-project.org/) instance to power an IT team KPI dashboard.

## Project layout

- `DashBoard/Controllers/DashboardController.cs` — `GET /tickets`, returns GLPI tickets.
- `DashBoard/Service/GLPIService.cs` — talks to GLPI's REST API and OAuth2 token endpoint.
- `DashBoard/Models/Glpi/` — `Ticket`, `Status`, `TeamMember`.

## How authentication works

This app authenticates to GLPI's OAuth2 API using the `password` grant —
it trades a technical/service GLPI account's username and password directly
for an `access_token`, with no browser or consent step. On the first
`GET /tickets` call (and again whenever the cached token is close to
expiring), `GLPIService` requests a fresh `access_token` from GLPI's token
endpoint and caches it in memory for `expires_in` seconds. There's no
`refresh_token` with this grant and nothing is persisted to disk.

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

2. **Configure secrets.** `appsettings.json` and `appsettings.Development.json`
   already have placeholder `GLPI:ClientId` / `GLPI:ClientSecret` entries so
   the required shape is visible — but they're committed to git, so **never
   put real values directly in those files**. Fill in `ClientId`,
   `ClientSecret`, `Username`, and `Password` via `dotnet user-secrets`
   instead, which stores them outside the repo:

   ```
   dotnet user-secrets set "GLPI:ClientId" "..." --project DashBoard
   dotnet user-secrets set "GLPI:ClientSecret" "..." --project DashBoard
   dotnet user-secrets set "GLPI:Username" "..." --project DashBoard
   dotnet user-secrets set "GLPI:Password" "..." --project DashBoard
   ```

   `GLPI:ApiBaseUrl` and `GLPI:TokenUrl` are already set in `appsettings.json`.

3. **Run the app**:

   ```
   dotnet run --project DashBoard
   ```

4. `GET /tickets` works immediately — no bootstrap step. If it fails, check
   that the OAuth client has the `password` grant enabled and that the
   configured GLPI account can actually view tickets.

## Running

```
dotnet run --project DashBoard
```

Swagger UI is available in development at `/swagger`.

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
