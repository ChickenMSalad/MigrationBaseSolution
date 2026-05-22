import { useEffect, useMemo, useState } from 'react';
import {
  getOperationalRuntimeDashboard,
  type OperationalRuntimeDashboardModel,
  type OperationalRunSummary
} from '../lib/operationalRuntimeApi';
import { RuntimeStatusBadge } from './RuntimeStatusBadge';

type LoadState = 'idle' | 'loading' | 'loaded' | 'failed';

const emptyModel: OperationalRuntimeDashboardModel = {
  readiness: {
    status: 'unknown',
    message: 'Dashboard has not loaded yet.'
  },
  runs: [],
  queue: {}
};

function getRunProgress(run: OperationalRunSummary): string {
  const total = run.totalWorkItems ?? 0;
  const complete = run.completedWorkItems ?? 0;

  if (total <= 0) {
    return 'No work items';
  }

  return `${complete}/${total}`;
}

function getActiveRunCount(runs: OperationalRunSummary[]): number {
  return runs.filter((run) => {
    const status = run.status.toLowerCase();
    return status === 'running' || status === 'queued' || status === 'dispatching';
  }).length;
}

export function OperationalRuntimeDashboard() {
  const [model, setModel] = useState<OperationalRuntimeDashboardModel>(emptyModel);
  const [loadState, setLoadState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);

  const activeRunCount = useMemo(() => getActiveRunCount(model.runs), [model.runs]);

  async function refresh(signal?: AbortSignal) {
    setLoadState('loading');
    setError(null);

    try {
      const nextModel = await getOperationalRuntimeDashboard(signal);
      setModel(nextModel);
      setLoadState('loaded');
    } catch (nextError) {
      setLoadState('failed');
      setError(nextError instanceof Error ? nextError.message : 'Failed to load runtime dashboard.');
    }
  }

  useEffect(() => {
    const controller = new AbortController();

    void refresh(controller.signal);

    return () => controller.abort();
  }, []);

  return (
    <section
      aria-labelledby="operational-runtime-dashboard-title"
      style={{
        display: 'grid',
        gap: '1rem',
        marginTop: '1.5rem'
      }}
    >
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '1rem'
        }}
      >
        <div>
          <h2 id="operational-runtime-dashboard-title" style={{ margin: 0 }}>
            Operational Runtime
          </h2>
          <p style={{ margin: '0.35rem 0 0', color: '#526173' }}>
            SQL-backed run, queue, and readiness view for migration operators.
          </p>
        </div>

        <button
          type="button"
          onClick={() => void refresh()}
          disabled={loadState === 'loading'}
          style={{
            borderRadius: '0.6rem',
            border: '1px solid #b9c8da',
            background: '#ffffff',
            cursor: loadState === 'loading' ? 'not-allowed' : 'pointer',
            padding: '0.55rem 0.85rem',
            fontWeight: 700
          }}
        >
          {loadState === 'loading' ? 'Refreshing…' : 'Refresh'}
        </button>
      </header>

      {error ? (
        <div
          role="alert"
          style={{
            border: '1px solid #e4b4b4',
            background: '#fff6f6',
            borderRadius: '0.9rem',
            padding: '0.85rem'
          }}
        >
          {error}
        </div>
      ) : null}

      <div
        style={{
          display: 'grid',
          gap: '1rem',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))'
        }}
      >
        <article style={cardStyle}>
          <span style={labelStyle}>Readiness</span>
          <div style={{ marginTop: '0.55rem' }}>
            <RuntimeStatusBadge status={model.readiness.status} />
          </div>
          <p style={bodyStyle}>{model.readiness.message ?? 'No readiness message returned.'}</p>
        </article>

        <article style={cardStyle}>
          <span style={labelStyle}>Active runs</span>
          <strong style={metricStyle}>{activeRunCount}</strong>
          <p style={bodyStyle}>{model.runs.length} total run records returned.</p>
        </article>

        <article style={cardStyle}>
          <span style={labelStyle}>Queue</span>
          <strong style={metricStyle}>{model.queue.queued ?? 0}</strong>
          <p style={bodyStyle}>
            {model.queue.leased ?? 0} leased · {model.queue.failed ?? 0} failed ·{' '}
            {model.queue.retryPending ?? 0} retry pending
          </p>
        </article>
      </div>

      <article style={cardStyle}>
        <header
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            gap: '1rem',
            alignItems: 'center'
          }}
        >
          <h3 style={{ margin: 0 }}>Recent runs</h3>
          <span style={bodyStyle}>{model.runs.length} returned</span>
        </header>

        {model.runs.length === 0 ? (
          <p style={bodyStyle}>No runs were returned by the Admin API.</p>
        ) : (
          <div style={{ overflowX: 'auto', marginTop: '0.75rem' }}>
            <table style={{ borderCollapse: 'collapse', width: '100%' }}>
              <thead>
                <tr>
                  <th style={thStyle}>Run</th>
                  <th style={thStyle}>Status</th>
                  <th style={thStyle}>Progress</th>
                  <th style={thStyle}>Failures</th>
                  <th style={thStyle}>Started</th>
                </tr>
              </thead>
              <tbody>
                {model.runs.slice(0, 10).map((run) => (
                  <tr key={run.runId}>
                    <td style={tdStyle}>{run.runId}</td>
                    <td style={tdStyle}>
                      <RuntimeStatusBadge status={run.status} />
                    </td>
                    <td style={tdStyle}>{getRunProgress(run)}</td>
                    <td style={tdStyle}>{run.failedWorkItems ?? 0}</td>
                    <td style={tdStyle}>{run.startedAtUtc ?? 'not started'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>
    </section>
  );
}

const cardStyle: React.CSSProperties = {
  background: '#ffffff',
  border: '1px solid #d9e2ee',
  borderRadius: '1rem',
  boxShadow: '0 10px 30px rgba(19, 31, 48, 0.06)',
  padding: '1rem'
};

const labelStyle: React.CSSProperties = {
  color: '#5a6a7d',
  fontSize: '0.8rem',
  fontWeight: 800,
  letterSpacing: '0.04em',
  textTransform: 'uppercase'
};

const bodyStyle: React.CSSProperties = {
  color: '#526173',
  margin: '0.65rem 0 0'
};

const metricStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '2rem',
  marginTop: '0.45rem'
};

const thStyle: React.CSSProperties = {
  borderBottom: '1px solid #d9e2ee',
  color: '#526173',
  fontSize: '0.8rem',
  padding: '0.65rem',
  textAlign: 'left',
  textTransform: 'uppercase'
};

const tdStyle: React.CSSProperties = {
  borderBottom: '1px solid #edf2f7',
  padding: '0.65rem',
  verticalAlign: 'top'
};
