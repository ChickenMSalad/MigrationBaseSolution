import { useEffect, useState } from "react";
import { failureRetryApi } from "../api/failureRetryApi";
import type { FailureRetryResponse, FailureRetryWorkItem } from "../types/failureRetry";

type LoadState = {
  loading: boolean;
  error?: string;
  response?: FailureRetryResponse;
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

export function FailureRetry() {
  const [state, setState] = useState<LoadState>({ loading: true });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setState((current) => ({ ...current, loading: true, error: undefined }));

      try {
        const response = await failureRetryApi.recent(50);
        if (!cancelled) {
          setState({ loading: false, response });
        }
      } catch (error) {
        if (!cancelled) {
          setState({
            loading: false,
            error: error instanceof Error ? error.message : String(error),
          });
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const workItems = state.response?.workItems ?? [];
  const summary = state.response?.summary;

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Runtime operations</p>
          <h1>Failure retry</h1>
          <p className="muted">
            Failed and retryable operational work items from the canonical runtime dashboard API.
          </p>
        </div>
      </div>

      {state.error ? <div className="error-card">{state.error}</div> : null}
      {state.loading ? <div className="loading-card">Loading failure retry state...</div> : null}

      {!state.loading && summary ? (
        <div className="metric-grid">
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
        <div className="empty-card">No failed or retryable work items were returned.</div>
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
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {workItems.map((item) => (
                <tr key={item.workItemId}>
                  <td>{item.workItemId}</td>
                  <td className="mono">{item.runId}</td>
                  <td>
                    <span className={statusClass(item)}>{item.status}</span>
                  </td>
                  <td>{item.attemptCount ?? 0}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td className="error-cell">{item.lastErrorMessage ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}


