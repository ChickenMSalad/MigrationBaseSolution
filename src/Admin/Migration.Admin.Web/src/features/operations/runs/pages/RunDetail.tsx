import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import { runtimeDashboardApi } from "../../runtimeDashboard/api/runtimeDashboardApi";
import type { RuntimeDashboardRun, RuntimeDashboardEvent, RuntimeDashboardRunDetail, RuntimeDashboardWorkItem } from "../../runtimeDashboard/types/runtimeDashboard";

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


function formatPercent(value?: number | null) {
  return value === undefined || value === null ? "-" : `${value.toFixed(2)}%`;
}

function formatProgress(completed?: number | null, total?: number | null) {
  if (completed === undefined || completed === null || total === undefined || total === null) {
    return "-";
  }

  return `${completed.toLocaleString()} / ${total.toLocaleString()}`;
}

function workItemStatus(item: RuntimeDashboardWorkItem) {
  return item.status ?? "Unknown";
}


function csvValue(value: unknown) {
  if (value === undefined || value === null) {
    return "";
  }

  const text = String(value);
  if (text.includes("\"") || text.includes(",") || text.includes("\n") || text.includes("\r")) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function downloadCsv(fileName: string, rows: unknown[][]) {
  const csv = rows.map((row) => row.map(csvValue).join(",")).join("\r\n");
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

function safeFileName(value: string) {
  return value.replace(/[^a-zA-Z0-9._-]+/g, "-").replace(/^-+|-+$/g, "") || "run";
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

  function exportRunSummary() {
    const run = detail?.run;
    if (!run) {
      return;
    }

    const fileName = `${safeFileName(run.runName ?? run.runKey ?? run.runId)}-summary.csv`;
    downloadCsv(fileName, [
      ["Field", "Value"],
      ["Run ID", run.runId],
      ["Run key", run.runKey ?? ""],
      ["Run name", run.runName ?? ""],
      ["Status", run.effectiveStatus ?? run.status ?? ""],
      ["Source", run.sourceSystem ?? ""],
      ["Target", run.targetSystem ?? ""],
      ["Environment", run.environmentName ?? ""],
      ["Dry run", run.isDryRun ? "true" : "false"],
      ["Overwrite existing", run.overwriteExisting ? "true" : "false"],
      ["Work item count", run.workItemCount],
      ["Queued", run.queuedWorkItemCount],
      ["Dispatched", run.dispatchedWorkItemCount],
      ["Running", run.runningWorkItemCount ?? 0],
      ["Completed", run.completedWorkItemCount],
      ["Failed", run.failedWorkItemCount],
      ["Retryable", run.retryableWorkItemCount ?? 0],
      ["Percent complete", detail?.progress?.percentComplete ?? run.percentComplete ?? ""],
      ["Requested UTC", run.requestedAtUtc ?? ""],
      ["Created UTC", run.createdAtUtc ?? ""],
      ["Updated UTC", run.updatedAtUtc ?? ""]
    ]);
  }

  function exportWorkItems() {
    const run = detail?.run;
    const workItems = detail?.workItems ?? [];
    if (!run) {
      return;
    }

    downloadCsv(`${safeFileName(run.runName ?? run.runKey ?? run.runId)}-work-items.csv`, [
      ["WorkItemId", "RunId", "Status", "WorkType", "AttemptCount", "ClaimedBy", "CreatedUtc", "UpdatedUtc", "CompletedUtc", "LastErrorMessage"],
      ...workItems.map((item) => [
        item.workItemId,
        item.runId,
        item.status ?? "",
        item.workType ?? "",
        item.attemptCount ?? "",
        item.claimedBy ?? "",
        item.createdAtUtc ?? "",
        item.updatedAtUtc ?? "",
        item.completedAtUtc ?? "",
        item.lastErrorMessage ?? ""
      ])
    ]);
  }

  function exportFailures() {
    const run = detail?.run;
    const failures = detail?.failures ?? [];
    if (!run) {
      return;
    }

    downloadCsv(`${safeFileName(run.runName ?? run.runKey ?? run.runId)}-failures.csv`, [
      ["FailureId", "RunId", "WorkItemId", "ManifestRowId", "FailureType", "Message", "CreatedUtc"],
      ...failures.map((failure) => [
        failure.failureId ?? "",
        failure.runId ?? "",
        failure.workItemId ?? "",
        failure.manifestRowId ?? "",
        failure.failureType ?? "",
        failure.message ?? "",
        failure.createdAtUtc ?? ""
      ])
    ]);
  }

  function exportTimeline() {
    const run = detail?.run;
    const events = detail?.events ?? [];
    if (!run) {
      return;
    }

    downloadCsv(`${safeFileName(run.runName ?? run.runKey ?? run.runId)}-timeline.csv`, [
      ["EventId", "CreatedUtc", "EventType", "Severity", "Category", "Source", "WorkItemId", "Completed", "Total", "Message"],
      ...events.map((event) => [
        event.eventId ?? "",
        event.createdAtUtc ?? "",
        event.eventType ?? "",
        event.severity ?? "",
        event.category ?? "",
        event.source ?? "",
        event.workItemId ?? "",
        event.completed ?? "",
        event.total ?? "",
        event.message ?? ""
      ])
    ]);
  }

  useEffect(() => {
    void load();
    const timer = window.setInterval(load, 5000);
    return () => window.clearInterval(timer);
  }, [runId]);

  const run = detail?.run ?? null;
  const displayName = run?.runName ?? run?.runKey ?? run?.runId ?? "Run";
  const status = run?.effectiveStatus ?? run?.status ?? "Unknown";
  const progress = detail?.progress ?? null;
  const events: RuntimeDashboardEvent[] = detail?.events ?? [];

  return (
    <>
      <p><Link to="/runs">Back to Runs</Link></p>

      <Card
        title={displayName}
        subtitle="SQL operational runtime run detail."
        action={
          <div className="actionRow">
            <button onClick={() => void load()}>Refresh</button>
            {run && <button type="button" onClick={exportRunSummary}>Export Summary</button>}
            {run && <button type="button" onClick={exportWorkItems}>Export Work Items</button>}
            {run && <button type="button" onClick={exportFailures}>Export Failures</button>}
            {run && <button type="button" onClick={exportTimeline}>Export Timeline</button>}
            {run && <Link className="secondaryButton" to={`/runs/${encodeURIComponent(run.runId)}/target-evidence`}>Target Evidence</Link>}
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
              <Card title="Progress"><strong>{formatPercent(progress?.percentComplete ?? run.percentComplete)}</strong><div className="muted">{formatProgress(progress?.completed, progress?.total)}</div></Card>
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
        <Card title="Live progress" subtitle="Latest durable progress event written by the executor while the run is active.">
          {progress?.updatedAtUtc ? (
            <table>
              <tbody>
                <tr><th>Completed / total</th><td>{formatProgress(progress.completed, progress.total)}</td></tr>
                <tr><th>Percent</th><td>{formatPercent(progress.percentComplete)}</td></tr>
                <tr><th>Last update</th><td>{formatDate(progress.updatedAtUtc)}</td></tr>
                <tr><th>Message</th><td>{progress.message ?? "-"}</td></tr>
              </tbody>
            </table>
          ) : (
            <EmptyState title="No live progress event yet" message="Progress events appear once the executor starts processing manifest rows for this run." />
          )}
        </Card>
      )}

      {run && (
        <Card title="Work item progress" subtitle="Latest SQL work item state for this run.">
          {detail?.workItems.length === 0 ? (
            <EmptyState title="No work items" message="No work items were found for this operational run." />
          ) : (
            <div className="tableScroll tableScrollMedium">
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
            </div>
          )}
        </Card>
      )}


      {run && (
        <Card title="Run event timeline" subtitle="Durable operational and migration progress events for this run.">
          {events.length === 0 ? (
            <EmptyState title="No run events" message="No operational events were recorded for this run yet." />
          ) : (
            <div className="tableScroll tableScrollMedium">
              <table>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Type</th>
                    <th>Severity</th>
                    <th>Progress</th>
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  {events.map((event, index) => (
                    <tr key={event.eventId ?? `${event.createdAtUtc ?? "event"}-${index}`}>
                      <td>{formatDate(event.createdAtUtc)}</td>
                      <td>{event.eventType ?? "-"}</td>
                      <td>{event.severity ?? "-"}</td>
                      <td>{formatProgress(event.completed, event.total)}</td>
                      <td>{event.message ?? "-"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {run && detail && detail.failures.length > 0 && (
        <Card title="Failures" subtitle="Failure rows associated with this operational run.">
          <div className="tableScroll tableScrollMedium">
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
          </div>
        </Card>
      )}
    </>
  );
}

