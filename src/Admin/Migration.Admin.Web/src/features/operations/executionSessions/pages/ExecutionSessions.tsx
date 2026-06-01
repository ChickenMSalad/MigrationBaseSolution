import { useEffect, useState } from "react";
import { executionSessionsApi } from "../api/executionSessionsApi";
import type { ExecutionSessionRecord } from "../types/executionSessions";

type LoadState = {
  loading: boolean;
  error?: string;
  sessions: ExecutionSessionRecord[];
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

export function ExecutionSessions() {
  const [state, setState] = useState<LoadState>({ loading: true, sessions: [] });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setState((current) => ({ ...current, loading: true, error: undefined }));
      try {
        const response = await executionSessionsApi.recent(25);
        if (!cancelled) {
          setState({ loading: false, sessions: response.sessions ?? [] });
        }
      } catch (error) {
        if (!cancelled) {
          setState({
            loading: false,
            sessions: [],
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

  return (
    <main className="page-shell">
      <section className="page-header">
        <div>
          <p className="eyebrow">Runtime operations</p>
          <h1>Execution sessions</h1>
          <p>
            Recent execution sessions from the operational Admin API. This page is the canonical Admin Web migration target for the execution-session surface.
          </p>
        </div>
      </section>

      {state.error ? <div className="alert error">{state.error}</div> : null}
      {state.loading ? <div className="card">Loading execution sessions...</div> : null}

      {!state.loading && state.sessions.length === 0 ? (
        <div className="card">No recent execution sessions were returned.</div>
      ) : null}

      {!state.loading && state.sessions.length > 0 ? (
        <section className="card table-card">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Source</th>
                <th>Target</th>
                <th>Created</th>
                <th>Completed</th>
              </tr>
            </thead>
            <tbody>
              {state.sessions.map((session) => (
                <tr key={session.executionSessionId}>
                  <td>
                    <strong>{session.name}</strong>
                    <div className="muted">{session.executionSessionId}</div>
                  </td>
                  <td>{session.status}</td>
                  <td>{session.sourceConnector ?? "-"}</td>
                  <td>{session.targetConnector ?? "-"}</td>
                  <td>{formatDate(session.createdUtc)}</td>
                  <td>{formatDate(session.completedUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      ) : null}
    </main>
  );
}
