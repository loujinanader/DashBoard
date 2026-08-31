# Feature: Ticket Date-Range Filter (Date From / Date To)

> Handoff plan for implementation. Branch `feature/date-range-filter` has
> already been created off `master` — do the work described below on this
> branch.

## Context

The dashboard currently has no way to narrow tickets, totals, or the per-person
breakdown to a time window — the only filter today is by status. The goal is
a "Date From" / "Date To" filter across the dashboard, filtered on ticket
**creation date**, applied everywhere (main list/totals *and* the per-person
team breakdown).

Investigation surfaced a blocking gap: **no date field exists anywhere in the
current pipeline.** `TicketEntity` has no date columns, and the GLPI `Ticket`
model doesn't map any of GLPI's date fields — they're silently discarded
during JSON deserialization today. A `CreatedAt`/`UpdatedAt` pair was briefly
added and then dropped in the very next migration
(`20260826094646_RemoveCreatedAndUpdatedAt.cs`), but confirmed via the sync
code that those columns were never actually populated from GLPI — dead-column
cleanup, not a decision to avoid date tracking. So this feature needs a small
schema change (new nullable `CreatedAt` column, actually populated this time)
in addition to the query/UI work.

## Known risk — MUST be tested, not just assumed

GLPI's official API v2 docs (the "high-level" API this app already uses —
same `Assistance/Ticket`-style resource routing, confirmed at
https://help.glpi-project.org/tutorials/readme-1/api-v2) confirm the ticket
resource's creation-date field is named **`date_creation`**, alongside
`date_mod`, `date_solve`, `date_close`. On writes GLPI expects
`"YYYY-MM-DD HH:MM:SS"`; on reads it returns full ISO 8601
(e.g. `"2026-01-09T07:31:01+00:00"`) — .NET's default `DateTime?` JSON
parsing handles ISO 8601 natively, so no extra converter is needed.

This reduces but does not eliminate the risk: **the target GLPI instance is
not on the latest version**, and nothing in this repo today references any
GLPI date field to confirm the exact key on *this specific* instance/version.
`GlpiBroker.cs` deserializes with only `PropertyNameCaseInsensitive = true`
(no strict/extension-data validation), so if the field name or shape differs
on this instance, the result is **silent nulls, not an error** — the feature
would look fully implemented while quietly filtering nothing.

