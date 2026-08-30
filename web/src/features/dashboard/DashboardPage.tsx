import { useState } from 'react';
import { useDashboardSummary, useTickets } from './api/dashboard-queries';
import { TICKET_STATUSES, type DateRange, type Ticket } from './api/dashboard-api';
import { TeamBreakdown } from './TeamBreakdown';
import './dashboard-page.css';

function StatCard({ label, value, tone }: { label: string; value: number | string; tone?: 'warning' }) {
  return (
    <div className="dashboard-stat-card" data-tone={tone}>
      <span className="dashboard-stat-value">{value}</span>
      <span className="dashboard-stat-label">{label}</span>
    </div>
  );
}

function statusPillTone(statusId: number | undefined): 'accent' | 'warning' | 'success' | 'neutral' {
  switch (statusId) {
    case 1: return 'accent'; // New
    case 2: return 'accent'; // Processing
    case 4: return 'warning'; // Pending
    case 5: return 'success'; // Solved
    case 6: return 'neutral'; // Closed
    default: return 'neutral';
  }
}

function assignedName(ticket: Ticket): string {
  const assigned = ticket.team?.find((m) => m.role === 'assigned') ?? ticket.team?.[0];
  return assigned?.name ?? '—';
}

interface DashboardPageProps {
  dateFrom: string | null;
  dateTo: string | null;
}

export function DashboardPage({ dateFrom, dateTo }: DashboardPageProps) {
  const [statusFilter, setStatusFilter] = useState<number | null>(null);

  const isDateRangeInvalid = dateFrom && dateTo && dateFrom > dateTo;

  const range: DateRange = {};
  if (dateFrom) range.dateFrom = dateFrom;
  if (dateTo) range.dateTo = dateTo;

  const { data: summary, isLoading: summaryLoading, isError: summaryError } = useDashboardSummary(range, { enabled: !isDateRangeInvalid });
  const { data: tickets, isLoading: ticketsLoading, isError: ticketsError } = useTickets(statusFilter, range, { enabled: !isDateRangeInvalid });

  return (
    <section className="dashboard-page">
      {summaryError && (
        <p className="status-line" data-state="error">
          Could not reach the API. Is the backend running on port 5276?
        </p>
      )}

      {summaryLoading ? (
        <p className="status-line" data-state="checking">Loading summary…</p>
      ) : summary ? (
        <div className="dashboard-stat-grid">
          <StatCard label="Total" value={summary.total} />
          <StatCard label="New" value={summary.new} />
          <StatCard label="Processing" value={summary.processing} />
          <StatCard label="Pending" value={summary.pending} tone={summary.pending > 0 ? 'warning' : undefined} />
          <StatCard label="Solved" value={summary.solved} />
          <StatCard label="Closed" value={summary.closed} />
        </div>
      ) : null}

      {isDateRangeInvalid && (
        <p className="status-line" data-state="error">
          Date From must be on or before Date To.
        </p>
      )}

      <TeamBreakdown dateFrom={dateFrom} dateTo={dateTo} enabled={!isDateRangeInvalid} />

      <div className="dashboard-tickets-header">
        <h2>Tickets</h2>
        <select
          className="select"
          value={statusFilter ?? ''}
          onChange={(e) => setStatusFilter(e.target.value === '' ? null : Number(e.target.value))}
        >
          <option value="">All statuses</option>
          {TICKET_STATUSES.map((s) => (
            <option key={s.id} value={s.id}>{s.label}</option>
          ))}
        </select>
      </div>

      {ticketsError && (
        <p className="status-line" data-state="error">Could not load tickets.</p>
      )}

      {ticketsLoading ? (
        <p className="status-line" data-state="checking">Loading tickets…</p>
      ) : !tickets || tickets.length === 0 ? (
        <p className="dashboard-empty">No tickets to show.</p>
      ) : (
        <div className="dashboard-table-scroll">
          <table className="table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Status</th>
                <th>Assigned to</th>
              </tr>
            </thead>
            <tbody>
              {tickets.map((ticket) => (
                <tr key={ticket.id}>
                  <td className="data-mono">{ticket.id}</td>
                  <td>{ticket.name ?? '—'}</td>
                  <td>
                    {ticket.status ? (
                      <span className={`pill pill-${statusPillTone(ticket.status.id)}`}>{ticket.status.name}</span>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td>{assignedName(ticket)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
