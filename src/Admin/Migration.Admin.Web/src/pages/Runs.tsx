import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { Card, EmptyState, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { RunRecord } from "../types/api";

export function Runs() {
  const navigate = useNavigate();
  const openRun = (runId: string) => navigate("/runs/" + encodeURIComponent(runId));

  const [runs, setRuns] = useState<RunRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [deletingRunId, setDeletingRunId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);

    try {
      setRuns(await api.runs());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();

    const timer = window.setInterval(load, 5000);
    return () => window.clearInterval(timer);
  }, []);

  async function deleteRun(run: RunRecord) {
    const confirmed = window.confirm(`Delete run "${run.jobName}" (${run.runId})? This removes the local control-plane run record and monitoring state.`);

    if (!confirmed) {
      return;
    }

    setDeletingRunId(run.runId);
    setError(null);
    setMessage(null);

    try {
      await api.deleteRun(run.runId);
      setMessage(`Deleted run ${run.runId}.`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeletingRunId(null);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageHeader">
        <div>
          <h1>Runs</h1>
          <p className="muted">Run status from the control plane.</p>
        </div>

        <button type="button" className="secondaryButton" onClick={() => void load()}>
          Refresh
        </button>
      </div>

      {error && <LoadingError message={error} />}
      {message && <div className="successBanner">{message}</div>}

      <Card title="Stored Runs">
        {loading ? (
          <p className="muted">Loading runs…</p>
        ) : runs.length === 0 ? (
          <EmptyState title="No runs yet" />
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Job</th>
                  <th>Status</th>
                  <th>Dry Run</th>
                  <th>Created</th>
                  <th>Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {runs.map(run => (
                  <tr key={run.runId}>
                    <td>
                      <button className="linkButton" type="button" onClick={() => openRun(run.runId)}>
                        {run.jobName}
                      </button>
                      <br />
                      <small>{run.runId}</small>
                    </td>
                    <td><StatusPill status={run.status} /></td>
                    <td>{run.dryRun ? "Yes" : "No"}</td>
                    <td>{new Date(run.createdUtc).toLocaleString()}</td>
                    <td>{new Date(run.updatedUtc).toLocaleString()}</td>
                    <td>
                      <div className="inlineActions">
                        <button type="button" onClick={() => openRun(run.runId)}>Open</button>
                        <button
                          type="button"
                          className="dangerButton"
                          onClick={() => void deleteRun(run)}
                          disabled={deletingRunId === run.runId}
                        >
                          {deletingRunId === run.runId ? "Deleting…" : "Delete"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}