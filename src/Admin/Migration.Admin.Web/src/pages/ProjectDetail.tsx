import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { Card, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, ProjectRecord } from "../types/api";

export function ProjectDetail() {
  const navigate = useNavigate();
  const { projectId: routeProjectId } = useParams();
  const projectId = routeProjectId ?? "";
  const openRun = (runId: string) => navigate("/runs/" + encodeURIComponent(runId));
  const openPreflight = () => navigate("/projects/" + encodeURIComponent(projectId) + "/preflight");
  const back = () => navigate("/projects");
  const [project, setProject] = useState<ProjectRecord | null>(null);
  const [manifestArtifacts, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [mappingArtifacts, setMappingArtifacts] = useState<ArtifactRecord[]>([]);
  const [jobName, setJobName] = useState("platform-smoke-localstorage-realrun");
  const [manifestPath, setManifestPath] = useState("");
  const [mappingProfilePath, setMappingProfilePath] = useState("");
  const [manifestArtifactId, setManifestArtifactId] = useState("");
  const [mappingArtifactId, setMappingArtifactId] = useState("");
  const [dryRun, setDryRun] = useState(false);
  const [parallelism, setParallelism] = useState(1);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [binding, setBinding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [projectResult, manifests, mappings] = await Promise.all([
        api.project(projectId),
        api.artifacts("Manifest"),
        api.artifacts("Mapping")
      ]);

      setProject(projectResult);
      setManifestArtifacts(manifests);
      setMappingArtifacts(mappings);
      setManifestArtifactId(projectResult.manifestArtifactId ?? "");
      setMappingArtifactId(projectResult.mappingArtifactId ?? "");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [projectId]);

  const selectedManifest = useMemo(
    () => manifestArtifacts.find(x => x.artifactId === manifestArtifactId),
    [manifestArtifacts, manifestArtifactId]
  );

  const selectedMapping = useMemo(
    () => mappingArtifacts.find(x => x.artifactId === mappingArtifactId),
    [mappingArtifacts, mappingArtifactId]
  );

  async function bindArtifacts() {
    setBinding(true);
    setError(null);
    try {
      const updated = await api.bindProjectArtifacts(projectId, {
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null
      });
      setProject(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBinding(false);
    }
  }

  async function startRun() {
    setSaving(true);
    setError(null);
    try {
      const run = await api.createRun(projectId, {
        jobName,
        manifestPath: manifestPath.trim() || null,
        mappingProfilePath: mappingProfilePath.trim() || null,
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null,
        dryRun,
        parallelism
      });
      openRun(run.runId);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <button className="ghost" onClick={back}>← Projects</button>
          <h1>{project?.displayName ?? "Project"}</h1>
          <p>{projectId}</p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />

      {project && <Card title="Project record"><JsonBlock value={project} /></Card>}

      <Card title="Project artifacts" subtitle="Bind uploaded manifest and mapping artifacts to this project. Raw paths can still be used below as an escape hatch.">
        <div className="formGrid wide">
          <label>
            Manifest artifact
            <select value={manifestArtifactId} onChange={(e) => setManifestArtifactId(e.target.value)}>
              <option value="">No project manifest artifact</option>
              {manifestArtifacts.map(a => (
                <option key={a.artifactId} value={a.artifactId}>{a.fileName} ({a.artifactId})</option>
              ))}
            </select>
          </label>
          <label>
            Mapping artifact
            <select value={mappingArtifactId} onChange={(e) => setMappingArtifactId(e.target.value)}>
              <option value="">No project mapping artifact</option>
              {mappingArtifacts.map(a => (
                <option key={a.artifactId} value={a.artifactId}>{a.fileName} ({a.artifactId})</option>
              ))}
            </select>
          </label>
        </div>
        <div className="muted">
          Selected manifest: {selectedManifest?.fileName ?? "none"}<br />
          Selected mapping: {selectedMapping?.fileName ?? "none"}
        </div>
        <div className="actionRow">
          <button className="primary" onClick={bindArtifacts} disabled={binding}>{binding ? "Binding…" : "Bind Artifacts To Project"}</button>
          <button className="ghost" onClick={openPreflight}>Run Preflight</button>
        </div>
      </Card>

      <Card title="Start run" subtitle="Prefer artifact IDs. Raw paths are still supported for local operator workflows.">
        <div className="formGrid wide">
          <label>Job name<input value={jobName} onChange={(e) => setJobName(e.target.value)} /></label>
          <label>Manifest path override<input value={manifestPath} onChange={(e) => setManifestPath(e.target.value)} placeholder="Optional when a manifest artifact is selected" /></label>
          <label>Mapping profile path override<input value={mappingProfilePath} onChange={(e) => setMappingProfilePath(e.target.value)} placeholder="Optional when a mapping artifact is selected" /></label>
          <label>Parallelism<input type="number" min={1} value={parallelism} onChange={(e) => setParallelism(Number(e.target.value || 1))} /></label>
          <label className="check"><input type="checkbox" checked={dryRun} onChange={(e) => setDryRun(e.target.checked)} /> Dry run</label>
        </div>
        <div className="actionRow">
          <button className="ghost" onClick={openPreflight}>Preflight First</button>
          <button className="primary" onClick={startRun} disabled={saving}>{saving ? "Queueing…" : "Queue Run"}</button>
        </div>
      </Card>
    </div>
  );
}
