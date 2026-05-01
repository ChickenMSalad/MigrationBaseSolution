import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, ProjectRecord, RunRecord } from "../types/api";

function artifactDownloadUrl(artifactId: string) {
  return `/api/artifacts/${encodeURIComponent(artifactId)}/download`;
}

function artifactKind(artifact: ArtifactRecord): string {
  const value = artifact.kind ?? artifact.artifactType;

  if (value === null || value === undefined) {
    return "Unknown";
  }

  return String(value);
}

function artifactCreatedUtc(artifact: ArtifactRecord): string | null {
  return artifact.createdUtc ?? artifact.uploadedUtc ?? null;
}

function formatDate(value?: string | null) {
  if (!value) {
    return "Unknown";
  }

  return new Date(value).toLocaleString();
}

function ArtifactSummary(props: {
  label: string;
  artifact: ArtifactRecord | null | undefined;
}) {
  const artifact = props.artifact;

  if (!artifact) {
    return (
      <div className="projectArtifactSummary empty">
        <strong>{props.label}</strong>
        <span>No artifact selected</span>
      </div>
    );
  }

  return (
    <div className="projectArtifactSummary">
      <strong>{props.label}</strong>
      <span>{artifact.fileName}</span>
      <small>{artifactKind(artifact)} · {formatDate(artifactCreatedUtc(artifact))}</small>
      <a href={artifactDownloadUrl(artifact.artifactId)}>Download</a>
    </div>
  );
}

export function ProjectDetail() {
  const navigate = useNavigate();
  const { projectId: routeProjectId } = useParams();
  const projectId = routeProjectId ?? "";

  const [project, setProject] = useState<ProjectRecord | null>(null);
  const [manifestArtifacts, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [mappingArtifacts, setMappingArtifacts] = useState<ArtifactRecord[]>([]);
  const [runs, setRuns] = useState<RunRecord[]>([]);
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
  const [message, setMessage] = useState<string | null>(null);

  const openRun = (runId: string) => navigate("/runs/" + encodeURIComponent(runId));
  const openPreflight = () => navigate("/projects/" + encodeURIComponent(projectId) + "/preflight");
  const back = () => navigate("/projects");

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const [projectResult, manifests, mappings, runResults] = await Promise.all([
        api.project(projectId),
        api.artifacts("Manifest"),
        api.artifacts("Mapping"),
        api.runs()
      ]);

      setProject(projectResult);
      setManifestArtifacts(manifests);
      setMappingArtifacts(mappings);
      setRuns(runResults.filter(run => run.projectId === projectId));
      setManifestArtifactId(projectResult.manifestArtifactId ?? "");
      setMappingArtifactId(projectResult.mappingArtifactId ?? "");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectId]);

  const selectedManifest = useMemo(
    () => manifestArtifacts.find(artifact => artifact.artifactId === manifestArtifactId) ?? null,
    [manifestArtifacts, manifestArtifactId]
  );

  const selectedMapping = useMemo(
    () => mappingArtifacts.find(artifact => artifact.artifactId === mappingArtifactId) ?? null,
    [mappingArtifacts, mappingArtifactId]
  );

