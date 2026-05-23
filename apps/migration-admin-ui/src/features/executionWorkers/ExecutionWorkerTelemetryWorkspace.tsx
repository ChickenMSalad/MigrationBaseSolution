import { useEffect, useState } from 'react';
import { fetchExecutionWorkerTelemetrySummary } from './executionWorkerTelemetryApi';
import type { ExecutionWorkerTelemetrySummary } from './executionWorkerTelemetryTypes';

export function ExecutionWorkerTelemetryWorkspace() {
  const [summary, setSummary] = useState<ExecutionWorkerTelemetrySummary | null>(null);
  const [staleAfterSeconds, setStaleAfterSeconds] = useState(120);
  const [error, setError] = useState<string | null>(null);

  async function loadTelemetry() {
    try {
      const response = await fetchExecutionWorkerTelemetrySummary(staleAfterSeconds);
      setSummary(response);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load execution worker telemetry.');
    }
  }

  useEffect(() => {
    void loadTelemetry();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Workers</p>
          <h2>Execution worker telemetry</h2>
        </div>
        <span className="status-pill">{summary?.totalWorkers ?? 0} workers</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Total workers</span>
          <strong>{summary?.totalWorkers ?? 0}</strong>
        </article>
        <article>
          <span>Active</span>
          <strong>{summary?.activeWorkers ?? 0}</strong>
        </article>
        <article>
          <span>Idle</span>
          <strong>{summary?.idleWorkers ?? 0}</strong>
        </article>
        <article>
          <span>Stale</span>
          <strong>{summary?.staleWorkers ?? 0}</strong>
        </article>
      </div>

      <div className="filter-row">
        <label>
          Stale after seconds
          <input
            type="number"
            min="30"
            max="86400"
            value={staleAfterSeconds}
            onChange={(event) => setStaleAfterSeconds(Number(event.target.value))}
          />
        </label>
        <button type="button" onClick={loadTelemetry}>Refresh workers</button>
      </div>

      <div className="table-shell">
        <table>
          <thead>
            <tr>
              <th>Worker</th>
              <th>Status</th>
              <th>Active leases</th>
              <th>Session</th>
              <th>Last heartbeat</th>
              <th>Message</th>
            </tr>
          </thead>
          <tbody>
            {summary?.workers.length ? (
              summary.workers.map((worker) => (
                <tr key={worker.workerId}>
                  <td><code>{worker.workerId}</code></td>
                  <td>{worker.status}</td>
                  <td>{worker.activeLeaseCount}</td>
                  <td>{worker.executionSessionId ? <code>{worker.executionSessionId}</code> : '—'}</td>
                  <td>{new Date(worker.lastHeartbeatUtc).toLocaleString()}</td>
                  <td>{worker.message ?? '—'}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={6}>No worker heartbeats have been recorded yet.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {summary?.generatedUtc ? <p>Generated {new Date(summary.generatedUtc).toLocaleString()}</p> : null}
    </section>
  );
}
