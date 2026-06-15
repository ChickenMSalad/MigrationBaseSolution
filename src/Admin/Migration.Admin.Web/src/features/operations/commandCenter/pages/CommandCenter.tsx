import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
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

  if (status.includes('degraded') || status.includes('failed') || status.includes('critical') || status.includes('error')) {
    return 'status-danger';
  }

  if (status.includes('warning') || status.includes('stale') || status.includes('queued')) {
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
    const timer = window.setInterval(() => {
      void loadCommandCenter();
    }, 10000);

    return () => window.clearInterval(timer);
  }, []);

  const summary = state.summary;
  const events = useMemo(() => summary?.recentEvents ?? [], [summary?.recentEvents]);

  return (
    <main className="page-shell">
      <div className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Command Center</h1>
          <p>Live SQL runtime summary across runs, queue pressure, failures, workers, readiness, and recent activity.</p>
        </div>
        <button className="button button-secondary" type="button" onClick={() => void loadCommandCenter()} disabled={state.loading}>
          Refresh
        </button>
      </div>

      {state.error ? <div className="alert alert-danger">{state.error}</div> : null}
      {state.loading ? <div className="panel">Loading command center summary...</div> : null}

      <section className="metric-grid">
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
          <span>Dispatched work</span>
          <strong>{metric(summary?.dispatchedWorkItems)}</strong>
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
          <span>Critical alerts</span>
          <strong>{metric(summary?.criticalAlerts)}</strong>
        </article>
      </section>

      <section className="panel-grid two-column">
        <article className="panel">
          <div className="section-header">
            <div>
              <h2>Operational readiness</h2>
              <p>SQL backbone and runtime table checks.</p>
            </div>
            <Link to="/operations/preflight">Open Preflight</Link>
          </div>

          {state.checks.length === 0 ? (
            <p>No command-center health checks are available yet.</p>
          ) : (
            <table className="data-table compact">
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
                    <td><span className={statusClass(check.status)}>{check.status}</span></td>
                    <td>{check.message ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </article>

        <article className="panel">
          <div className="section-header">
            <div>
              <h2>Quick links</h2>
              <p>Open the operational pages that now use SQL runtime truth.</p>
            </div>
          </div>
          <div className="action-list">
            <Link to="/operations/runtime-dashboard">Runtime Dashboard</Link>
            <Link to="/operations/runs">Runs</Link>
            <Link to="/operations/failure-retry">Failure Retry</Link>
            <Link to="/operations/operational-events">Operational Events</Link>
            <Link to="/operations/execution-sessions">Execution Sessions</Link>
          </div>
        </article>
      </section>

      <section className="panel">
        <div className="section-header">
          <div>
            <h2>Recent runtime activity</h2>
            <p>Derived from operational SQL run and work item state.</p>
          </div>
          <Link to="/operations/operational-events">Open Events</Link>
        </div>

        {events.length === 0 ? (
          <p>No recent command-center events are available yet.</p>
        ) : (
          <table className="data-table compact">
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
                <tr key={`${event.eventId ?? index}-${event.createdUtc ?? 'event'}`}>
                  <td>{formatDate(event.createdUtc)}</td>
                  <td><span className={statusClass(event.severity)}>{event.severity ?? '-'}</span></td>
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
    </main>
  );
}
