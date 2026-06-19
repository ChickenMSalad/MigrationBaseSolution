import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type {
  RuntimeDashboardFailure,
  RuntimeDashboardRun,
  RuntimeDashboardEvent,
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

function formatProgressValue(completed?: number | null, total?: number | null) {
  if (completed === undefined || completed === null || total === undefined || total === null) {
    return "-";
  }

  return `${completed.toLocaleString()} / ${total.toLocaleString()}`;
}

function formatProgressPercent(value?: number | null) {
  return value === undefined || value === null ? "-" : `${value.toFixed(2)}%`;
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

  function exportRunSummary() {
    const run = getRun(detail);
    if (!run) {
      return;
    }

    downloadCsv(`${safeFileName(run.runName ?? run.runKey ?? run.runId)}-summary.csv`, [
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
    const run = getRun(detail);
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
    const run = getRun(detail);
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
    const run = getRun(detail);
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
    void load(false);
    const timer = window.setInterval(() => void load(true), 5000);
    return () => window.clearInterval(timer);
  }, [runId]);

  const run = getRun(detail);
  const workItems = useMemo<RuntimeDashboardWorkItem[]>(() => detail?.workItems ?? [], [detail]);
  const failures = useMemo<RuntimeDashboardFailure[]>(() => detail?.failures ?? [], [detail]);
  const events = useMemo<RuntimeDashboardEvent[]>(() => detail?.events ?? [], [detail]);
  const liveProgress = detail?.progress ?? null;

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
        <div className="actionRow">
          <button type="button" onClick={() => void load(true)} disabled={refreshing}>
            {refreshing ? "Refreshing" : "Refresh"}
          </button>
          <button type="button" onClick={exportRunSummary}>Export Summary</button>
          <button type="button" onClick={exportWorkItems}>Export Work Items</button>
          <button type="button" onClick={exportFailures}>Export Failures</button>
          <button type="button" onClick={exportTimeline}>Export Timeline</button>
        </div>
      </div>

      <div className="metric-grid">
        <Card title="Status">
          <StatusPill status={run.status ?? "Unknown"} />
        </Card>
        <Card title="Progress">
          <strong>{formatProgressPercent(liveProgress?.percentComplete) !== "-" ? formatProgressPercent(liveProgress?.percentComplete) : formatPercent(completedCount, totalCount)}</strong>
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


      <Card title="Live progress" subtitle="Latest durable progress event written by the executor.">
        {liveProgress?.updatedAtUtc ? (
          <div className="detail-grid">
            <div><dt>Completed / total</dt><dd>{formatProgressValue(liveProgress.completed, liveProgress.total)}</dd></div>
            <div><dt>Percent</dt><dd>{formatProgressPercent(liveProgress.percentComplete)}</dd></div>
            <div><dt>Last update</dt><dd>{formatDate(liveProgress.updatedAtUtc)}</dd></div>
            <div><dt>Message</dt><dd>{liveProgress.message ?? "-"}</dd></div>
          </div>
        ) : (
          <EmptyState title="No live progress event yet" description="Progress events appear once the executor starts processing manifest rows for this run." />
        )}
      </Card>

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


      <Card title="Run event timeline" subtitle="Durable operational and migration progress events for this run.">
        {events.length === 0 ? (
          <EmptyState title="No run events" description="No operational events were recorded for this run yet." />
        ) : (
          <div className="table-wrap">
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
                    <td>{formatProgressValue(event.completed, event.total)}</td>
                    <td>{event.message ?? "-"}</td>
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
