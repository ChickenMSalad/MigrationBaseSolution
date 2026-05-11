import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { Card, EmptyState, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, PreflightResult, ProjectRecord } from "../types/api";

function statusClass(status?: string) {
  const value = (status ?? "").toLowerCase();
  if (value.includes("fail")) return "pill bad";
  if (value.includes("warn")) return "pill warn";
  if (value.includes("pass")) return "pill good";
  return "pill neutral";
}

export function Preflight() {
  const navigate = useNavigate();
  const { projectId: routeProjectId } = useParams();
  const projectId = routeProjectId ?? "";

  const [project, setProject] = useState<ProjectRecord | null>(null);
  const [manifestArtifacts, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [mappingArtifacts, setMappingArtifacts] = useState<ArtifactRecord[]>([]);
  const [jobName, setJobName] = useState("platform-smoke-preflight");
  const [manifestPath, setManifestPath] = useState("");
  const [mappingProfilePath, setMappingProfilePath] = useState("");
  const [manifestArtifactId, setManifestArtifactId] = useState("");
  const [mappingArtifactId, setMappingArtifactId] = useState("");
  const [validateSourceSample, setValidateSourceSample] = useState(false);
  const [sourceSampleSize, setSourceSampleSize] = useState(0);
  const [maxRows, setMaxRows] = useState(250);
  const [result, setResult] = useState<PreflightResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
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
      setJobName(`${projectResult.displayName}-preflight`.replace(/\s+/g, "-").toLowerCase());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [projectId]);

  const visibleIssues = useMemo(() => result?.issues ?? [], [result]);

  async function runPreflight() {
    setRunning(true);
    setError(null);
    setResult(null);
    try {
      const preflight = await api.runPreflight(projectId, {
        jobName,
        manifestPath: manifestPath.trim() || null,
        mappingProfilePath: mappingProfilePath.trim() || null,
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null,
        settings: {
          "Preflight:MaxRows": String(maxRows),
          "Preflight:ValidateSourceSample": String(validateSourceSample),
          "Preflight:SourceSampleSize": String(sourceSampleSize)
        }
      });
      setResult(preflight);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setRunning(false);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <button className="ghost" onClick={() => navigate(`/projects/${encodeURIComponent(projectId)}`)}>← Project</button>
          <h1>Preflight</h1>
          <p>{project?.displayName ?? projectId}</p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />

      <Card title="Preflight inputs" subtitle="Validate manifest, mapping, required target fields, and optional source samples without queueing a worker run or uploading assets.">
        <div className="formGrid wide">
          <label>Job name<input value={jobName} onChange={(e) => setJobName(e.target.value)} /></label>
          <label>
            Manifest artifact
            <select value={manifestArtifactId} onChange={(e) => setManifestArtifactId(e.target.value)}>
              <option value="">No manifest artifact</option>
              {manifestArtifacts.map(a => <option key={a.artifactId} value={a.artifactId}>{a.fileName}</option>)}
            </select>
          </label>
          <label>
            Mapping artifact
            <select value={mappingArtifactId} onChange={(e) => setMappingArtifactId(e.target.value)}>
              <option value="">No mapping artifact</option>
              {mappingArtifacts.map(a => <option key={a.artifactId} value={a.artifactId}>{a.fileName}</option>)}
            </select>
          </label>
          <label>Manifest path override<input value={manifestPath} onChange={(e) => setManifestPath(e.target.value)} placeholder="Optional" /></label>
          <label>Mapping path override<input value={mappingProfilePath} onChange={(e) => setMappingProfilePath(e.target.value)} placeholder="Optional" /></label>
          <label>Max rows to check<input type="number" min={0} value={maxRows} onChange={(e) => setMaxRows(Number(e.target.value || 0))} /></label>
          <label>Source sample size<input type="number" min={0} value={sourceSampleSize} onChange={(e) => setSourceSampleSize(Number(e.target.value || 0))} /></label>
          <label className="check"><input type="checkbox" checked={validateSourceSample} onChange={(e) => setValidateSourceSample(e.target.checked)} /> Validate source sample</label>
        </div>
        <button className="primary" onClick={runPreflight} disabled={running}>{running ? "Running…" : "Run Preflight"}</button>
      </Card>

      {result && (
        <Card title="Preflight result" subtitle={result.preflightId}>
          <div className="metricGrid">
            <div className="metric"><span>Status</span><strong><span className={statusClass(result.status)}>{result.status}</span></strong></div>
            <div className="metric"><span>Total rows</span><strong>{result.summary.totalRows}</strong></div>
            <div className="metric"><span>Checked</span><strong>{result.summary.checkedRows}</strong></div>
            <div className="metric"><span>Errors</span><strong>{result.summary.errorCount}</strong></div>
            <div className="metric"><span>Warnings</span><strong>{result.summary.warningCount}</strong></div>
          </div>
        </Card>
      )}

      {result && (
        <Card title="Issues">
          {visibleIssues.length === 0 ? <EmptyState title="No issues found" message="This preflight passed without warnings or errors." /> : (
            <table>
              <thead><tr><th>Severity</th><th>Code</th><th>Row</th><th>Field</th><th>Message</th></tr></thead>
              <tbody>
                {visibleIssues.map((issue, index) => (
                  <tr key={`${issue.code}-${index}`}>
                    <td><span className={statusClass(issue.severity)}>{issue.severity}</span></td>
                    <td>{issue.code}</td>
                    <td>{issue.rowId ?? ""}</td>
                    <td>{issue.field ?? ""}</td>
                    <td>{issue.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>
      )}

      {result && <Card title="Raw result"><JsonBlock value={result} /></Card>}
    </div>
  );
}
