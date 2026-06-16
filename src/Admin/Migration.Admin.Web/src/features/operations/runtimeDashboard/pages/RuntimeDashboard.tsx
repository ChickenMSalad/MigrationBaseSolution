import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type { RuntimeDashboardRun, RuntimeDashboardSummary } from "../types/runtimeDashboard";

function formatDate(value?: string | null) {
  if (!value) {
    return "n/a";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatNumber(value: number | undefined | null) {
  return value === undefined || value === null ? "0" : value.toLocaleString();
}

function formatPercent(value: number | undefined | null) {
  if (value === undefined || value === null || Number.isNaN(value)) {
    return "0%";
  }

  return `${Math.max(0, Math.min(100, value)).toFixed(1)}%`;
}

function calculatePercent(run: RuntimeDashboardRun) {
  if (typeof run.percentComplete === "number") {
    return run.percentComplete;
  }

  const total = run.workItemCount ?? 0;
  if (total <= 0) {
    return 0;
  }

  const finished = (run.completedWorkItemCount ?? 0) + (run.failedWorkItemCount ?? 0);
  return (finished / total) * 100;
}

function calculateActive(run: RuntimeDashboardRun) {
  return (
    (run.activeWorkItemCount ?? 0) ||
    (run.queuedWorkItemCount ?? 0) +
      (run.dispatchedWorkItemCount ?? 0) +
      (run.runningWorkItemCount ?? 0)
  );
}

function calculateThroughput(run: RuntimeDashboardRun) {
  const started = run.firstWorkItemStartedAtUtc ? new Date(run.firstWorkItemStartedAtUtc).getTime() : NaN;
  const finished = (run.completedWorkItemCount ?? 0) + (run.failedWorkItemCount ?? 0);

  if (!Number.isFinite(started) || finished <= 0) {
    return null;
  }

  const elapsedMinutes = Math.max((Date.now() - started) / 60000, 1 / 60);
  return finished / elapsedMinutes;
}

function calculateEta(run: RuntimeDashboardRun) {
  const throughput = calculateThroughput(run);
  if (!throughput || throughput <= 0) {
    return "n/a";
  }

  const remaining =
    (run.workItemCount ?? 0) -
    ((run.completedWorkItemCount ?? 0) + (run.failedWorkItemCount ?? 0));

  if (remaining <= 0) {
    return "Complete";
  }

  const minutes = remaining / throughput;
  if (minutes < 1) {
    return "Less than 1 minute";
  }

  if (minutes < 60) {
    return `${Math.ceil(minutes)} minutes`;
  }

  const hours = minutes / 60;
  return `${hours.toFixed(1)} hours`;
}

export function RuntimeDashboard() {
  const [summary, setSummary] = useState<RuntimeDashboardSummary | null>(null);
  const [runs, setRuns] = useState<RuntimeDashboardRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);

    try {
      const [summaryResult, runResult] = await Promise.all([
        runtimeDashboardApi.summary(),
        runtimeDashboardApi.runs(50),
      ]);

      setSummary(summaryResult);
      setRuns(runResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();

    const timer = window.setInterval(load, 5000);
    return () => window.clearInterval(timer);
  }, []);

  const activeRuns = useMemo(
    () =>
      runs.filter((run) => {
        const status = (run.effectiveStatus ?? run.status ?? "").toLowerCase();
        return status.includes("run") || calculateActive(run) > 0;
      }).length,
    [runs],
  );

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Runtime Dashboard</h1>
          <p>Live SQL operational runtime state from the cloud execution store.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()}>
          Refresh
        </button>
      </div>

      {error && <LoadingError message={error} />}

      {!loading && !error && (
        <>
          <div className="dashboard-grid">
            <Card title="Runs" subtitle="Operational runs">
              <div className="metric-value">{formatNumber(summary?.runCount)}</div>
              <div className="muted">{formatNumber(activeRuns)} active</div>
            </Card>
            <Card title="Work items" subtitle="Manifest execution rows">
              <div className="metric-value">{formatNumber(summary?.workItemCount)}</div>
              <div className="muted">{formatPercent(summary?.percentComplete)} complete</div>
            </Card>
            <Card title="Queued" subtitle="Waiting for dispatch">
              <div className="metric-value">{formatNumber(summary?.queuedWorkItemCount)}</div>
            </Card>
            <Card title="Dispatched" subtitle="Sent to executor">
              <div className="metric-value">{formatNumber(summary?.dispatchedWorkItemCount)}</div>
            </Card>
            <Card title="Running" subtitle="Started but not completed">
              <div className="metric-value">{formatNumber(summary?.runningWorkItemCount)}</div>
            </Card>
            <Card title="Completed" subtitle="Finished successfully">
              <div className="metric-value">{formatNumber(summary?.completedWorkItemCount)}</div>
            </Card>
            <Card title="Failures" subtitle="Failed or retryable rows">
              <div className="metric-value">{formatNumber(summary?.failedWorkItemCount)}</div>
              <div className="muted">{formatNumber(summary?.retryableWorkItemCount)} retryable</div>
            </Card>
            <Card title="Active pressure" subtitle="Queued + dispatched + running">
              <div className="metric-value">{formatNumber(summary?.activeWorkItemCount)}</div>
            </Card>
          </div>

          <Card title="Run progress" subtitle="Large manifest progress, throughput, and ETA from SQL work-item state.">
            {runs.length === 0 ? (
              <EmptyState title="No runs found" message="No SQL operational runs are currently recorded." />
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Run</th>
                    <th>Status</th>
                    <th>Progress</th>
                    <th>Queue</th>
                    <th>Throughput</th>
                    <th>ETA</th>
                    <th>Updated</th>
                  </tr>
                </thead>
                <tbody>
                  {runs.map((run) => {
                    const percent = calculatePercent(run);
                    const throughput = calculateThroughput(run);
                    return (
                      <tr key={run.runId}>
                        <td>
                          <Link to={`/runtime-dashboard/runs/${encodeURIComponent(run.runId)}`}>
                            {run.runName || run.runKey || run.runId}
                          </Link>
                          <div className="muted">
                            {run.sourceSystem ?? "source"} to {run.targetSystem ?? "target"}
                          </div>
                          <div className="muted">{run.runId}</div>
                        </td>
                        <td>
                          <StatusPill status={run.effectiveStatus ?? run.status ?? "Unknown"} />
                        </td>
                        <td>
                          <div className="progress-row">
                            <div className="progress-track" aria-label="Run progress">
                              <div className="progress-fill" style={{ width: `${Math.max(0, Math.min(100, percent))}%` }} />
                            </div>
                            <span>{formatPercent(percent)}</span>
                          </div>
                          <div className="muted">
                            {formatNumber((run.completedWorkItemCount ?? 0) + (run.failedWorkItemCount ?? 0))} /{" "}
                            {formatNumber(run.workItemCount)} finished
                          </div>
                          {run.failedWorkItemCount > 0 && (
                            <div className="muted">{formatNumber(run.failedWorkItemCount)} failed</div>
                          )}
                        </td>
                        <td>
                          <div>{formatNumber(run.queuedWorkItemCount)} queued</div>
                          <div>{formatNumber(run.dispatchedWorkItemCount)} dispatched</div>
                          <div>{formatNumber(run.runningWorkItemCount)} running</div>
                        </td>
                        <td>{throughput ? `${throughput.toFixed(1)} items/min` : "n/a"}</td>
                        <td>{calculateEta(run)}</td>
                        <td>{formatDate(run.updatedAtUtc ?? run.createdAtUtc ?? run.requestedAtUtc)}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </Card>
        </>
      )}
    </>
  );
}
