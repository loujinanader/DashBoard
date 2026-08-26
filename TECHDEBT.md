# Tech debt / known issues

Findings from a code review pass, kept here so they don't get lost. Nothing
below is urgent enough to block anything — pick them up when there's time.

## Worth fixing soon

1. **Secrets hygiene is fragile, not yet leaked.** `appsettings.json` and
   `appsettings.Development.json` currently hold a real GLPI client secret
   and password on disk, not placeholders. They're kept out of git only by
   `.gitignore`'s `Dashboard/appsettings.*` — note the lowercase `Dashboard`,
   while the real folder is `DashBoard`. That only works today because
   Windows/git default to case-insensitive matching; it's one `git add -A`
   on a case-sensitive setup away from committing real credentials.
   - Fix the casing in `.gitignore`.
   - Move `GLPI:ClientId` / `GLPI:ClientSecret` / `GLPI:Username` /
     `GLPI:Password` into `dotnet user-secrets` (the README has already told
     people to do this since it was written; it just isn't done yet).

2. **Dead code in the GLPI service layer.**
   - `GLPIService.GetTicketsAsync()` / `IGLPIService.GetTicketsAsync()`
     (`DashBoard/Service/GlpiServices/`) are unreachable — the read path
     moved to `TicketRepository` and nothing calls these anymore.
   - `IGLPIBroker.GetAccessTokenAsync()` (`DashBoard/ApiBroker/Glpi/`) is
     only ever used internally by `GLPIBroker.GetTicketsAsync()` — it
     doesn't need to be on the public interface.

3. **Unused config & constants, and magic strings instead.**
   - `GLPI:Department` and `GLPI:ITUserIds` in `appsettings.json` aren't
     read anywhere in the codebase.
   - `Models/Glpi/TicketsStatus.cs`'s constants (`New = 1`, `Processing = 2`,
     `Pending = 4`, `Solved = 5`, `Closed = 6`) are unused. Status
     filtering/grouping instead uses raw string literals (`"New"`,
     `"Processing"`, `"Pending"`, `"Solved"`, `"Closed"`) duplicated across
     `DashboardServices.cs` and `TicketRepository.GetSummaryByUserAsync()`,
     with no validation against GLPI's actual status names — a typo'd
     status name would silently count as zero instead of erroring.
   - This is also *why* `UserTicketSummary.Other` exists: GLPI has a status
     (id `3`, between `Processing` and `Pending`) this app has never
     tracked, so `Total` and the five named counts don't actually agree.

4. **`DashboardSummary`'s doubly-nested namespace.**
   `DashBoard/Models/Dashboard/DashboardSummary.cs` declares
   `namespace DashBoard.Models.Dashboard { namespace DashBoard.Models { ... } }`
   — looks like a copy/paste artifact. Every consumer currently imports it
   via the odd `using DashBoard.Models.Dashboard.DashBoard.Models;`. Worth
   flattening to a single `namespace DashBoard.Models.Dashboard` (matching
   `UserTicketSummary.cs`, which already lives there correctly) — just
   needs the `using` fixed everywhere it's imported.

## Lower priority

- **Sync does one DB round trip per ticket.** `GLPIService.SyncTicketsAsync()`
  calls `TicketRepository.UpsertAsync()` in a loop, each doing its own
  `FirstOrDefaultAsync` lookup before a single `SaveChangesAsync()` at the
  end. Fine at ~830 tickets (current real count), won't scale indefinitely.
  Could batch-load existing IDs once up front instead.
- **No timeout/retry policy on the GLPI `HttpClient`.** Registered via
  plain `AddHttpClient<IGLPIBroker, GLPIBroker>()` in `Program.cs` — a slow
  or hung GLPI instance blocks a sync with only the default ~100s timeout
  and no retry-on-transient-failure.
- **Zero automated tests.** No test project exists at all, despite the
  pagination logic in `GLPIBroker.GetTicketsAsync()` having already had one
  real, silent bug (returning only 100 of 830 tickets — see the README
  changelog) that only manual testing caught.
