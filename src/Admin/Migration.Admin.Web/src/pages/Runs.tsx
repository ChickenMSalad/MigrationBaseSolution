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

  return (
    <div className="pageStack">
      <div className="pageTitle"><div><h1>Runs</h1><p>Run status from the control plane.</p></div><button onClick={load}>Refresh</button></div>
      <LoadingError loading={loading} error={error} />
      <Card>
        {runs.length === 0 ? <EmptyState title="No runs yet" /> : (
          <table>
            <thead><tr><th>Job</th><th>Status</th><th>Dry Run</th><th>Created</th><th>Updated</th></tr></thead>
            <tbody>
              {runs.map((run) => (
                <tr key={run.runId}>
                  <td><button className="linkButton" onClick={() => openRun(run.runId)}>{run.jobName}</button><div className="muted mono">{run.runId}</div></td>
                  <td><StatusPill status={run.status} /></td>
                  <td>{run.dryRun ? "Yes" : "No"}</td>
                  <td>{new Date(run.createdUtc).toLocaleString()}</td>
                  <td>{new Date(run.updatedUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
