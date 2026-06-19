import { useCallback, useEffect, useState } from "react";
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


function csvCell(value: unknown) {
  const text = value === null || value === undefined ? "" : String(value);
  return `"${text.replace(/"/g, '""')}"`;
}

function downloadFailureReport(workItems: FailureRetryWorkItem[]) {
  const headers = [
    "workItemId",
    "runId",
    "status",
    "workType",
    "attemptCount",
    "claimedBy",
    "createdAtUtc",
    "updatedAtUtc",
    "completedAtUtc",
    "lastErrorMessage"
  ];

  const lines = [
    headers.join(","),
    ...workItems.map(item => [
      item.workItemId,
      item.runId,
      item.status,
      item.workType ?? "",
      item.attemptCount ?? 0,
      item.claimedBy ?? "",
      item.createdAtUtc ?? "",
      item.updatedAtUtc ?? "",
      item.completedAtUtc ?? "",
      item.lastErrorMessage ?? ""
    ].map(csvCell).join(","))
  ];

  const blob = new Blob([lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
  anchor.href = url;
  anchor.download = `failure-retry-report-${timestamp}.csv`;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

function canRetry(item: FailureRetryWorkItem) {
  const status = String(item.status ?? "").toLowerCase();
  return status.includes("fail") || status.includes("retry");
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
      const message = response.message || `Work item ${item.workItemId} was reset for retry.`;
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
        <button
          type="button"
          className="button-secondary"
          onClick={() => downloadFailureReport(workItems)}
          disabled={state.loading || workItems.length === 0}
        >
          Export CSV
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
            <span>Retryable</span>
            <strong>{summary.retryable}</strong>
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
                <th>Type</th>
                <th>Attempts</th>
                <th>Updated</th>
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
                  <td>{item.workType ?? "-"}</td>
                  <td>{item.attemptCount ?? 0}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td>{item.lastErrorMessage ?? "-"}</td>
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
