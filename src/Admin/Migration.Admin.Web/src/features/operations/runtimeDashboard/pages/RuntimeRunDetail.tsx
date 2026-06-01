import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
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
    return <EmptyState title="Loading runtime run" description="Reading run detail from the operational store." />;
  }

  if (error) {
    return <LoadingError title="Runtime run failed to load" message={error} onRetry={() => void load()} />;
  }

  if (!detail) {
    return <EmptyState title="Runtime run not found" description="No run detail was returned for this run id." />;
  }

  return (
    <>
      <div className="page-header">
        <div>
          <Link to="/runtime-dashboard">← Runtime dashboard</Link>
          <h1>{detail.runName || detail.runKey || detail.runId}</h1>
          <p>Operational runtime run detail from Azure SQL.</p>
        </div>
        <button type="button" onClick={() => void load()}>Refresh</button>
      </div>

      <div className="metric-grid">
        <Card title="Status"><StatusPill status={detail.status} /></Card>
        <Card title="Work items">{formatNumber(detail.workItemCount)}</Card>
        <Card title="Completed">{formatNumber(detail.completedWorkItemCount)}</Card>
        <Card title="Failed">{formatNumber(detail.failedWorkItemCount)}</Card>
      </div>

      <section className="panel">
        <h2>Run metadata</h2>
        <dl className="definition-list">
          <dt>Run ID</dt><dd>{detail.runId}</dd>
          <dt>Run key</dt><dd>{detail.runKey ?? "—"}</dd>
          <dt>Environment</dt><dd>{detail.environmentName ?? "—"}</dd>
          <dt>Requested</dt><dd>{formatDate(detail.requestedAtUtc)}</dd>
          <dt>Created</dt><dd>{formatDate(detail.createdAtUtc)}</dd>
          <dt>Updated</dt><dd>{formatDate(detail.updatedAtUtc)}</dd>
        </dl>
      </section>

      <section className="panel">
        <h2>Work items</h2>
        {detail.workItems.length === 0 ? (
          <EmptyState title="No work items" description="This run has no operational work items." />
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
                  <td><StatusPill status={item.status} /></td>
                  <td>{item.workType}</td>
                  <td>{formatNumber(item.attemptCount)}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td>{item.lastErrorMessage ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}
