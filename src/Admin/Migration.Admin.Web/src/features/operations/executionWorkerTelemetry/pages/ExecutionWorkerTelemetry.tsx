import { useEffect, useMemo, useState } from "react";
import { executionWorkerTelemetryApi } from "../api/executionWorkerTelemetryApi";
import type { WorkerHealthDiagnostics, WorkerHealthRow } from "../types/executionWorkerTelemetry";

type LoadState = {
  loading: boolean;
  error?: string;
  diagnostics?: WorkerHealthDiagnostics;
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

function formatAge(seconds?: number | null): string {
  if (seconds === undefined || seconds === null) {
    return "-";
  }

  if (seconds < 60) {
    return `${seconds}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainder = seconds % 60;
  if (minutes < 60) {
    return `${minutes}m ${remainder}s`;
  }

  const hours = Math.floor(minutes / 60);
  const minuteRemainder = minutes % 60;
  return `${hours}h ${minuteRemainder}m`;
}

function statusClass(worker: WorkerHealthRow): string {
  switch (worker.status) {
    case "Busy":
    case "Online":
      return "status-success";
    case "Idle":
      return "status-neutral";
    case "Stale":
    case "Offline":
      return "status-danger";
    default:
      return "status-warning";
  }
}

function csvEscape(value: unknown): string {
  const text = String(value ?? "");
  if (text.includes('"') || text.includes(",") || text.includes("\n") || text.includes("\r")) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function downloadCsv(fileName: string, rows: WorkerHealthRow[]) {
  const headers = [
    "workerId",
    "source",
    "status",
    "lastSeenUtc",
    "heartbeatAgeSeconds",
    "activeLeases",
    "inFlightWorkItems",
    "executionSessionId",
    "role",
    "message",
  ];

  const lines = [
    headers.join(","),
    ...rows.map(row => headers.map(header => csvEscape((row as unknown as Record<string, unknown>)[header])).join(",")),
  ];

  const blob = new Blob([lines.join("\n")], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export function ExecutionWorkerTelemetry() {
  const [staleAfterSeconds, setStaleAfterSeconds] = useState(120);
  const [state, setState] = useState<LoadState>({ loading: true });

  async function loadTelemetry(seconds = staleAfterSeconds) {
    setState((current) => ({ ...current, loading: true, error: undefined }));

    try {
      const diagnostics = await executionWorkerTelemetryApi.diagnostics(seconds);
      setState({ loading: false, diagnostics });
    } catch (error) {
      setState({
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  useEffect(() => {
    void loadTelemetry();
    const timer = window.setInterval(() => void loadTelemetry(), 15000);
    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const diagnostics = state.diagnostics;
  const rows = diagnostics?.workers ?? [];
  const queue = diagnostics?.operationalTelemetry.queue;

  const totals = useMemo(() => {
    return {
      total: rows.length,
      busy: rows.filter(x => x.status === "Busy").length,
      idle: rows.filter(x => x.status === "Idle").length,
      stale: rows.filter(x => x.status === "Stale" || x.status === "Offline").length,
    };
  }, [rows]);

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Runtime operations</p>
          <h1>Execution worker telemetry</h1>
          <p>
            Worker heartbeat, queue pressure, and stale-worker diagnostics from the operational runtime APIs.
          </p>
        </div>
        <div className="actionRow">
          <button type="button" className="button button-secondary" onClick={() => void loadTelemetry()}>
            Refresh workers
          </button>
          <button
            type="button"
            className="button button-secondary"
            disabled={rows.length === 0}
            onClick={() => downloadCsv("worker-health-diagnostics.csv", rows)}
          >
            Export CSV
          </button>
        </div>
      </div>

      {state.error ? <div className="alert alert-error">{state.error}</div> : null}
      {state.loading ? <div className="panel">Loading worker health diagnostics...</div> : null}

      {diagnostics ? (
        <>
          <div className="summary-grid">
            <div className="metric-card"><span>Workers</span><strong>{totals.total}</strong></div>
            <div className="metric-card"><span>Busy/online</span><strong>{totals.busy}</strong></div>
            <div className="metric-card"><span>Idle</span><strong>{totals.idle}</strong></div>
            <div className="metric-card"><span>Stale/offline</span><strong>{totals.stale}</strong></div>
          </div>

          <div className="summary-grid">
            <div className="metric-card"><span>Queued</span><strong>{queue?.ready ?? 0}</strong></div>
            <div className="metric-card"><span>Leased/dispatched</span><strong>{queue?.leased ?? 0}</strong></div>
            <div className="metric-card"><span>In flight</span><strong>{queue?.inFlight ?? 0}</strong></div>
            <div className="metric-card"><span>Failed/retryable</span><strong>{(queue?.failed ?? 0) + (queue?.retryable ?? 0)}</strong></div>
          </div>
        </>
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
        <span className="muted">Auto-refreshes every 15 seconds.</span>
      </div>

      {diagnostics?.warnings && diagnostics.warnings.length > 0 ? (
        <div className="panel">
          <h2>Operational warnings</h2>
          <ul>
            {diagnostics.warnings.map((warning, index) => <li key={`${warning}-${index}`}>{warning}</li>)}
          </ul>
        </div>
      ) : null}

      {!state.loading && rows.length === 0 ? (
        <div className="panel">No worker telemetry rows are visible yet.</div>
      ) : null}

      {rows.length > 0 ? (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Worker</th>
                <th>Health</th>
                <th>Source</th>
                <th>Age</th>
                <th>Active leases</th>
                <th>In flight</th>
                <th>Last seen</th>
                <th>Session / role</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((worker, index) => (
                <tr key={`${worker.source}-${worker.workerId}-${worker.lastSeenUtc ?? index}`}>
                  <td><code>{worker.workerId}</code></td>
                  <td><span className={statusClass(worker)}>{worker.status}</span></td>
                  <td>{worker.source}</td>
                  <td>{formatAge(worker.heartbeatAgeSeconds)}</td>
                  <td>{worker.activeLeases}</td>
                  <td>{worker.inFlightWorkItems}</td>
                  <td>{formatDate(worker.lastSeenUtc)}</td>
                  <td>{worker.executionSessionId ? <code>{worker.executionSessionId}</code> : worker.role ?? "-"}</td>
                  <td>{worker.message ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {diagnostics?.generatedUtc ? (
        <p className="muted">Generated {formatDate(diagnostics.generatedUtc)}</p>
      ) : null}
    </section>
  );
}
