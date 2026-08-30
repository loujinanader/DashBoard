export interface StatusSegment {
  key: 'new' | 'processing' | 'pending' | 'solved' | 'closed' | 'other';
  label: string;
  colorVar: string;
}

/** Ordinal ticket-lifecycle order (New -> Closed): position carries meaning,
 * so color is a single hue, monotone lightness (see tokens.css --status-*). */
export const STATUS_SEGMENTS: StatusSegment[] = [
  { key: 'new', label: 'New', colorVar: '--status-new' },
  { key: 'processing', label: 'Processing', colorVar: '--status-processing' },
  { key: 'pending', label: 'Pending', colorVar: '--status-pending' },
  { key: 'solved', label: 'Solved', colorVar: '--status-solved' },
  { key: 'closed', label: 'Closed', colorVar: '--status-closed' },
  { key: 'other', label: 'Other', colorVar: '--status-other' },
];
