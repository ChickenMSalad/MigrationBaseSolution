import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import { runtimeDashboardApi } from "../../runtimeDashboard/api/runtimeDashboardApi";
import type { RuntimeDashboardRun, RuntimeDashboardRunDetail, RuntimeDashboardWorkItem } from "../../runtimeDashboard/types/runtimeDashboard";

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

function workItemStatus(item: RuntimeDashboardWorkItem) {
  return item.status ?? "Unknown";
}

function canDeleteRun(run: RuntimeDashboardRun) {
  const status = String(run.effectiveStatus ?? run.status ?? "").toLowerCase();
  return ![
    "queued",
    "dispatching",
    "dispatched",
    "running",
    "leased",
    "inprogress",
    "in-progress",
    "processing",
    "started",
    "executing"
  ].includes(status);
}

export function RunDetail() {
  const navigate = useNavigate();
  const { runId } = useParams();
  const [detail, setDetail] = useState<RuntimeDashboardRunDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState(false);

  async function load() {
    if (!runId) {
      setError("Missing run id.");
      setLoading(false);
      return;
    }

    setError(null);

    try {
      setDetail(await runtimeDashboardApi.runDetail(runId));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  async function deleteRun() {
    const run = detail?.run;
    if (!run) {
      return;
    }

    const displayName = run.runName ?? run.runKey ?? run.runId;
    if (!canDeleteRun(run)) {
      setError(`Run "${displayName}" is active and cannot be deleted.`);
      return;
    }

    const confirmed = window.confirm(`Delete run "${displayName}"? This removes the Admin run record and SQL runtime run/work-item records.`);
    if (!confirmed) {
      return;
    }

    setDeleting(true);
    setError(null);

    try {
      await runtimeDashboardApi.deleteRun(run.runId);
      navigate("/runs");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeleting(false);
    }
  }

  useEffect(() => {
    void load();
    const timer = window.setInterval(load, 5000);
    return () => window.clearInterval(timer);
  }, [runId]);

  const run = detail?.run ?? null;
  const displayName = run?.runName ?? run?.runKey ?? run?.runId ?? "Run";
  const status = run?.effectiveStatus ?? run?.status ?? "Unknown";

  return (
    <>
      <p><Link to="/runs">Back to Runs</Link></p>

      <Card
        title={displayName}
        subtitle="SQL operational runtime run detail."
        action={
          <div className="actionRow">
            <button onClick={() => void load()}>Refresh</button>
            {run && (
              <button
                type="button"
                onClick={() => void deleteRun()}
                disabled={deleting || !canDeleteRun(run)}
                title={canDeleteRun(run) ? "Delete this run" : "Active runs cannot be deleted"}
              >
                {deleting ? "Deleting..." : "Delete"}
              </button>
            )}
          </div>
        }
      >
        <LoadingError loading={loading} error={error} onRetry={() => void load()} />

        {!loading && !error && !run && (
          <EmptyState title="Run not found" message="No SQL operational run was found for this run id." />
        )}

        {run && (
          <>
            <div className="metric-grid">
              <Card title="Status"><StatusPill status={status} /></Card>
              <Card title="Progress"><strong>{formatNumber(run.percentComplete)}%</strong></Card>
              <Card title="Completed"><strong>{formatNumber(run.completedWorkItemCount)}</strong></Card>
              <Card title="Failed"><strong>{formatNumber(run.failedWorkItemCount)}</strong></Card>
            </div>

            <table>
              <tbody>
                <tr><th>Run ID</th><td>{run.runId}</td></tr>
                <tr><th>Run key</th><td>{run.runKey ?? "-"}</td></tr>
                <tr><th>Source</th><td>{run.sourceSystem ?? "-"}</td></tr>
                <tr><th>Target</th><td>{run.targetSystem ?? "-"}</td></tr>
                <tr><th>Environment</th><td>{run.environmentName ?? "-"}</td></tr>
                <tr><th>Dry run</th><td>{run.isDryRun ? "Yes" : "No"}</td></tr>
                <tr><th>Overwrite existing target</th><td>{run.overwriteExisting ? "Yes" : "No"}</td></tr>
                <tr><th>Requested</th><td>{formatDate(run.requestedAtUtc)}</td></tr>
                <tr><th>Created</th><td>{formatDate(run.createdAtUtc)}</td></tr>
                <tr><th>Updated</th><td>{formatDate(run.updatedAtUtc)}</td></tr>
              </tbody>
            </table>
          </>
        )}
      </Card>

      {run && (
        <Card title="Work item progress" subtitle="Latest SQL work item state for this run.">
          {detail?.workItems.length === 0 ? (
            <EmptyState title="No work items" message="No work items were found for this operational run." />
          ) : (
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
                {(detail?.workItems ?? []).map((item) => (
                  <tr key={item.workItemId}>
                    <td>{item.workItemId}</td>
                    <td><StatusPill status={workItemStatus(item)} /></td>
                    <td>{item.workType ?? "-"}</td>
                    <td>{formatNumber(item.attemptCount)}</td>
                    <td>{item.claimedBy ?? "-"}</td>
                    <td>{formatDate(item.updatedAtUtc ?? item.completedAtUtc ?? item.createdAtUtc)}</td>
                    <td>{item.lastErrorMessage ?? "-"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>
      )}

      {run && detail && detail.failures.length > 0 && (
        <Card title="Failures" subtitle="Failure rows associated with this operational run.">
          <table>
            <thead>
              <tr>
                <th>Work item</th>
                <th>Type</th>
                <th>Message</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {detail.failures.map((failure, index) => (
                <tr key={`${failure.workItemId ?? "failure"}-${index}`}>
                  <td>{failure.workItemId ?? "-"}</td>
                  <td>{failure.failureType ?? "-"}</td>
                  <td>{failure.message ?? "-"}</td>
                  <td>{formatDate(failure.createdAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </>
  );
}
