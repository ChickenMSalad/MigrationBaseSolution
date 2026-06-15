import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import { runtimeDashboardApi } from "../../runtimeDashboard/api/runtimeDashboardApi";
import type { RuntimeDashboardRun, RuntimeDashboardSummary } from "../../runtimeDashboard/types/runtimeDashboard";

function formatDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatNumber(value?: number | null) {
  return value === undefined || value === null ? "-" : value.toLocaleString();
}

function displayRunName(run: RuntimeDashboardRun) {
  return run.runName ?? run.runKey ?? run.runId;
}

function displayStatus(run: RuntimeDashboardRun) {
  return run.effectiveStatus ?? run.status ?? "Unknown";
}

export function Runs() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<RuntimeDashboardSummary | null>(null);
  const [runs, setRuns] = useState<RuntimeDashboardRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);

    try {
      const [summaryResult, runsResult] = await Promise.all([
        runtimeDashboardApi.summary(),
        runtimeDashboardApi.runs(100),
      ]);

      setSummary(summaryResult);
      setRuns(runsResult);
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

  return (
    <>
      <Card
        title="Runs"
        subtitle="SQL operational runtime runs. Status and progress come from migration.Runs and migration.WorkItems."
        action={<button onClick={() => void load()}>Refresh</button>}
      >
        <LoadingError loading={loading} error={error} onRetry={() => void load()} />

        {!loading && !error && (
          <>
            <div className="metric-grid">
              <Card title="Runs"><strong>{formatNumber(summary?.runCount)}</strong></Card>
              <Card title="Work items"><strong>{formatNumber(summary?.workItemCount)}</strong></Card>
              <Card title="Active"><strong>{formatNumber(summary?.activeWorkItemCount)}</strong></Card>
              <Card title="Failed"><strong>{formatNumber(summary?.failedWorkItemCount)}</strong></Card>
            </div>

            {runs.length === 0 ? (
              <EmptyState title="No operational runs" message="No SQL operational runs were found." />
            ) : (
              <table>
                <thead>
                  <tr>
                    <th>Run</th>
                    <th>Status</th>
                    <th>Source</th>
                    <th>Target</th>
                    <th>Progress</th>
                    <th>Failures</th>
                    <th>Updated</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {runs.map((run) => (
                    <tr key={run.runId}>
                      <td>
                        <button type="button" className="link-button" onClick={() => navigate("/runs/" + encodeURIComponent(run.runId))}>
                          {displayRunName(run)}
                        </button>
                        <div className="muted">{run.runId}</div>
                      </td>
                      <td><StatusPill status={displayStatus(run)} /></td>
                      <td>{run.sourceSystem ?? "-"}</td>
                      <td>{run.targetSystem ?? "-"}</td>
                      <td>
                        {formatNumber(run.completedWorkItemCount)} / {formatNumber(run.workItemCount)}
                        <div className="muted">{formatNumber(run.percentComplete)}% complete</div>
                      </td>
                      <td>{formatNumber(run.failedWorkItemCount)}</td>
                      <td>{formatDate(run.updatedAtUtc ?? run.createdAtUtc ?? run.requestedAtUtc)}</td>
                      <td>
                        <button type="button" onClick={() => navigate("/runs/" + encodeURIComponent(run.runId))}>
                          Open
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}
      </Card>
    </>
  );
}
