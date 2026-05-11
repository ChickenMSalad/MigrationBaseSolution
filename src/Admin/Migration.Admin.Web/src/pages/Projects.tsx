import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, EmptyState } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorsResponse, ConnectorDescriptor, ProjectRecord } from "../types/api";

function uniqueConnectorOptions(connectors: ConnectorDescriptor[], fallback: string) {
  const seen = new Set<string>();
  const result: Array<{ value: string; label: string }> = [];

  for (const connector of connectors) {
    const value = connectorValue(connector) || fallback;
    const key = value.toLowerCase();

    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push({ value, label: displayConnectorName(connector) });
  }

  if (result.length === 0) {
    result.push({ value: fallback, label: fallback });
  }

  return result;
}

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
  const [deletingProjectId, setDeletingProjectId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const [projectResult, connectorResult] = await Promise.all([
        api.projects(),
        api.connectors()
      ]);

      setProjects(projectResult);
      setConnectors(connectorResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const sourceOptions = useMemo(
    () => uniqueConnectorOptions(connectors?.sources ?? [], "LocalStorage"),
    [connectors]
  );

  const targetOptions = useMemo(
    () => uniqueConnectorOptions(connectors?.targets ?? [], "LocalStorage"),
    [connectors]
  );

  const manifestOptions = useMemo(
    () => uniqueConnectorOptions(connectors?.manifestProviders ?? [], "Csv"),
    [connectors]
  );

  async function submit() {
    setSaving(true);
    setError(null);
    setMessage(null);

    try {
      const project = await api.createProject({
        displayName,
        sourceType,
        targetType,
        manifestType
      });

      await load();
      openProject(project.projectId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  async function deleteProject(project: ProjectRecord) {
    const confirmed = window.confirm(`Delete project "${project.displayName}"? This removes the project record but does not delete artifacts.`);

    if (!confirmed) {
      return;
    }

    setDeletingProjectId(project.projectId);
    setError(null);
    setMessage(null);

    try {
      await api.deleteProject(project.projectId);
      setMessage(`Deleted project "${project.displayName}".`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeletingProjectId(null);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageHeader">
        <div>
          <h1>Projects</h1>
          <p className="muted">Create reusable source/target migration workspaces.</p>
        </div>
      </div>

      {error && <LoadingError message={error} />}
      {message && <div className="successBanner">{message}</div>}

      <Card title="Create Project">
        <div className="formGrid">
          <label>
            Display name
            <input value={displayName} onChange={event => setDisplayName(event.target.value)} />
          </label>

          <label>
            Source
            <select value={sourceType} onChange={event => setSourceType(event.target.value)}>
              {sourceOptions.map(option => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label>
            Target
            <select value={targetType} onChange={event => setTargetType(event.target.value)}>
              {targetOptions.map(option => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label>
            Manifest
            <select value={manifestType} onChange={event => setManifestType(event.target.value)}>
              {manifestOptions.map(option => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="buttonRow">
          <button className="primaryButton" type="button" onClick={() => void submit()} disabled={saving}>
            {saving ? "Creating…" : "Create Project"}
          </button>
        </div>
      </Card>

      <Card title="Stored Projects">
        {loading ? (
          <p className="muted">Loading projects…</p>
        ) : projects.length === 0 ? (
          <EmptyState title="No projects yet" />
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Source</th>
                  <th>Target</th>
                  <th>Manifest</th>
                  <th>Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {projects.map(project => (
                  <tr key={project.projectId}>
                    <td>
                      <button className="linkButton" type="button" onClick={() => openProject(project.projectId)}>
                        {project.displayName}
                      </button>
                    </td>
                    <td>{project.sourceType}</td>
                    <td>{project.targetType}</td>
                    <td>{project.manifestType}</td>
                    <td>{new Date(project.updatedUtc).toLocaleString()}</td>
                    <td>
                      <div className="inlineActions">
                        <button type="button" onClick={() => openProject(project.projectId)}>Open</button>
                        <button
                          type="button"
                          className="dangerButton"
                          onClick={() => void deleteProject(project)}
                          disabled={deletingProjectId === project.projectId}
                        >
                          {deletingProjectId === project.projectId ? "Deleting…" : "Delete"}
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