import { useEffect, useState } from "react";
import { executionWorkerTelemetryApi } from "../api/executionWorkerTelemetryApi";
import type {
  ExecutionWorkerHeartbeatRecord,
  ExecutionWorkerTelemetrySummary,
} from "../types/executionWorkerTelemetry";

type LoadState = {
  loading: boolean;
  error?: string;
  summary?: ExecutionWorkerTelemetrySummary;
};

function formatDate(value?: string | null): string {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
}

function workerStatusClass(worker: ExecutionWorkerHeartbeatRecord): string {
  const status = String(worker.status ?? "").toLowerCase();
  if (status.includes("stale") || status.includes("failed")) {
    return "status-danger";
  }

  if (status.includes("active") || status.includes("running")) {
    return "status-success";
  }

  if (status.includes("idle")) {
    return "status-neutral";
  }

  return "status-warning";
}

export function ExecutionWorkerTelemetry() {
  const [staleAfterSeconds, setStaleAfterSeconds] = useState(120);
  const [state, setState] = useState<LoadState>({ loading: true });

  async function loadTelemetry(seconds = staleAfterSeconds) {
    setState((current) => ({ ...current, loading: true, error: undefined }));

    try {
      const summary = await executionWorkerTelemetryApi.summary(seconds);
      setState({ loading: false, summary });
    } catch (error) {
      setState({
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  useEffect(() => {
    void loadTelemetry();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const workers = state.summary?.workers ?? [];

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Runtime operations</p>
          <h1>Execution worker telemetry</h1>
          <p>
            Worker heartbeat and lease visibility from the canonical Admin API execution-worker telemetry endpoint.
          </p>
        </div>
        <button type="button" className="button button-secondary" onClick={() => void loadTelemetry()}>
          Refresh workers
        </button>
      </div>

      {state.error ? <div className="alert alert-error">{state.error}</div> : null}
      {state.loading ? <div className="panel">Loading execution worker telemetry...</div> : null}

      {state.summary ? (
        <div className="summary-grid">
          <div className="metric-card">
            <span>Total workers</span>
            <strong>{state.summary.totalWorkers}</strong>
          </div>
          <div className="metric-card">
            <span>Active</span>
            <strong>{state.summary.activeWorkers}</strong>
          </div>
          <div className="metric-card">
            <span>Idle</span>
            <strong>{state.summary.idleWorkers}</strong>
          </div>
          <div className="metric-card">
            <span>Stale</span>
            <strong>{state.summary.staleWorkers}</strong>
          </div>
        </div>
      ) : null}

      <div className="panel toolbar-panel">
        <label htmlFor="staleAfterSeconds">Stale after seconds</label>
        <input
          id="staleAfterSeconds"
          type="number"
          min="15"
          max="3600"
          value={staleAfterSeconds}
          onChange={(event) => setStaleAfterSeconds(Number(event.target.value))}
        />
        <button type="button" className="button" onClick={() => void loadTelemetry(staleAfterSeconds)}>
          Apply threshold
        </button>
      </div>

      {!state.loading && workers.length === 0 ? (
        <div className="panel">No worker heartbeats have been recorded yet.</div>
      ) : null}

      {workers.length > 0 ? (
        <div className="table-card">
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
              {workers.map((worker) => (
                <tr key={`${worker.workerId}-${worker.lastHeartbeatUtc}`}>
                  <td><code>{worker.workerId}</code></td>
                  <td><span className={workerStatusClass(worker)}>{worker.status}</span></td>
                  <td>{worker.activeLeaseCount}</td>
                  <td>{worker.executionSessionId ? <code>{worker.executionSessionId}</code> : "-"}</td>
                  <td>{formatDate(worker.lastHeartbeatUtc)}</td>
                  <td>{worker.message ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {state.summary?.generatedUtc ? (
        <p className="muted">Generated {formatDate(state.summary.generatedUtc)}</p>
      ) : null}
    </section>
  );
}
