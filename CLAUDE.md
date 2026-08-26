# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet run --project DashBoard              # run the API (http://localhost:5276)
dotnet build                                 # build the solution
dotnet ef migrations add <Name> --project DashBoard   # add a migration after changing TicketEntity/DashboardDbContext
dotnet ef database update --project DashBoard         # apply migrations to the configured SQL Server instance

dotnet user-secrets set "GLPI:ClientId" "..." --project DashBoard
dotnet user-secrets set "GLPI:ClientSecret" "..." --project DashBoard
dotnet user-secrets set "GLPI:Username" "..." --project DashBoard
dotnet user-secrets set "GLPI:Password" "..." --project DashBoard
```

There are no automated tests and no lint/format tooling configured in this repo yet.

## Architecture

This is a single ASP.NET Core (.NET 10) Web API project (`DashBoard/`). The key thing to understand
before touching any endpoint is that **reads and GLPI sync are two separate paths that only meet at
the database** — no controller endpoint calls out to GLPI live.

```
GLPI REST API  <--(OAuth2 password grant)-->  GLPIBroker
                                                   |
                                          GLPIService.SyncTicketsAsync()
                                                   |
                                          TicketRepository.UpsertAsync()  -->  SQL Server (Tickets table)
                                                   ^
                          TicketSyncBackgroundService (every 5 min, 10s startup delay)
                          or POST /sync (same code path, on demand)

DashboardController  -->  DashboardService  -->  TicketRepository  -->  SQL Server (Tickets table)
   (GET /tickets, /total, /tickets/{id}, ...)      (maps TicketEntity -> Ticket/DashboardSummary DTOs)
```

- **Write/sync path**: `GLPIBroker` (`ApiBroker/Glpi/`) is the only thing that talks to GLPI. It
  authenticates via the OAuth2 **`password`** grant (not `client_credentials` — this GLPI instance's
  high-level API doesn't support that grant for user-scoped routes; see `README.md`'s Changelog for
  the full investigation) and pages through `Assistance/Ticket` using `start`/`limit` (500/page) until
  `Content-Range`'s reported total is reached. `GLPIService.SyncTicketsAsync()` maps each GLPI `Ticket`
  into a `TicketEntity` and upserts it via `TicketRepository`. This runs on a timer
  (`TicketSyncBackgroundService`, every 5 minutes) and can also be triggered on demand via `POST /sync`.
- **Read path**: `DashboardController`'s endpoints never touch GLPI. `DashboardService` reads
  `TicketEntity` rows via `ITicketRepository` and reshapes them back into `Models/Glpi/Ticket` /
  `DashboardSummary` DTOs for the API response. This means `GET /tickets` reflects whatever the last
  sync wrote to the DB, not GLPI's live state.
- **Status filtering is stringly-typed**: `DashboardService` and `TicketRepository` filter/group by
  status using literal strings (`"New"`, `"Processing"`, `"Pending"`, `"Solved"`, `"Closed"`) rather
  than the constants in `Models/Glpi/TicketsStatus.cs` (which are currently unused).
- **Per-person breakdown** (`GET /tickets/users/totaldetails`, `TicketRepository.GetSummaryByUserAsync()`)
  groups tickets by `AssignedUserId` and counts the same five statuses, plus `UserTicketSummary.Other`
  (a computed property: `Total` minus the five tracked counts) for tickets in statuses outside that
  set — this exists precisely because `Total` and the five named counts don't always agree (see the
  status-filtering note above). The frontend (`web/src/features/dashboard/TeamBreakdown.tsx`) renders
  this as a horizontal stacked bar per person, ordinal color ramp (`--status-*` tokens in
  `tokens.css`), since ticket lifecycle stage is an ordered sequence, not arbitrary categories.
- **Config vs. secrets**: `GLPI:ApiBaseUrl`, `GLPI:TokenUrl` are plain config in `appsettings.json`.
  `GLPI:ClientId`, `GLPI:ClientSecret`, `GLPI:Username`, `GLPI:Password` are secrets — they must go
  through `dotnet user-secrets` (see Commands above), never committed with real values, even though
  `appsettings.json`/`appsettings.Development.json` carry placeholder-shaped entries so the required
  keys are visible.
- EF Core migrations live in `DashBoard/Migrations/`; `DashboardDbContext` (`Data/`) maps the single
  `Tickets` table (`TicketEntity`: id, name, status id/name, deleted flag, assigned-user id/name — no
  timestamps, no navigation properties).