async function bindArtifacts() {
  setBinding(true);
  setError(null);
  setMessage(null);

  try {
    const updated = await api.bindProjectArtifacts(projectId, {
      manifestArtifactId: manifestArtifactId || null,
      mappingArtifactId: mappingArtifactId || null
    });

    setProject(updated);
    setManifestArtifactId(updated.manifestArtifactId ?? "");
    setMappingArtifactId(updated.mappingArtifactId ?? "");
    setMessage("Project artifact references updated.");
    await load();
  } catch (err) {
    setError(err instanceof Error ? err.message : String(err));
  } finally {
    setBinding(false);
  }
}

  async function startRun() {
    setSaving(true);
    setError(null);
    setMessage(null);

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

  if (loading && !project) {
    return (
      <div className="pageStack projectDetailPage">
        <p className="muted">Loading project…</p>
      </div>
    );
  }

  return (
    <div className="pageStack projectDetailPage">
      <div className="pageHeader projectDetailHeader">
        <div>
          <button className="linkButton" type="button" onClick={back}>
            ← Projects
          </button>

          <h1>{project?.displayName ?? "Project"}</h1>

          <p className="muted">
            {project?.sourceType ?? "Source"} → {project?.targetType ?? "Target"} · {projectId}
          </p>
        </div>

        <div className="buttonRow projectDetailHeaderActions">
          <button className="secondaryButton" type="button" onClick={openPreflight}>
            Run Preflight
          </button>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      {message && (
        <div className="successBanner">
          {message}
        </div>
      )}

      {project && (
        <Card title="Project Snapshot">
          <dl className="projectSnapshotGrid">
            <div>
              <dt>Project ID</dt>
              <dd>{project.projectId}</dd>
            </div>

            <div>
              <dt>Source</dt>
              <dd>{project.sourceType}</dd>
            </div>

            <div>
              <dt>Target</dt>
              <dd>{project.targetType}</dd>
            </div>

            <div>
              <dt>Manifest type</dt>
              <dd>{project.manifestType}</dd>
            </div>

            <div>
              <dt>Updated</dt>
              <dd>{formatDate(project.updatedUtc)}</dd>
            </div>
          </dl>
        </Card>
      )}

      <Card title="Migration Workspace">
        <p className="muted">
          A project groups the artifacts and decisions used to move from source to target.
        </p>

        <div className="projectArtifactGrid">
          <label>
            Manifest artifact
            <select value={manifestArtifactId} onChange={event => setManifestArtifactId(event.target.value)}>
              <option value="">No project manifest artifact</option>
              {manifestArtifacts.map(artifact => (
                <option key={artifact.artifactId} value={artifact.artifactId}>
                  {artifact.fileName}
                </option>
              ))}
            </select>
          </label>

          <label>
            Mapping artifact
            <select value={mappingArtifactId} onChange={event => setMappingArtifactId(event.target.value)}>
              <option value="">No project mapping artifact</option>
              {mappingArtifacts.map(artifact => (
                <option key={artifact.artifactId} value={artifact.artifactId}>
                  {artifact.fileName}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="projectArtifactSummaryGrid">
          <ArtifactSummary label="Selected manifest" artifact={selectedManifest} />
          <ArtifactSummary label="Selected mapping" artifact={selectedMapping} />
        </div>

        <div className="buttonRow">
          <button
            className="primaryButton"
            type="button"
            onClick={() => void bindArtifacts()}
            disabled={binding}
          >
            {binding ? "Binding…" : "Save Artifact References"}
          </button>

          <button className="secondaryButton" type="button" onClick={openPreflight}>
            Run Preflight
          </button>
        </div>
      </Card>

      <Card title="Run Setup">
        <p className="muted">
          Runs consume the project artifact references by default. Use path overrides only for local debugging.
        </p>

        <div className="formGrid">
          <label>
            Job name
            <input value={jobName} onChange={event => setJobName(event.target.value)} />
          </label>

          <label>
            Manifest path override
            <input
              value={manifestPath}
              onChange={event => setManifestPath(event.target.value)}
              placeholder="Optional when a manifest artifact is selected"
            />
          </label>

          <label>
            Mapping profile path override
            <input
              value={mappingProfilePath}
              onChange={event => setMappingProfilePath(event.target.value)}
              placeholder="Optional when a mapping artifact is selected"
            />
          </label>

          <label>
            Parallelism
            <input
              type="number"
              min={1}
              value={parallelism}
              onChange={event => setParallelism(Number(event.target.value || 1))}
            />
          </label>

          <label className="checkboxRow">
            <input
              type="checkbox"
              checked={dryRun}
              onChange={event => setDryRun(event.target.checked)}
            />
            Dry run
          </label>
        </div>

        <div className="buttonRow">
          <button className="secondaryButton" type="button" onClick={openPreflight}>
            Preflight First
          </button>

          <button
            className="primaryButton"
            type="button"
            onClick={() => void startRun()}
            disabled={saving}
          >
            {saving ? "Queueing…" : "Queue Run"}
          </button>
        </div>
      </Card>

      <Card title="Runs For This Project">
        {runs.length === 0 ? (
          <p className="muted">No runs have been queued for this project yet.</p>
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Run ID</th>
                  <th>Job</th>
                  <th>Dry Run</th>
                  <th>Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {runs.map(run => (
                  <tr key={run.runId}>
                    <td>{run.status}</td>
                    <td><small>{run.runId}</small></td>
                    <td>{run.jobName}</td>
                    <td>{run.dryRun ? "Yes" : "No"}</td>
                    <td>{formatDate(run.updatedUtc)}</td>
                    <td>
                      <button type="button" onClick={() => openRun(run.runId)}>
                        View
                      </button>
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