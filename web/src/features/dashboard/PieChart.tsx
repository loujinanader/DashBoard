import { useRef, useState } from 'react';
import './pie-chart.css';

export interface PieSlice {
  key: string;
  label: string;
  value: number;
  color: string;
}

interface TooltipState {
  x: number;
  y: number;
  text: string;
}

const SIZE = 160;
const RADIUS = 60;
const STROKE_WIDTH = 28;
const CENTER = SIZE / 2;

interface PieChartProps {
  slices: PieSlice[];
  emptyMessage?: string;
}

/** Donut chart built from stacked SVG circles (stroke-dasharray/dashoffset
 * with pathLength=100 normalizes the math to percentages, no arc-path
 * trigonometry needed). Each slice is its own focusable/hoverable ring
 * segment, mirroring the tooltip pattern in team-breakdown's bar segments. */
export function PieChart({ slices, emptyMessage = 'No tickets to show.' }: PieChartProps) {
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const total = slices.reduce((sum, s) => sum + s.value, 0);
  const visibleSlices = slices.filter((s) => s.value > 0);

  const showTooltip = (el: SVGElement, text: string) => {
    const containerRect = containerRef.current?.getBoundingClientRect();
    const rect = el.getBoundingClientRect();
    if (!containerRect) return;
    setTooltip({ x: rect.left + rect.width / 2 - containerRect.left, y: rect.top - containerRect.top, text });
  };

  if (total === 0) {
    return <p className="dashboard-empty">{emptyMessage}</p>;
  }

  let cumulativePercent = 0;

  return (
    <div className="pie-chart" ref={containerRef}>
      <svg viewBox={`0 0 ${SIZE} ${SIZE}`} width={SIZE} height={SIZE} role="img" aria-label={`Pie chart, ${total} total`}>
        <circle cx={CENTER} cy={CENTER} r={RADIUS} fill="none" stroke="var(--color-surface-sunken)" strokeWidth={STROKE_WIDTH} />
        {visibleSlices.map((slice) => {
          const percent = (slice.value / total) * 100;
          const offset = -cumulativePercent;
          cumulativePercent += percent;
          const pct = Math.round((slice.value / total) * 100);
          const text = `${slice.label}: ${slice.value} (${pct}%)`;
          return (
            <circle
              key={slice.key}
              cx={CENTER}
              cy={CENTER}
              r={RADIUS}
              fill="none"
              stroke={slice.color}
              strokeWidth={STROKE_WIDTH}
              strokeDasharray={`${percent} ${100 - percent}`}
              strokeDashoffset={offset}
              pathLength={100}
              transform={`rotate(-90 ${CENTER} ${CENTER})`}
              tabIndex={0}
              role="img"
              aria-label={text}
              className="pie-chart-slice"
              onMouseEnter={(e) => showTooltip(e.currentTarget, text)}
              onFocus={(e) => showTooltip(e.currentTarget, text)}
              onMouseLeave={() => setTooltip(null)}
              onBlur={() => setTooltip(null)}
            />
          );
        })}
        <text x={CENTER} y={CENTER - 6} textAnchor="middle" className="pie-chart-total-value">{total}</text>
        <text x={CENTER} y={CENTER + 14} textAnchor="middle" className="pie-chart-total-label">total</text>
      </svg>
      <ul className="pie-chart-legend">
        {visibleSlices.map((slice) => (
          <li key={slice.key} className="legend-item">
            <span className="legend-swatch" style={{ background: slice.color }} />
            {slice.label} — {slice.value} ({Math.round((slice.value / total) * 100)}%)
          </li>
        ))}
      </ul>
      {tooltip && (
        <div className="pie-chart-tooltip" style={{ left: tooltip.x, top: tooltip.y }}>
          {tooltip.text}
        </div>
      )}
    </div>
  );
}
