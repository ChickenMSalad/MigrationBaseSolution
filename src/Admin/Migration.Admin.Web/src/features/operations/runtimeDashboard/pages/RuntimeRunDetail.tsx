import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type {
  RuntimeDashboardFailure,
  RuntimeDashboardRun,
  RuntimeDashboardRunDetail,
  RuntimeDashboardWorkItem,
} from "../types/runtimeDashboard";

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

function formatPercent(completed: number, total: number) {
  if (total <= 0) {
    return "0%";
  }

  return `${Math.round((completed / total) * 100)}%`;
}

function isFailedStatus(status?: string | null) {
  return String(status ?? "").toLowerCase().includes("fail");
}

function isCompletedStatus(status?: string | null) {
  return String(status ?? "").toLowerCase().includes("complete");
}

function getRun(detail: RuntimeDashboardRunDetail | null): RuntimeDashboardRun | null {
  if (!detail) {
    return null;
  }

  const candidate = detail as RuntimeDashboardRunDetail & RuntimeDashboardRun;
  return detail.run ?? (candidate.runId ? candidate : null);
}

export function RuntimeRunDetail() {
  const { runId } = useParams();
  const [detail, setDetail] = useState<RuntimeDashboardRunDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load(showRefreshing = false) {
    if (!runId) {
      setError("Missing run id.");
      setLoading(false);
      return;
    }

    if (showRefreshing) {
      setRefreshing(true);
    }

    setError(null);
    try {
      const result = await runtimeDashboardApi.runDetail(runId);
      setDetail(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  useEffect(() => {
    void load(false);
    const timer = window.setInterval(() => void load(true), 5000);
    return () => window.clearInterval(timer);
  }, [runId]);

  const run = getRun(detail);
  const workItems = useMemo<RuntimeDashboardWorkItem[]>(() => detail?.workItems ?? [], [detail]);
  const failures = useMemo<RuntimeDashboardFailure[]>(() => detail?.failures ?? [], [detail]);

  const completedCount = run?.completedWorkItemCount ?? workItems.filter((item) => isCompletedStatus(item.status)).length;
  const failedCount = run?.failedWorkItemCount ?? workItems.filter((item) => isFailedStatus(item.status)).length;
  const totalCount = run?.workItemCount ?? workItems.length;
  const queuedCount = run?.queuedWorkItemCount ?? workItems.filter((item) => String(item.status ?? "").toLowerCase() === "queued").length;
  const dispatchedCount = run?.dispatchedWorkItemCount ?? workItems.filter((item) => String(item.status ?? "").toLowerCase() === "dispatched").length;

  if (loading) {
    return <LoadingError loading />;
  }

  if (error) {
    return <LoadingError error={error} onRetry={() => void load(false)} />;
  }

  if (!detail || !run) {
    return <EmptyState title="Run not found" description="The runtime dashboard did not return a SQL runtime record for this run." />;
  }

  return (
    <>
      <div className="page-header">
        <div>
          <Link to="/runtime-dashboard">Back to Runtime Dashboard</Link>
          <h1>{run.runName || run.runKey || run.runId}</h1>
          <p>Live SQL operational runtime detail.</p>
        </div>
        <button type="button" onClick={() => void load(true)} disabled={refreshing}>
          {refreshing ? "Refreshing" : "Refresh"}
        </button>
      </div>

      <div className="metric-grid">
        <Card title="Status">
          <StatusPill status={run.status ?? "Unknown"} />
        </Card>
        <Card title="Progress">
          <strong>{formatPercent(completedCount, totalCount)}</strong>
        </Card>
        <Card title="Completed">
          <strong>{formatNumber(completedCount)} / {formatNumber(totalCount)}</strong>
        </Card>
        <Card title="Failed">
          <strong>{formatNumber(failedCount)}</strong>
        </Card>
        <Card title="Queued">
          <strong>{formatNumber(queuedCount)}</strong>
        </Card>
        <Card title="Dispatched">
          <strong>{formatNumber(dispatchedCount)}</strong>
        </Card>
      </div>

      <Card title="Run metadata">
        <div className="detail-grid">
          <div><dt>Run ID</dt><dd>{run.runId}</dd></div>
          <div><dt>Run key</dt><dd>{run.runKey ?? "-"}</dd></div>
          <div><dt>Source</dt><dd>{run.sourceSystem ?? "-"}</dd></div>
          <div><dt>Target</dt><dd>{run.targetSystem ?? "-"}</dd></div>
          <div><dt>Environment</dt><dd>{run.environmentName ?? "-"}</dd></div>
          <div><dt>Dry run</dt><dd>{run.isDryRun ? "Yes" : "No"}</dd></div>
          <div><dt>Overwrite existing target</dt><dd>{run.overwriteExisting ? "Yes" : "No"}</dd></div>
          <div><dt>Requested</dt><dd>{formatDate(run.requestedAtUtc)}</dd></div>
          <div><dt>Created</dt><dd>{formatDate(run.createdAtUtc)}</dd></div>
          <div><dt>Updated</dt><dd>{formatDate(run.updatedAtUtc)}</dd></div>
        </div>
      </Card>

      <Card title="Work items" subtitle="Most recent SQL operational work items for this run.">
        {workItems.length === 0 ? (
          <EmptyState title="No work items" description="No work item rows were returned for this run." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Status</th>
                  <th>Type</th>
                  <th>Attempts</th>
                  <th>Claimed by</th>
                  <th>Updated</th>
                  <th>Error</th>
                </tr>
              </thead>
              <tbody>
                {workItems.map((item) => (
                  <tr key={item.workItemId}>
                    <td>{item.workItemId}</td>
                    <td><StatusPill status={item.status ?? "Unknown"} /></td>
                    <td>{item.workType ?? "-"}</td>
                    <td>{formatNumber(item.attemptCount)}</td>
                    <td>{item.claimedBy ?? "-"}</td>
                    <td>{formatDate(item.updatedAtUtc)}</td>
                    <td>{item.lastErrorMessage ?? "-"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <Card title="Failures" subtitle="Failure records linked to this runtime run.">
        {failures.length === 0 ? (
          <EmptyState title="No failures" description="No SQL failure rows were returned for this run." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Work item</th>
                  <th>Manifest row</th>
                  <th>Type</th>
                  <th>Message</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {failures.map((failure, index) => (
                  <tr key={`${failure.workItemId ?? "failure"}-${index}`}>
                    <td>{failure.workItemId ?? "-"}</td>
                    <td>{failure.manifestRowId ?? "-"}</td>
                    <td>{failure.failureType ?? "-"}</td>
                    <td>{failure.message ?? "-"}</td>
                    <td>{formatDate(failure.createdAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  );
}
