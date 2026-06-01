import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { RuntimeDashboardRun, RuntimeDashboardSummary } from "../types/runtimeDashboard";

function formatDate(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatNumber(value: number | undefined | null) {
  return value === undefined || value === null ? "—" : value.toLocaleString();
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
        runtimeDashboardApi.runs(50)
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
    const timer = window.setInterval(load, 10000);
    return () => window.clearInterval(timer);
  }, []);

  return (
    <>
      <header className="pageHeader">
        <div>
          <h1>Runtime Dashboard</h1>
          <p>Live SQL operational runtime state from the cloud execution store.</p>
        </div>
        <button className="secondaryButton" type="button" onClick={() => void load()}>
          Refresh
        </button>
      </header>

      <LoadingError loading={loading} error={error} />

      {!loading && !error && (
        <>
          <section className="metricGrid">
            <Card title="Runs">
              <div className="metricValue">{formatNumber(summary?.runCount)}</div>
            </Card>
            <Card title="Work items">
              <div className="metricValue">{formatNumber(summary?.workItemCount)}</div>
            </Card>
            <Card title="Queued">
              <div className="metricValue">{formatNumber(summary?.queuedWorkItemCount)}</div>
            </Card>
            <Card title="Dispatched">
              <div className="metricValue">{formatNumber(summary?.dispatchedWorkItemCount)}</div>
            </Card>
            <Card title="Completed">
              <div className="metricValue">{formatNumber(summary?.completedWorkItemCount)}</div>
            </Card>
            <Card title="Failed">
              <div className="metricValue">{formatNumber(summary?.failedWorkItemCount)}</div>
            </Card>
          </section>

          <Card title="Recent runtime runs" subtitle="Backed by /api/runtime/dashboard/runs">
            {runs.length === 0 ? (
              <EmptyState title="No runtime runs found" message="Run a smoke or migration execution to populate this dashboard." />
            ) : (
              <div className="tableWrap">
                <table>
                  <thead>
                    <tr>
                      <th>Run</th>
                      <th>Status</th>
                      <th>Environment</th>
                      <th>Work</th>
                      <th>Updated</th>
                    </tr>
                  </thead>
                  <tbody>
                    {runs.map((run) => (
                      <tr key={run.runId}>
                        <td>
                          <Link to={`/runtime/runs/${encodeURIComponent(run.runId)}`}>
                            {run.runName || run.runKey || run.runId}
                          </Link>
                          <div className="mutedText">{run.runId}</div>
                        </td>
                        <td><StatusPill status={run.status ?? undefined} /></td>
                        <td>{run.environmentName ?? "—"}</td>
                        <td>
                          {formatNumber(run.completedWorkItemCount)} / {formatNumber(run.workItemCount)} complete
                          {run.failedWorkItemCount > 0 && <div className="errorText">{run.failedWorkItemCount} failed</div>}
                        </td>
                        <td>{formatDate(run.updatedAtUtc ?? run.createdAtUtc ?? run.requestedAtUtc)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </Card>
        </>
      )}
    </>
  );
}