This is not optional to check. Required test, done as its own step before
moving on to the repository/controller/frontend work: temporarily log the raw
JSON response in `GlpiBroker.cs` (right after it's read), run one `POST
/sync`, inspect a real ticket object in the log for the actual creation-date
key and format GLPI returns on this instance, then remove the temporary log
line. If `date_creation` turns out wrong, correct the `[JsonPropertyName]` in
`Ticket.cs` before proceeding — isolated to one file, nothing downstream
needs to change. Do not consider step 1 (GLPI model) done until this has been
confirmed against a live response from this instance, and do not consider the
whole feature done until "Verification" step 2 below (`SELECT ... CreatedAt`)
confirms real, non-null dates are actually landing in the database after a
sync.

## Backend changes

**`DashBoard/Models/Glpi/Ticket.cs`** — add:
```csharp
[JsonPropertyName("date_creation")]
public DateTime? DateCreation { get; set; }
```

**`DashBoard/Models/Database/TicketEntity.cs`** — add:
```csharp
public DateTime? CreatedAt { get; set; }
```
Then run:
```
dotnet ef migrations add AddTicketCreatedAt --project DashBoard
dotnet ef database update --project DashBoard
```
Nullable `DateTime?`/`datetime2` matches the type EF generated last time
(confirmed via the dropped migration's `Down()`), protecting against rows
synced before this change or a still-wrong GLPI field-name guess.

**`DashBoard/Service/GlpiServices/GLPIService.cs`** (`SyncTicketsAsync`, the
`TicketEntity` initializer) — add `CreatedAt = ticket.DateCreation,`.

**`DashBoard/Repository/TicketRepository.cs`** (`UpsertAsync`) — add
`existingTicket.CreatedAt = ticket.CreatedAt;` to the update block. Since
`UpsertAsync` overwrites all mutable fields on every sync, existing rows
self-heal on the next sync cycle — no backfill needed.

**`ITicketRepository` / `TicketRepository`** — add trailing optional
`DateTime? from = null, DateTime? to = null` params (interface and
implementation both, since callers go through the interface) to:
`GetAllAsync`, `GetByStatusIdAsync`, `GetTotalAsync`, `GetCountByStatusAsync`,
`GetSummaryByUserAsync`. Leave `GetByIdAsync`, `GetByUserIdAsync`,
`GetCountByUserIdAsync`, `GetCountByStatusAndUserIdAsync` unchanged (no
frontend caller uses per-user detail endpoints today). Add a private helper
in `TicketRepository.cs` and apply it in each modified method before/around
the existing `Where`:
```csharp
private static IQueryable<TicketEntity> WithDateRange(IQueryable<TicketEntity> query, DateTime? from, DateTime? to)
{
    if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value.Date);
    if (to.HasValue) query = query.Where(t => t.CreatedAt < to.Value.Date.AddDays(1));
    return query;
}
```
(Exclusive upper bound makes `to` inclusive of the whole day without
`DateTime` precision edge cases.)

**`IDashboardServices` / `DashboardService`** — mirror the same optional
params through: `GetTicketsAsync`, `GetTicketsByStatusIdAsync`,
`GetTotalAsync` (→ private `CreateSummary(from, to)`, which passes them into
all 6 repository calls), `GetSummaryByAllUsersAsync`. Leave
`GetTicketByIdAsync`, `GetTicketsByUserIdAsync`, `GetTotalByUserIdAsync`
unchanged. Also add `DateCreation = t.CreatedAt` to all 4 `new Ticket { ... }`
mapping blocks in this file for consistency, so the frontend can display/
verify the date and so the DTO isn't inconsistently populated across
endpoints.

**`DashBoard/Controllers/DashboardController.cs`** — add
`[FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo` to
`GetTickets`, `GetTotal`, `GetTicketsByStatusId`, and
`GetTotalTicketsByAllUsers`. Use `DateOnly?` (not `DateTime`/`string`) since
it binds automatically from a query string and matches the `yyyy-MM-dd`
format native `<input type="date">` emits, with no culture ambiguity. Convert
at the controller boundary: `var from = dateFrom?.ToDateTime(TimeOnly.MinValue);`
(same for `to`) before calling the service. No `[ApiController]` attribute
exists today and none should be added just for this — a malformed date query
param silently binds to `null` (treated as unbounded), consistent with this
controller's existing zero-validation style, and the app's own date inputs
can't produce malformed values anyway.

## Frontend changes

**`web/src/shared/api/http-client.ts`** — add a small query-string helper:
```ts
export function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value));
  }
  const qs = search.toString();
  return qs ? `?${qs}` : '';
}
```

**`web/src/features/dashboard/api/dashboard-api.ts`** — add
`export interface DateRange { dateFrom?: string; dateTo?: string }` and give
`getSummary`, `getTickets`, `getTicketsByStatus`, `getUserSummaries` a
`range: DateRange = {}` param, appending `buildQuery(range)` to each path, so
status and date filters compose into one request. Optionally add
`dateCreation?: string | null;` to the `Ticket` interface to mirror the new
DTO field.

**`web/src/features/dashboard/api/dashboard-queries.ts`** — incorporate the
range into every query key (`summaryKey`, `ticketsKey`, `userSummariesKey`)
so caching/refetching is correct per filter combination. `useDashboardSummary`,
`useUserSummaries`, and `useTickets` all take a `range` argument and pass it
through. `useUserSummaries` moves from 0-arg to 1-arg (its only caller,
`TeamBreakdown`, is updated below). `useSyncTickets`'s prefix invalidation
(`['dashboard']`) needs no change.

**`web/src/features/dashboard/DashboardPage.tsx`** — add
`dateFrom`/`dateTo` state (`useState<string | null>(null)` each, same pattern
as the existing `statusFilter`) alongside it. Validate
`dateFrom > dateTo` (ISO strings compare correctly) and show an inline
`status-line` error, disabling the queries while invalid (pass `enabled` into
the query hooks). Add two `<input type="date" className="input">` controls to
the existing `.dashboard-tickets-header` toolbar next to the status
`<select>`. Pass `dateFrom`/`dateTo` into `useDashboardSummary`, `useTickets`,
and down as props to `<TeamBreakdown dateFrom={dateFrom} dateTo={dateTo} />`.

**`web/src/features/dashboard/TeamBreakdown.tsx`** — convert from
self-fetching to prop-driven: accept `{ dateFrom, dateTo }` props and pass
them into `useUserSummaries`.

**`web/src/styles/primitives.css`** — widen the existing `.select` /
`.select:focus` selectors to also match `.input` (native date inputs need the
identical border/radius/padding/focus-ring treatment; no separate rule block
needed).

**`web/vite.config.ts`** — no change needed. The proxy matches by path
prefix; query strings pass through unaffected, and `/tickets/users/totaldetails`
already falls under the existing `/tickets` entry.

## Verification (manual — no automated test suite in this repo)

1. `dotnet ef database update --project DashBoard`, then
   `dotnet run --project DashBoard`.
2. Trigger `POST /sync`, then check `SELECT TOP 5 Id, CreatedAt FROM Tickets
   ORDER BY Id` to confirm `CreatedAt` actually populates — this validates
   (or invalidates) the `date_creation` field-name guess.
3. `npm run dev` in `web/`, open the dashboard.
4. Confirm both date inputs render in the toolbar; pick a range covering only
   some tickets (informed by step 2's data) and confirm stat cards, the
   ticket table, and the "Tickets by person" breakdown all narrow
   consistently; clear the range and confirm totals return to full.
5. Set `dateFrom` after `dateTo` and confirm the inline validation error
   appears with no new network request firing.
6. Combine the status filter with a date range and confirm the request URL
   carries both the status path segment and `dateFrom`/`dateTo` query params.
7. Restart the backend with a range still selected to confirm nothing throws
   when `CreatedAt` is null for some rows.

## Critical files

- `DashBoard/Models/Glpi/Ticket.cs`
- `DashBoard/Models/Database/TicketEntity.cs`
- `DashBoard/Repository/ITicketRepository.cs`, `TicketRepository.cs`
- `DashBoard/Service/GlpiServices/GLPIService.cs`
- `DashBoard/Service/DashboardServices/IDashboardServices.cs`, `DashboardServices.cs`
- `DashBoard/Controllers/DashboardController.cs`
- `web/src/shared/api/http-client.ts`
- `web/src/features/dashboard/api/dashboard-api.ts`, `dashboard-queries.ts`
- `web/src/features/dashboard/DashboardPage.tsx`, `TeamBreakdown.tsx`
- `web/src/styles/primitives.css`
