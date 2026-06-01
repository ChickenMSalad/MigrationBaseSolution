import { useEffect, useState } from 'react';
import { commandCenterApi } from '../api/commandCenterApi';
import type { CommandCenterHealthCheck, CommandCenterSummary } from '../types/commandCenter';

type LoadState = {
  loading: boolean;
  summary?: CommandCenterSummary;
  checks: CommandCenterHealthCheck[];
  error?: string;
};

function formatDate(value?: string | null): string {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
}

function statusClass(value?: string | null): string {
  const status = String(value ?? '').toLowerCase();
  if (status.includes('healthy') || status.includes('active') || status.includes('running')) {
    return 'status-success';
  }

  if (status.includes('degraded') || status.includes('failed') || status.includes('critical')) {
    return 'status-danger';
  }

  if (status.includes('warning') || status.includes('stale')) {
    return 'status-warning';
  }

  return 'status-neutral';
}

function metric(value: number | undefined): number {
  return value ?? 0;
}

export function CommandCenter() {
  const [state, setState] = useState<LoadState>({ loading: true, checks: [] });

  async function loadCommandCenter() {
    setState((current) => ({ ...current, loading: true, error: undefined }));

    try {
      const [summary, health] = await Promise.all([
        commandCenterApi.getSummary(),
        commandCenterApi.getHealth(),
      ]);

      setState({
        loading: false,
        summary,
        checks: health.checks ?? [],
      });
    } catch (error) {
      setState({
        loading: false,
        checks: [],
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  useEffect(() => {
    void loadCommandCenter();
  }, []);

  const summary = state.summary;
  const events = summary?.recentEvents ?? [];

  return (
    <section className="page-stack">
      <header className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Command Center</h1>
          <p className="page-subtitle">
            Consolidated operational runtime health, queue, worker, notification, and recent-event visibility.
          </p>
        </div>
        <button className="button-primary" type="button" onClick={() => void loadCommandCenter()}>
          Refresh
        </button>
      </header>

      {state.error ? <div className="error-banner">{state.error}</div> : null}
      {state.loading ? <div className="card">Loading command center summary...</div> : null}

      <div className="card-grid metrics-grid">
        <article className="metric-card">
          <span>Runtime status</span>
          <strong className={statusClass(summary?.runtimeStatus)}>{summary?.runtimeStatus ?? 'Unknown'}</strong>
        </article>
        <article className="metric-card">
          <span>Active runs</span>
          <strong>{metric(summary?.activeRuns)}</strong>
        </article>
        <article className="metric-card">
          <span>Queued work</span>
          <strong>{metric(summary?.queuedWorkItems)}</strong>
        </article>
        <article className="metric-card">
          <span>Failed work</span>
          <strong>{metric(summary?.failedWorkItems)}</strong>
        </article>
        <article className="metric-card">
          <span>Retry pending</span>
          <strong>{metric(summary?.retryPendingWorkItems)}</strong>
        </article>
        <article className="metric-card">
          <span>Active workers</span>
          <strong>{metric(summary?.activeWorkers)}</strong>
        </article>
        <article className="metric-card">
          <span>Stale workers</span>
          <strong>{metric(summary?.staleWorkers)}</strong>
        </article>
        <article className="metric-card">
          <span>Critical alerts</span>
          <strong>{metric(summary?.criticalAlerts)}</strong>
        </article>
      </div>

      <section className="card">
        <h2>Health checks</h2>
        {state.checks.length === 0 ? (
          <p>No command-center health checks are available yet.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {state.checks.map((check) => (
                <tr key={check.name}>
                  <td>{check.name}</td>
                  <td className={statusClass(check.status)}>{check.status}</td>
                  <td>{check.message ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="card">
        <h2>Recent events</h2>
        {events.length === 0 ? (
          <p>No recent command-center events are available yet.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Created</th>
                <th>Severity</th>
                <th>Category</th>
                <th>Title</th>
                <th>Source</th>
              </tr>
            </thead>
            <tbody>
              {events.map((event, index) => (
                <tr key={String(event.eventId ?? index)}>
                  <td>{formatDate(event.createdUtc)}</td>
                  <td className={statusClass(event.severity)}>{event.severity ?? '-'}</td>
                  <td>{event.category ?? '-'}</td>
                  <td>{event.title ?? event.message ?? '-'}</td>
                  <td>{event.source ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {summary?.generatedUtc ? <p className="muted">Generated {formatDate(summary.generatedUtc)}</p> : null}
    </section>
  );
}
