# KPI Dashboard

ASP.NET Core (.NET 10) Web API that pulls IT helpdesk ticket data from a
[GLPI](https://glpi-project.org/) instance to power an IT team KPI dashboard.

## Project layout

- `DashBoard/Controllers/DashboardController.cs` — `GET /tickets`, returns GLPI tickets.
- `DashBoard/Controllers/GlpiAuthController.cs` — one-time GLPI OAuth authorization endpoints.
- `DashBoard/Service/GLPIService.cs` — talks to GLPI's REST API and OAuth2 token endpoint.
- `DashBoard/Models/Glpi/` — `Ticket`, `Status`, `TeamMember`.

## How authentication works

This app authenticates to GLPI's OAuth2 API using the `authorization_code` grant
**once**, to obtain a `refresh_token`. From then on, every `GET /tickets` call
exchanges that refresh token for a brand-new `access_token` via the
`refresh_token` grant — no long-lived access token is cached.

GLPI rotates the refresh token on every use, so the app persists whatever new
refresh token comes back after each exchange, to a local file:
`DashBoard/App_Data/glpi-token.json` (gitignored — it holds a live secret and
must never be committed).

If no refresh token has been stored yet, or GLPI rejects the stored one
(expired/revoked), `GET /tickets` responds `401 Unauthorized` with a JSON body
pointing at the authorize endpoint instead of throwing a raw exception:

```json
{
  "message": "No GLPI refresh token stored yet. Visit /auth/glpi/login once to authorize this app.",
  "authorizeUrl": "/auth/glpi/login"
}
```

## What you need to do before this works

1. **In GLPI** (Setup → OAuth clients, for this app's client):
   - Enable both the **Authorization code** and **Refresh token** grant types.
   - Add this app's callback as an allowed redirect URI, e.g.
     `https://localhost:<port>/auth/glpi/callback` (must match `GLPI:RedirectUri`
     exactly, including scheme, host, and port).

2. **Configure secrets.** `appsettings.json` and `appsettings.Development.json`
   already have placeholder `GLPI:ClientId` / `GLPI:ClientSecret` /
   `GLPI:RedirectUri` entries so the required shape is visible — but they're
   committed to git, so **never put real values directly in those files**.
   Fill them in via `dotnet user-secrets` instead, which stores them outside
   the repo:

   ```
   dotnet user-secrets set "GLPI:ClientId" "..." --project DashBoard
   dotnet user-secrets set "GLPI:ClientSecret" "..." --project DashBoard
   ```

   `GLPI:RedirectUri` already defaults to
   `https://localhost:7002/auth/glpi/callback` in `appsettings.Development.json`
   (matching the `https` launch profile's port). Register that same URL as the
   allowed redirect URI on the GLPI OAuth client. If you run on a different
   port, override `GLPI:RedirectUri` via user-secrets too.

   `GLPI:BaseUrl`, `GLPI:ApiBaseUrl`, `GLPI:AuthorizationUrl`, and
   `GLPI:TokenUrl` are already set in `appsettings.json`.

3. **Run the app**:

   ```
   dotnet run --project DashBoard
   ```

4. **Bootstrap once**: open `/auth/glpi/login` in a browser, log in to GLPI and
   consent. GLPI redirects back to `/auth/glpi/callback`, which stores the first
   refresh token in `DashBoard/App_Data/glpi-token.json`.

5. From then on, `GET /tickets` works on its own — no further manual steps,
   as long as `App_Data/glpi-token.json` isn't deleted and GLPI doesn't revoke
   access. If it ever does (you'll get a `401` from `/tickets`), just repeat
   step 4.

## Running

```
dotnet run --project DashBoard
```

Swagger UI is available in development at `/swagger`.
