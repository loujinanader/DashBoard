import { useState } from 'react';
import { useTheme } from './theme-context';
import { useSyncTickets } from '@/features/dashboard/api/dashboard-queries';
import { DashboardPage } from '@/features/dashboard/DashboardPage';
import './app.css';

function SunIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
    </svg>
  );
}

export default function App() {
  const { effectiveTheme, toggle } = useTheme();
  const sync = useSyncTickets();
  const [syncMessage, setSyncMessage] = useState<string | null>(null);

  const handleSync = () => {
    setSyncMessage(null);
    sync.mutate(undefined, {
      onSuccess: () => setSyncMessage('Sync complete.'),
      onError: (err) => setSyncMessage(err instanceof Error ? err.message : 'Sync failed.'),
    });
  };

  return (
    <div className="app-shell">
      <header className="app-topbar">
        <div className="app-topbar-start">
          <span className="app-brand">IT KPI Dashboard</span>
        </div>
        <div className="app-topbar-end">
          {syncMessage && (
            <span className="status-line" data-state={sync.isError ? 'error' : 'ok'}>
              {syncMessage}
            </span>
          )}
          <button type="button" className="btn btn-primary btn-sm" onClick={handleSync} disabled={sync.isPending}>
            {sync.isPending ? 'Syncing…' : 'Sync now'}
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-icon"
            onClick={toggle}
            aria-label={effectiveTheme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
            title={effectiveTheme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
          >
            {effectiveTheme === 'dark' ? <SunIcon /> : <MoonIcon />}
          </button>
        </div>
      </header>
      <main className="app-main">
        <DashboardPage />
      </main>
    </div>
  );
}
