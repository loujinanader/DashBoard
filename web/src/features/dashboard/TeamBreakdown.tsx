import { useRef, useState } from 'react';
import { useUserSummaries } from './api/dashboard-queries';
import type { DateRange, UserTicketSummary } from './api/dashboard-api';
import { STATUS_SEGMENTS as SEGMENTS } from './status-segments';
import './team-breakdown.css';

interface TooltipState {
  x: number;
  y: number;
  text: string;
}

function personLabel(person: UserTicketSummary): string {
  return person.userName ?? `User ${person.userId}`;
}

/** Chart stays readable as a scan of "who's busiest"; the table view (all
 * people, already sorted by total from the API) is the full-data escape
 * hatch, same as the ticket table below it. */
const CHART_LIMIT = 10;

interface TeamBreakdownProps {
  dateFrom?: string | null;
  dateTo?: string | null;
  enabled?: boolean;
}

export function TeamBreakdown({ dateFrom, dateTo, enabled = true }: TeamBreakdownProps = {}) {
  const range: DateRange = {};
  if (dateFrom) range.dateFrom = dateFrom;
  if (dateTo) range.dateTo = dateTo;

  const { data, isLoading, isError } = useUserSummaries(range, { enabled });
  const [view, setView] = useState<'chart' | 'table'>('chart');
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const showTooltip = (el: HTMLElement, text: string) => {
    const containerRect = containerRef.current?.getBoundingClientRect();
    const rect = el.getBoundingClientRect();
    if (!containerRect) return;
    setTooltip({ x: rect.left + rect.width / 2 - containerRect.left, y: rect.top - containerRect.top, text });
  };

  return (
    <section className="team-breakdown card">
      <div className="team-breakdown-header">
        <h2>Tickets by person{view === 'chart' && data && data.length > CHART_LIMIT ? ` — top ${CHART_LIMIT}` : ''}</h2>
        <button type="button" className="btn btn-ghost btn-sm" onClick={() => setView(view === 'chart' ? 'table' : 'chart')}>
          {view === 'chart' ? 'View as table' : 'View as chart'}
        </button>
      </div>

      {isError && <p className="status-line" data-state="error">Could not load per-person data.</p>}
      {isLoading && <p className="status-line" data-state="checking">Loading…</p>}

      {data && data.length === 0 && <p className="dashboard-empty">No assigned tickets yet.</p>}

      {data && data.length > 0 && view === 'table' && (
        <div className="team-breakdown-table-scroll">
          <table className="table">
            <thead>
              <tr>
                <th>Person</th>
                {SEGMENTS.map((s) => (
                  <th key={s.key}>{s.label}</th>
                ))}
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {data.map((person) => (
                <tr key={person.userId}>
                  <td>{personLabel(person)}</td>
                  {SEGMENTS.map((s) => (
                    <td key={s.key} className="data-mono">{person[s.key] as number}</td>
                  ))}
                  <td className="data-mono">{person.total}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && data.length > 0 && view === 'chart' && (
        <>
          <div className="team-breakdown-legend">
            {SEGMENTS.map((s) => (
              <span className="legend-item" key={s.key}>
                <span className="legend-swatch" style={{ background: `var(${s.colorVar})` }} />
                {s.label}
              </span>
            ))}
          </div>
          <div className="team-breakdown-rows" ref={containerRef}>
            {(() => {
              const topPeople = data.slice(0, CHART_LIMIT);
              const maxTotal = Math.max(...topPeople.map((p) => p.total));
              return topPeople.map((person) => {
                const barWidthPercent = (person.total / maxTotal) * 100;
                const visibleSegments = SEGMENTS.filter((s) => (person[s.key] as number) > 0);
                return (
                  <div className="team-row" key={person.userId}>
                    <span className="team-row-name" title={personLabel(person)}>{personLabel(person)}</span>
                    <div className="team-row-bar-outer">
                      <div className="team-row-bar" style={{ width: `${barWidthPercent}%` }}>
                        {visibleSegments.map((s) => {
                          const count = person[s.key] as number;
                          const text = `${personLabel(person)} — ${s.label}: ${count}`;
                          return (
                            <div
                              key={s.key}
                              className="team-row-segment"
                              tabIndex={0}
                              role="img"
                              aria-label={text}
                              style={{ flexBasis: `${(count / person.total) * 100}%`, background: `var(${s.colorVar})` }}
                              onMouseEnter={(e) => showTooltip(e.currentTarget, text)}
                              onFocus={(e) => showTooltip(e.currentTarget, text)}
                              onMouseLeave={() => setTooltip(null)}
                              onBlur={() => setTooltip(null)}
                            />
                          );
                        })}
                      </div>
                    </div>
                    <span className="team-row-total data-mono">{person.total}</span>
                  </div>
                );
              });
            })()}
            {tooltip && (
              <div className="team-breakdown-tooltip" style={{ left: tooltip.x, top: tooltip.y }}>
                {tooltip.text}
              </div>
            )}
          </div>
        </>
      )}
    </section>
  );
}
