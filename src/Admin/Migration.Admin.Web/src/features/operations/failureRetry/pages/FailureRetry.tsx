import { useCallback, useEffect, useMemo, useState } from "react";
import { failureRetryApi } from "../api/failureRetryApi";
import type { FailureRetryResponse, FailureRetryWorkItem } from "../types/failureRetry";

type LoadState = {
  loading: boolean;
  error?: string;
  response?: FailureRetryResponse;
  actionMessage?: string;
  actionError?: string;
  retryingWorkItemId?: number;
};

function formatDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
}

function statusClass(item: FailureRetryWorkItem) {
  const status = String(item.status ?? "").toLowerCase();
  if (status.includes("retry")) {
    return "status-warning";
  }

  if (status.includes("fail")) {
    return "status-danger";
  }

  if (status.includes("complete")) {
    return "status-success";
  }

  return "status-neutral";
}

function canRetry(item: FailureRetryWorkItem) {
  const status = String(item.status ?? "").toLowerCase();
  return status.includes("fail") || status.includes("retry");
}

function csvValue(value: unknown) {
  const text = value === null || value === undefined ? "" : String(value);
  return '"' + text.replace(/"/g, '""') + '"';
}

function exportFailures(workItems: FailureRetryWorkItem[]) {
  const headers = [
    "WorkItemId",
    "RunId",
    "Status",
    "AttemptCount",
    "UpdatedUtc",
    "ErrorCode",
    "ErrorMessage",
    "PayloadJson"
  ];

  const rows = workItems.map(item => [
    item.workItemId,
    item.runId,
    item.status,
    item.attemptCount ?? 0,
    item.updatedAtUtc ?? "",
    item.lastErrorCode ?? "",
    item.lastErrorMessage ?? "",
    item.payloadJson ?? ""
  ]);

  const csv = [headers, ...rows]
    .map(row => row.map(csvValue).join(","))
    .join("\n");

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `failure-retry-${new Date().toISOString().replace(/[:.]/g, "-")}.csv`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

export function FailureRetry() {
  const [state, setState] = useState<LoadState>({ loading: true });

  const load = useCallback(async () => {
    setState((current) => ({
      ...current,
      loading: true,
      error: undefined,
      actionError: undefined,
    }));

    try {
      const response = await failureRetryApi.recent(50);
      setState((current) => ({
        ...current,
        loading: false,
        response,
      }));
    } catch (error) {
      setState((current) => ({
        ...current,
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      }));
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function retry(item: FailureRetryWorkItem) {
    setState((current) => ({
      ...current,
      retryingWorkItemId: item.workItemId,
      actionMessage: undefined,
      actionError: undefined,
    }));

    try {
      const response = await failureRetryApi.retryWorkItem(item.workItemId);
      const message = response.message || `Work item ${item.workItemId} was queued for retry.`;
      setState((current) => ({
        ...current,
        retryingWorkItemId: undefined,
        actionMessage: message,
      }));
      await load();
    } catch (error) {
      setState((current) => ({
        ...current,
        retryingWorkItemId: undefined,
        actionError: error instanceof Error ? error.message : String(error),
      }));
    }
  }

  const workItems = state.response?.workItems ?? [];
  const summary = state.response?.summary;
  const infoMessage = state.response?.message;
  const retryableCount = useMemo(() => workItems.filter(canRetry).length, [workItems]);

  return (
    <section className="operations-page">
      <p className="eyebrow">Runtime operations</p>
      <h1>Failure retry</h1>
      <p className="page-intro">
        Failed and retryable operational work items from the canonical runtime dashboard API.
      </p>

      <div className="toolbar-row">
        <button type="button" className="button-secondary" onClick={() => void load()} disabled={state.loading}>
          Refresh
        </button>
        <button type="button" className="button-secondary" onClick={() => exportFailures(workItems)} disabled={workItems.length === 0}>
          Export failures CSV
        </button>
      </div>

      {state.error ? <div className="alert alert-danger">{state.error}</div> : null}
      {state.actionError ? <div className="alert alert-danger">{state.actionError}</div> : null}
      {state.actionMessage ? <div className="alert alert-success">{state.actionMessage}</div> : null}
      {state.loading ? <div className="panel">Loading failure retry state...</div> : null}
      {!state.loading && infoMessage ? <div className="panel muted">{infoMessage}</div> : null}

      {!state.loading && summary ? (
        <div className="metric-grid compact">
          <article className="metric-card">
            <span>Failed</span>
            <strong>{summary.failed}</strong>
          </article>
          <article className="metric-card">
            <span>Retryable now</span>
            <strong>{retryableCount}</strong>
          </article>
          <article className="metric-card">
            <span>Retry queued</span>
            <strong>{summary.retryQueued}</strong>
          </article>
        </div>
      ) : null}

      {!state.loading && workItems.length === 0 ? (
        <div className="panel">No failed or retryable work items were returned.</div>
      ) : null}

      {!state.loading && workItems.length > 0 ? (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Work item</th>
                <th>Run</th>
                <th>Status</th>
                <th>Attempts</th>
                <th>Updated</th>
                <th>Error code</th>
                <th>Error</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {workItems.map((item) => (
                <tr key={item.workItemId}>
                  <td>{item.workItemId}</td>
                  <td>{item.runId}</td>
                  <td>
                    <span className={statusClass(item)}>{item.status}</span>
                  </td>
                  <td>{item.attemptCount ?? 0}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td>{item.lastErrorCode ?? "-"}</td>
                  <td>
                    <div>{item.lastErrorMessage ?? "-"}</div>
                    {item.payloadJson ? <details><summary>Payload</summary><pre>{item.payloadJson}</pre></details> : null}
                  </td>
                  <td>
                    <button
                      type="button"
                      className="button-secondary"
                      disabled={!canRetry(item) || state.retryingWorkItemId === item.workItemId}
                      onClick={() => void retry(item)}
                    >
                      {state.retryingWorkItemId === item.workItemId ? "Retrying..." : "Retry"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}
