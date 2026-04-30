import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { Card, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ProjectRecord, RunRecord } from "../types/api";

export function Dashboard() {
  const navigate = useNavigate();
  const openRun = (runId: string) => navigate("/runs/" + encodeURIComponent(runId));
  const openProject = (projectId: string) => navigate("/projects/" + encodeURIComponent(projectId));
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [runs, setRuns] = useState<RunRecord[]>([]);
  const [health, setHealth] = useState<string>("unknown");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [healthResult, projectResult, runResult] = await Promise.all([api.health(), api.projects(), api.runs()]);
        if (!active) return;
        setHealth(healthResult.status);
        setProjects(projectResult);
        setRuns(runResult);
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : String(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void load();
    const timer = window.setInterval(load, 10000);
    return () => {
      active = false;
      window.clearInterval(timer);
    };
  }, []);

  const activeRuns = runs.filter((r) => ["Queued", "PreflightQueued", "Running"].includes(r.status)).length;
  const failedRuns = runs.filter((r) => r.status.toLowerCase().includes("fail")).length;

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <h1>Dashboard</h1>
          <p>Local operator view for the migration control plane.</p>
        </div>
        <StatusPill status={`API ${health}`} />
      </div>
      <LoadingError loading={loading} error={error} />
      <div className="metricGrid">
        <Card><div className="metric"><span>Projects</span><strong>{projects.length}</strong></div></Card>
        <Card><div className="metric"><span>Total runs</span><strong>{runs.length}</strong></div></Card>
        <Card><div className="metric"><span>Active runs</span><strong>{activeRuns}</strong></div></Card>
        <Card><div className="metric"><span>Failed runs</span><strong>{failedRuns}</strong></div></Card>
      </div>
      <Card title="Recent runs" subtitle="Auto-refreshes every 10 seconds.">
        <table>
          <thead><tr><th>Run</th><th>Project</th><th>Status</th><th>Updated</th></tr></thead>
          <tbody>
            {runs.slice(0, 8).map((run) => (
              <tr key={run.runId}>
                <td><button className="linkButton" onClick={() => openRun(run.runId)}>{run.jobName}</button></td>
                <td><button className="linkButton" onClick={() => openProject(run.projectId)}>{run.projectId}</button></td>
                <td><StatusPill status={run.status} /></td>
                <td>{new Date(run.updatedUtc).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
