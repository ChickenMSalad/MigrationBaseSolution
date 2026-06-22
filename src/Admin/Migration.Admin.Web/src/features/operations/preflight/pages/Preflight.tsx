import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../../../../api/client";
import { apiGet } from "../../../../api/core/adminApiClient";
import { runProjectPreflight } from "../../../../api/preflight";
import { Card, EmptyState, JsonBlock } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type { ArtifactRecord, PreflightResult, ProjectRecord } from "../../../../types/api";

type CheckStatus = "pass" | "warn" | "fail" | "unknown";

type ReadinessCheck = {
  name: string;
  status: CheckStatus;
  message: string;
  detail?: unknown;
};

type OperationalReadiness = {
  loadedAtUtc: string;
  checks: ReadinessCheck[];
};

function statusClass(status?: string) {
  const value = (status ?? "").toLowerCase();
  if (value.includes("fail") || value.includes("unhealthy")) return "pill bad";
  if (value.includes("warn") || value.includes("degraded")) return "pill warn";
  if (value.includes("pass") || value.includes("healthy") || value.includes("ready")) return "pill good";
  return "pill neutral";
}

function toStatus(value: unknown): CheckStatus {
  const text = String(value ?? "").toLowerCase();
  if (text.includes("fail") || text.includes("unhealthy") || text.includes("missing")) return "fail";
  if (text.includes("warn") || text.includes("degraded")) return "warn";
  if (text.includes("pass") || text.includes("healthy") || text.includes("ready") || text.includes("ok")) return "pass";
  return "unknown";
}

function summarizePayload(payload: unknown) {
  if (!payload || typeof payload !== "object") return String(payload ?? "No response payload");
  const record = payload as Record<string, unknown>;
  const status = record.status ?? record.overallStatus ?? record.state ?? record.result;
  const message = record.message ?? record.summary ?? record.description ?? record.reason;
  const parts = [status, message].filter(value => value !== undefined && value !== null && String(value).trim() !== "");
  return parts.length > 0 ? parts.map(String).join(" - ") : "Response received";
}

async function tryLoadCheck(name: string, path: string): Promise<ReadinessCheck> {
  try {
    const payload = await apiGet<unknown>(path);
    return {
      name,
      status: toStatus(summarizePayload(payload)),
      message: summarizePayload(payload),
      detail: payload,
    };
  } catch (error) {
    return {
      name,
      status: "fail",
      message: error instanceof Error ? error.message : String(error),
    };
  }
}

async function loadOperationalReadiness(): Promise<OperationalReadiness> {
  const checks = await Promise.all([
    tryLoadCheck("API liveness", "/api/operational/health/live"),
    tryLoadCheck("API readiness", "/api/operational/health/ready"),
    tryLoadCheck("SQL runtime backbone", "/api/operational/sql/readiness"),
  ]);

  return {
    loadedAtUtc: new Date().toISOString(),
    checks,
  };
}

