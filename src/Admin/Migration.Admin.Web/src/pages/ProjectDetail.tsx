import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { Card, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, CredentialSetSummary, ProjectRecord } from "../types/api";

function normalized(value: string | null | undefined) {
  return String(value ?? "").trim().toLowerCase();
}

function credentialMatches(credential: CredentialSetSummary, connectorType: string, connectorRole: "Source" | "Target") {
  return (
    normalized(credential.connectorRole) === normalized(connectorRole) &&
    normalized(credential.connectorType) === normalized(connectorType)
  );
}

function projectSetting(project: ProjectRecord | null, key: string) {
  if (!project) return "";

  if (key === "sourceCredentialSetId" && project.sourceCredentialSetId) {
    return project.sourceCredentialSetId;
  }

  if (key === "targetCredentialSetId" && project.targetCredentialSetId) {
    return project.targetCredentialSetId;
  }

  return project.settings?.[key] ?? "";
}

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
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);

  const [jobName, setJobName] = useState("platform-smoke-localstorage-realrun");
  const [manifestPath, setManifestPath] = useState("");
  const [mappingProfilePath, setMappingProfilePath] = useState("");
  const [manifestArtifactId, setManifestArtifactId] = useState("");
  const [mappingArtifactId, setMappingArtifactId] = useState("");
  const [sourceCredentialSetId, setSourceCredentialSetId] = useState("");
  const [targetCredentialSetId, setTargetCredentialSetId] = useState("");
  const [dryRun, setDryRun] = useState(false);
  const [forceRerun, setForceRerun] = useState(false);
  const [parallelism, setParallelism] = useState(1);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [bindingArtifacts, setBindingArtifacts] = useState(false);
  const [bindingCredentials, setBindingCredentials] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const [projectResult, manifests, mappings, credentialResult] = await Promise.all([
        api.project(projectId),
        api.artifacts("Manifest"),
        api.artifacts("Mapping"),
        api.credentials()
      ]);

      setProject(projectResult);
      setManifestArtifacts(manifests);
      setMappingArtifacts(mappings);
      setCredentials(credentialResult);
      setManifestArtifactId(projectResult.manifestArtifactId ?? "");
      setMappingArtifactId(projectResult.mappingArtifactId ?? "");
      setSourceCredentialSetId(projectSetting(projectResult, "sourceCredentialSetId"));
      setTargetCredentialSetId(projectSetting(projectResult, "targetCredentialSetId"));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (projectId) void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectId]);

  const selectedManifest = useMemo(
    () => manifestArtifacts.find(x => x.artifactId === manifestArtifactId),
    [manifestArtifacts, manifestArtifactId]
  );

  const selectedMapping = useMemo(
    () => mappingArtifacts.find(x => x.artifactId === mappingArtifactId),
    [mappingArtifacts, mappingArtifactId]
  );

  const sourceCredentials = useMemo(
    () => project ? credentials.filter(c => credentialMatches(c, project.sourceType, "Source")) : [],
    [credentials, project]
  );

  const targetCredentials = useMemo(
    () => project ? credentials.filter(c => credentialMatches(c, project.targetType, "Target")) : [],
    [credentials, project]
  );

  const selectedSourceCredential = useMemo(
    () => credentials.find(x => x.credentialSetId === sourceCredentialSetId),
    [credentials, sourceCredentialSetId]
  );

  const selectedTargetCredential = useMemo(
    () => credentials.find(x => x.credentialSetId === targetCredentialSetId),
    [credentials, targetCredentialSetId]
  );

  const missingCredentials = !sourceCredentialSetId || !targetCredentialSetId;

  async function bindArtifacts() {
    setBindingArtifacts(true);
    setError(null);
    setMessage(null);

    try {
      const updated = await api.bindProjectArtifacts(projectId, {
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null
      });

      setProject(updated);
      setMessage("Project artifacts saved.");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBindingArtifacts(false);
    }
  }

  async function bindCredentials() {
    if (!project) return;

    setBindingCredentials(true);
    setError(null);
    setMessage(null);

    try {
      const updated = await api.bindProjectCredentials(projectId, {
        sourceCredentialSetId: sourceCredentialSetId || null,
        targetCredentialSetId: targetCredentialSetId || null
      });

      setProject(updated);
      setSourceCredentialSetId(projectSetting(updated, "sourceCredentialSetId"));
      setTargetCredentialSetId(projectSetting(updated, "targetCredentialSetId"));
      setMessage("Project credentials saved.");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBindingCredentials(false);
    }
  }

  async function startRun() {
    if (!project) return;

    setSaving(true);
    setError(null);
    setMessage(null);

    if (missingCredentials) {
      setError("Select and save both source and target credentials before queueing a run.");
      setSaving(false);
      return;
    }

    try {
      const run = await api.createRun(projectId, {
        jobName,
        manifestPath: manifestPath.trim() || null,
        mappingProfilePath: mappingProfilePath.trim() || null,
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null,
        dryRun,
        parallelism,
    settings: {
      ...(project.settings ?? {}),
      sourceCredentialSetId,
      targetCredentialSetId,
      ForceRerun: forceRerun ? "true" : "false"
    }

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
      {message && <div className="notice">{message}</div>}

      {project && <Card title="Project record"><JsonBlock value={project} /></Card>}

      {project && (
        <Card title="Project credentials" subtitle="Bind the credential sets that this source → target project should use for preflight and runs.">
          <div className="formGrid wide">
            <label>
              Source credentials ({project.sourceType})
              <select value={sourceCredentialSetId} onChange={(e) => setSourceCredentialSetId(e.target.value)}>
                <option value="">No source credentials selected</option>
                {sourceCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName} ({credential.credentialSetId})
                  </option>
                ))}
              </select>
            </label>

            <label>
              Target credentials ({project.targetType})
              <select value={targetCredentialSetId} onChange={(e) => setTargetCredentialSetId(e.target.value)}>
                <option value="">No target credentials selected</option>
                {targetCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName} ({credential.credentialSetId})
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="muted">
            Selected source credentials: {selectedSourceCredential?.displayName ?? "none"}<br />
            Selected target credentials: {selectedTargetCredential?.displayName ?? "none"}
          </div>

          {sourceCredentials.length === 0 && (
            <p className="muted">No Source credential set exists for {project.sourceType}. Create one from the Credentials page first.</p>
          )}

          {targetCredentials.length === 0 && (
            <p className="muted">No Target credential set exists for {project.targetType}. Create one from the Credentials page first.</p>
          )}

          <button className="primary" onClick={bindCredentials} disabled={bindingCredentials}>
            {bindingCredentials ? "Saving…" : "Save Project Credentials"}
          </button>
        </Card>
      )}

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
          <button className="primary" onClick={bindArtifacts} disabled={bindingArtifacts}>
            {bindingArtifacts ? "Binding…" : "Bind Artifacts To Project"}
          </button>
          <button className="ghost" onClick={openPreflight}>Run Preflight</button>
        </div>
      </Card>

      <Card title="Start run" subtitle="Runs require saved source and target credentials. Prefer artifact IDs; raw paths remain available for local operator workflows.">
        <div className="formGrid wide">
          <label>Job name<input value={jobName} onChange={(e) => setJobName(e.target.value)} /></label>
          <label>Manifest path override<input value={manifestPath} onChange={(e) => setManifestPath(e.target.value)} placeholder="Optional when a manifest artifact is selected" /></label>
          <label>Mapping profile path override<input value={mappingProfilePath} onChange={(e) => setMappingProfilePath(e.target.value)} placeholder="Optional when a mapping artifact is selected" /></label>
          <label>Parallelism<input type="number" min={1} value={parallelism} onChange={(e) => setParallelism(Number(e.target.value || 1))} /></label>
          <label className="check"><input type="checkbox" checked={dryRun} onChange={(e) => setDryRun(e.target.checked)} /> Dry run</label>
    <label className="check"><input type="checkbox" checked={forceRerun} onChange={e => setForceRerun(e.target.checked)} /> Force re-run completed work items</label>
        </div>

        {missingCredentials && (
          <p className="muted">Save source and target credentials above before queueing a run.</p>
        )}

        <div className="actionRow">
          <button className="ghost" onClick={openPreflight}>Preflight First</button>
          <button className="primary" onClick={startRun} disabled={saving || missingCredentials}>
            {saving ? "Queueing…" : "Queue Run"}
          </button>
        </div>
      </Card>
    </div>
  );
}
