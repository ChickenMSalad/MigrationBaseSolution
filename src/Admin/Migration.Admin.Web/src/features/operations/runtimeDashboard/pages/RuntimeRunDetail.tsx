import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type { RuntimeDashboardRunDetail } from "../types/runtimeDashboard";

function formatDate(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatNumber(value?: number | null) {
  return value === undefined || value === null ? "—" : value.toLocaleString();
}

export function RuntimeRunDetail() {
  const { runId } = useParams();
  const [detail, setDetail] = useState<RuntimeDashboardRunDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    if (!runId) {
      setError("Missing run id.");
      setLoading(false);
      return;
    }

    setError(null);

    try {
      const result = await runtimeDashboardApi.runDetail(runId);
      setDetail(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [runId]);

  if (loading) {
    return <LoadingError loading />;
  }

  if (error) {
    return <LoadingError message={error} onRetry={() => void load()} />;
  }

  if (!detail || !detail.run) {
    return <EmptyState title="Run not found" message="The requested runtime run was not returned by the Admin API." />;
  }

  const run = detail.run;
  const displayName = run.runName ?? run.runKey ?? run.runId;

  return (
    <>
      <Link to="/operations/runtime-dashboard">â† Runtime dashboard</Link>

      <Card
        title={displayName}
        subtitle="Operational runtime run detail from Azure SQL."
        action={<button type="button" onClick={() => void load()}>Refresh</button>}
      >
        <div className="metric-grid">
          <div>
            <span>Total work items</span>
            <strong>{formatNumber(run.workItemCount)}</strong>
          </div>
          <div>
            <span>Completed</span>
            <strong>{formatNumber(run.completedWorkItemCount)}</strong>
          </div>
          <div>
            <span>Failed</span>
            <strong>{formatNumber(run.failedWorkItemCount)}</strong>
          </div>
          <div>
            <span>Status</span>
            <StatusPill status={run.status ?? undefined} />
          </div>
        </div>
      </Card>

      <Card title="Run metadata">
        <dl className="detail-grid">
          <dt>Run ID</dt>
          <dd>{run.runId}</dd>
          <dt>Run key</dt>
          <dd>{run.runKey ?? "—"}</dd>
          <dt>Environment</dt>
          <dd>{run.environmentName ?? "—"}</dd>
          <dt>Requested</dt>
          <dd>{formatDate(run.requestedAtUtc)}</dd>
          <dt>Created</dt>
          <dd>{formatDate(run.createdAtUtc)}</dd>
          <dt>Updated</dt>
          <dd>{formatDate(run.updatedAtUtc)}</dd>
        </dl>
      </Card>

      <Card title="Work items">
        {detail.workItems.length === 0 ? (
          <EmptyState title="No work items" message="No work items were returned for this run." />
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Status</th>
                <th>Type</th>
                <th>Attempts</th>
                <th>Updated</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {detail.workItems.map((item) => (
                <tr key={item.workItemId}>
                  <td>{item.workItemId}</td>
                  <td><StatusPill status={item.status ?? undefined} /></td>
                  <td>{item.workType ?? "—"}</td>
                  <td>{formatNumber(item.attemptCount)}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td>{item.lastErrorMessage ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </>
  );
}