function overallReadiness(readiness: OperationalReadiness | null) {
  if (!readiness) return "unknown";
  if (readiness.checks.some(check => check.status === "fail")) return "fail";
  if (readiness.checks.some(check => check.status === "warn" || check.status === "unknown")) return "warn";
  return "pass";
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
  const [readiness, setReadiness] = useState<OperationalReadiness | null>(null);
  const [loading, setLoading] = useState(true);
  const [readinessLoading, setReadinessLoading] = useState(false);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function refreshReadiness() {
    setReadinessLoading(true);
    try {
      setReadiness(await loadOperationalReadiness());
    } finally {
      setReadinessLoading(false);
    }
  }

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [projectResult, manifests, mappings, readinessResult] = await Promise.all([
        api.project(projectId),
        api.artifacts("Manifest"),
        api.artifacts("Mapping"),
        loadOperationalReadiness(),
      ]);

      setProject(projectResult);
      setManifestArtifacts(manifests);
      setMappingArtifacts(mappings);
      setReadiness(readinessResult);
      setManifestArtifactId(projectResult.manifestArtifactId ?? "");
      setMappingArtifactId(projectResult.mappingArtifactId ?? "");
      setJobName(`${projectResult.displayName}-preflight`.replace(/\s+/g, "-").toLowerCase());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [projectId]);

  const visibleIssues = useMemo(() => result?.issues ?? [], [result]);
  const readinessStatus = overallReadiness(readiness);

  async function runPreflight() {
    setRunning(true);
    setError(null);
    setResult(null);

    try {
      await refreshReadiness();
      const preflight = await runProjectPreflight(projectId, {
        jobName,
        manifestPath: manifestPath.trim() || null,
        mappingProfilePath: mappingProfilePath.trim() || null,
        manifestArtifactId: manifestArtifactId || null,
        mappingArtifactId: mappingArtifactId || null,
        settings: {
          "Preflight:MaxRows": String(maxRows),
          "Preflight:ValidateSourceSample": String(validateSourceSample),
          "Preflight:SourceSampleSize": String(sourceSampleSize),
        },
      });
      setResult(preflight);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setRunning(false);
    }
  }

  if (loading) {
    return <p>Loading preflight...</p>;
  }

  return (
    <div className="stack">
      <button className="link-button" type="button" onClick={() => navigate(`/projects/${encodeURIComponent(projectId)}`)}>
        &lt;- Project
      </button>

      <header className="page-header">
        <div>
          <h1>Preflight</h1>
          <p>{project?.displayName ?? projectId}</p>
        </div>
      </header>

      {error && <LoadingError message={error} onRetry={() => void load()} />}

      <Card
        title="Operational readiness"
        subtitle="Checks the API, runtime readiness, and SQL runtime backbone. Readiness is advisory and does not block preflight or migration execution."
        action={
          <button type="button" onClick={() => void refreshReadiness()} disabled={readinessLoading}>
            {readinessLoading ? "Refreshing..." : "Refresh readiness"}
          </button>
        }
      >
        <div className="metric-grid">
          <div className="metric-card">
            <span className="metric-label">Overall</span>
            <strong className={statusClass(readinessStatus)}>{readinessStatus}</strong>
          </div>
          <div className="metric-card">
            <span className="metric-label">Checked</span>
            <strong>{readiness?.loadedAtUtc ? new Date(readiness.loadedAtUtc).toLocaleString() : "Not loaded"}</strong>
          </div>
        </div>

        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Check</th>
                <th>Status</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {(readiness?.checks ?? []).map(check => (
                <tr key={check.name}>
                  <td>{check.name}</td>
                  <td><span className={statusClass(check.status)}>{check.status}</span></td>
                  <td>{check.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      <Card title="Run project preflight">
        <div className="form-grid">
          <label>
            Job name
            <input value={jobName} onChange={event => setJobName(event.target.value)} />
          </label>

          <label>
            Manifest artifact
            <select value={manifestArtifactId} onChange={event => setManifestArtifactId(event.target.value)}>
              <option value="">No manifest artifact</option>
              {manifestArtifacts.map(artifact => (
                <option key={artifact.artifactId} value={artifact.artifactId}>{artifact.fileName}</option>
              ))}
            </select>
          </label>

          <label>
            Mapping artifact
            <select value={mappingArtifactId} onChange={event => setMappingArtifactId(event.target.value)}>
              <option value="">No mapping artifact</option>
              {mappingArtifacts.map(artifact => (
                <option key={artifact.artifactId} value={artifact.artifactId}>{artifact.fileName}</option>
              ))}
            </select>
          </label>

          <label>
            Manifest path override
            <input value={manifestPath} onChange={event => setManifestPath(event.target.value)} placeholder="Optional" />
          </label>

          <label>
            Mapping path override
            <input value={mappingProfilePath} onChange={event => setMappingProfilePath(event.target.value)} placeholder="Optional" />
          </label>

          <label>
            Max rows to check
            <input type="number" value={maxRows} onChange={event => setMaxRows(Number(event.target.value || 0))} />
          </label>

          <label>
            Source sample size
            <input type="number" value={sourceSampleSize} onChange={event => setSourceSampleSize(Number(event.target.value || 0))} />
          </label>

          <label className="checkbox-row">
            <input type="checkbox" checked={validateSourceSample} onChange={event => setValidateSourceSample(event.target.checked)} />
            Validate source sample
          </label>
        </div>

        <div className="actions-row">
          <button type="button" onClick={() => void runPreflight()} disabled={running}>
            {running ? "Running..." : "Run Preflight"}
          </button>
          {readinessStatus === "fail" && <span className="muted">Readiness has failures, but preflight remains available.</span>}
        </div>
      </Card>

      {result && (
        <Card title="Preflight result">
          <div className="metric-grid">
            <div className="metric-card"><span className="metric-label">Status</span><strong className={statusClass(result.status)}>{result.status}</strong></div>
            <div className="metric-card"><span className="metric-label">Total rows</span><strong>{result.summary?.totalRows ?? 0}</strong></div>
            <div className="metric-card"><span className="metric-label">Checked</span><strong>{result.summary?.checkedRows ?? 0}</strong></div>
            <div className="metric-card"><span className="metric-label">Errors</span><strong>{result.summary?.errorCount ?? 0}</strong></div>
            <div className="metric-card"><span className="metric-label">Warnings</span><strong>{result.summary?.warningCount ?? 0}</strong></div>
          </div>

          {visibleIssues.length === 0 ? (
            <EmptyState title="No issues" message="Preflight did not report validation issues." />
          ) : (
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Severity</th>
                    <th>Code</th>
                    <th>Row</th>
                    <th>Field</th>
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  {visibleIssues.map((issue, index) => (
                    <tr key={`${issue.code ?? "issue"}-${index}`}>
                      <td><span className={statusClass(issue.severity!)}>{issue.severity}</span></td>
                      <td>{issue.code}</td>
                      <td>{issue.rowId ?? ""}</td>
                      <td>{issue.field ?? ""}</td>
                      <td>{issue.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <JsonBlock value={result} />
        </Card>
      )}
    </div>
  );
}
