import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, EmptyState } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorsResponse, ProjectRecord } from "../types/api";

export function Projects() {
  const navigate = useNavigate();
  const openProject = (projectId: string) => navigate("/projects/" + encodeURIComponent(projectId));
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [connectors, setConnectors] = useState<ConnectorsResponse | null>(null);
  const [displayName, setDisplayName] = useState("Platform Smoke LocalStorage");
  const [sourceType, setSourceType] = useState("LocalStorage");
  const [targetType, setTargetType] = useState("LocalStorage");
  const [manifestType, setManifestType] = useState("Csv");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [projectResult, connectorResult] = await Promise.all([api.projects(), api.connectors()]);
      setProjects(projectResult);
      setConnectors(connectorResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  const sourceOptions = useMemo(() => connectors?.sources ?? [], [connectors]);
  const targetOptions = useMemo(() => connectors?.targets ?? [], [connectors]);
  const manifestOptions = useMemo(() => connectors?.manifestProviders ?? [], [connectors]);

  async function submit() {
    setSaving(true);
    setError(null);
    try {
      const project = await api.createProject({ displayName, sourceType, targetType, manifestType });
      await load();
      openProject(project.projectId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageTitle"><div><h1>Projects</h1><p>Create reusable source/target migration projects.</p></div></div>
      <LoadingError loading={loading} error={error} />

      <Card title="Create project" subtitle="This writes a control-plane project record through the Admin API.">
        <div className="formGrid">
          <label>Display name<input value={displayName} onChange={(e) => setDisplayName(e.target.value)} /></label>
          <label>Source<select value={sourceType} onChange={(e) => setSourceType(e.target.value)}>
            <option value="LocalStorage">LocalStorage</option>
            {sourceOptions.map((x, i) => <option key={i} value={connectorValue(x)}>{displayConnectorName(x)}</option>)}
          </select></label>
          <label>Target<select value={targetType} onChange={(e) => setTargetType(e.target.value)}>
            <option value="LocalStorage">LocalStorage</option>
            {targetOptions.map((x, i) => <option key={i} value={connectorValue(x)}>{displayConnectorName(x)}</option>)}
          </select></label>
          <label>Manifest<select value={manifestType} onChange={(e) => setManifestType(e.target.value)}>
            <option value="Csv">Csv</option>
            {manifestOptions.map((x, i) => <option key={i} value={connectorValue(x)}>{displayConnectorName(x)}</option>)}
          </select></label>
        </div>
        <button className="primary" onClick={submit} disabled={saving}>{saving ? "Creating…" : "Create Project"}</button>
      </Card>

      <Card title="Existing projects">
        {projects.length === 0 ? <EmptyState title="No projects yet" message="Create one above to start testing." /> : (
          <table>
            <thead><tr><th>Name</th><th>Source</th><th>Target</th><th>Manifest</th><th>Updated</th></tr></thead>
            <tbody>
              {projects.map((project) => (
                <tr key={project.projectId}>
                  <td><button className="linkButton" onClick={() => openProject(project.projectId)}>{project.displayName}</button></td>
                  <td>{project.sourceType}</td><td>{project.targetType}</td><td>{project.manifestType}</td>
                  <td>{new Date(project.updatedUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
