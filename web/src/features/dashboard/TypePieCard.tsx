import { useTypeSummary } from './api/dashboard-queries';
import type { DateRange } from './api/dashboard-api';
import { PieChart, type PieSlice } from './PieChart';
import './pie-chart.css';

interface TypePieCardProps {
  dateFrom?: string | null;
  dateTo?: string | null;
  enabled?: boolean;
}

export function TypePieCard({ dateFrom, dateTo, enabled = true }: TypePieCardProps = {}) {
  const range: DateRange = {};
  if (dateFrom) range.dateFrom = dateFrom;
  if (dateTo) range.dateTo = dateTo;

  const { data, isLoading, isError } = useTypeSummary(range, { enabled });

  const slices: PieSlice[] = data
    ? [
        { key: 'requestOpen', label: 'Request — Open', value: data.requestOpen, color: 'var(--type-request-open)' },
        { key: 'requestClosed', label: 'Request — Closed', value: data.requestClosed, color: 'var(--type-request-closed)' },
        { key: 'incidentOpen', label: 'Incident — Open', value: data.incidentOpen, color: 'var(--type-incident-open)' },
        { key: 'incidentClosed', label: 'Incident — Closed', value: data.incidentClosed, color: 'var(--type-incident-closed)' },
      ]
    : [];

  return (
    <section className="card pie-card">
      <div className="pie-card-header">
        <h2>Tickets by type</h2>
      </div>
      {isError && <p className="status-line" data-state="error">Could not load type breakdown.</p>}
      {isLoading && <p className="status-line" data-state="checking">Loading…</p>}
      {!isLoading && !isError && data && <PieChart slices={slices} />}
    </section>
  );
}
