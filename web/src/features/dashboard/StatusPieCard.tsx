import { PieChart, type PieSlice } from './PieChart';
import { STATUS_SEGMENTS, type StatusSegment } from './status-segments';
import type { DashboardSummary } from './api/dashboard-api';
import './pie-chart.css';

interface StatusPieCardProps {
  summary: DashboardSummary | undefined;
  isLoading: boolean;
  isError: boolean;
}

/** Reads the same DashboardSummary the stat cards use (fetched once in
 * DashboardPage) rather than issuing its own request, so it automatically
 * obeys the same date-range gating (enabled: !isDateRangeInvalid). */
export function StatusPieCard({ summary, isLoading, isError }: StatusPieCardProps) {
  const counts: Record<StatusSegment['key'], number> | null = summary
    ? {
        new: summary.new,
        processing: summary.processing,
        pending: summary.pending,
        solved: summary.solved,
        closed: summary.closed,
        other: summary.total - summary.new - summary.processing - summary.pending - summary.solved - summary.closed,
      }
    : null;

  const slices: PieSlice[] = counts
    ? STATUS_SEGMENTS.map((s) => ({
        key: s.key,
        label: s.label,
        value: counts[s.key],
        color: `var(${s.colorVar})`,
      }))
    : [];

  return (
    <section className="card pie-card">
      <div className="pie-card-header">
        <h2>Tickets by status</h2>
      </div>
      {isError && <p className="status-line" data-state="error">Could not load status breakdown.</p>}
      {isLoading && <p className="status-line" data-state="checking">Loading…</p>}
      {!isLoading && !isError && summary && <PieChart slices={slices} />}
    </section>
  );
}
