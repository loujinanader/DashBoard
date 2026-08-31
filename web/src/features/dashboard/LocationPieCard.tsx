import { useMemo } from 'react';
import { useLocationSummaries } from './api/dashboard-queries';
import type { DateRange } from './api/dashboard-api';
import { PieChart, type PieSlice } from './PieChart';
import './pie-chart.css';

const CATEGORICAL_COLORS = [
  'var(--chart-cat-1)',
  'var(--chart-cat-2)',
  'var(--chart-cat-3)',
  'var(--chart-cat-4)',
  'var(--chart-cat-5)',
  'var(--chart-cat-6)',
  'var(--chart-cat-7)',
  'var(--chart-cat-8)',
];

/** A branch network can run 30+ locations; past this many slices a pie
 * stops being readable, so the long tail folds into one "Other" slice
 * (same idea as TeamBreakdown's top-10 cap). */
const CHART_LIMIT = 8;

interface LocationPieCardProps {
  dateFrom?: string | null;
  dateTo?: string | null;
  enabled?: boolean;
}

export function LocationPieCard({ dateFrom, dateTo, enabled = true }: LocationPieCardProps = {}) {
  const range: DateRange = {};
  if (dateFrom) range.dateFrom = dateFrom;
  if (dateTo) range.dateTo = dateTo;

  const { data, isLoading, isError } = useLocationSummaries(range, { enabled });

  const slices: PieSlice[] = useMemo(() => {
    if (!data) return [];
    const top = data.slice(0, CHART_LIMIT);
    const rest = data.slice(CHART_LIMIT);
    const restTotal = rest.reduce((sum, r) => sum + r.total, 0);
    const result: PieSlice[] = top.map((loc, i) => ({
      key: String(loc.locationId),
      label: loc.locationName,
      value: loc.total,
      color: CATEGORICAL_COLORS[i % CATEGORICAL_COLORS.length],
    }));
    if (restTotal > 0) {
      result.push({ key: 'other', label: 'Other', value: restTotal, color: 'var(--status-other)' });
    }
    return result;
  }, [data]);

  return (
    <section className="card pie-card">
      <div className="pie-card-header">
        <h2>Tickets by location</h2>
      </div>
      {isError && <p className="status-line" data-state="error">Could not load location breakdown.</p>}
      {isLoading && <p className="status-line" data-state="checking">Loading…</p>}
      {!isLoading && !isError && data && <PieChart slices={slices} />}
    </section>
  );
}
