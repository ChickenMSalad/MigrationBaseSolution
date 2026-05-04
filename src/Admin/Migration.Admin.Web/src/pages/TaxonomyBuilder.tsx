import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, ConnectorDescriptor, CredentialSetSummary, ProjectRecord } from "../types/api";

type NoticeKind = "success" | "error" | "info";

type PageNotice = {
  kind: NoticeKind;
  message: string;
};

type BuildTaxonomyArtifactRequest = {
  targetType: string;
  credentialSetId: string;
  includeOptions: boolean;
  includeRaw: boolean;
  projectId?: string | null;
  fileName?: string | null;
  description?: string | null;
};

type BuildTaxonomyArtifactResponse = {
  artifact: ArtifactRecord;
  targetType: string;
  fieldCount: number;
  optionCount: number;
  fileName: string;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;

    try {
      const body = await response.json();
      message = body?.error ?? body?.message ?? JSON.stringify(body);
    } catch {
      try {
        message = await response.text();
      } catch {
        // keep default
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function normalizeTargetKind(value: string | null | undefined): string {
  const text = String(value ?? "").trim().toLowerCase();

  if (text.includes("bynder")) return "bynder";
  if (text.includes("cloudinary")) return "cloudinary";
  if (text.includes("aprimo")) return "aprimo";

  return text;
}

function targetKindForConnector(connector: ConnectorDescriptor | null | undefined): string {
  return normalizeTargetKind(
    `${connector?.type ?? ""} ${connector?.name ?? ""} ${connector?.displayName ?? ""}`
  );
}

function targetKindForCredential(credential: CredentialSetSummary): string {
  return normalizeTargetKind(credential.connectorType);
}

function isSupportedTaxonomyTarget(connector: ConnectorDescriptor): boolean {
  const kind = targetKindForConnector(connector);
  return kind === "bynder" || kind === "cloudinary" || kind === "aprimo";
}

function optionKey(connector: ConnectorDescriptor): string {
  return connectorValue(connector) || displayConnectorName(connector);
}

function safeFilePart(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_.-]+/g, "-")
    .replace(/^-+|-+$/g, "") || "taxonomy";
}

function buildDefaultFileName(targetType: string): string {
  const timestamp = new Date()
    .toISOString()
    .replace(/[-:]/g, "")
    .replace(/\.\d{3}Z$/, "Z");

  return `${safeFilePart(targetType)}-taxonomy-${timestamp}.xls`;
}

function noticeClassName(kind: NoticeKind): string {
  return `taxonomyNotice taxonomyNotice--${kind}`;
}

async function buildTaxonomy(payload: BuildTaxonomyArtifactRequest) {
  return request<BuildTaxonomyArtifactResponse>("/api/taxonomy-builder/build", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export function TaxonomyBuilder() {
  const [targets, setTargets] = useState<ConnectorDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [projects, setProjects] = useState<ProjectRecord[]>([]);

  const [targetType, setTargetType] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [projectId, setProjectId] = useState("");
  const [includeOptions, setIncludeOptions] = useState(true);
  const [includeRaw, setIncludeRaw] = useState(true);
  const [fileName, setFileName] = useState("");

  const [loading, setLoading] = useState(true);
  const [building, setBuilding] = useState(false);
  const [notice, setNotice] = useState<PageNotice | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<BuildTaxonomyArtifactResponse | null>(null);

  const selectedTarget = useMemo(
    () => targets.find(target => optionKey(target) === targetType) ?? null,
    [targets, targetType]
  );

  const selectedTargetKind = useMemo(
    () => targetKindForConnector(selectedTarget),
    [selectedTarget]
  );

  const targetCredentials = useMemo(
    () => credentials.filter(credential => credential.connectorRole?.toLowerCase() === "target"),
    [credentials]
  );

  const matchingCredentials = useMemo(() => {
    if (!selectedTargetKind) {
      return targetCredentials;
    }

    const exact = targetCredentials.filter(
      credential => targetKindForCredential(credential) === selectedTargetKind
    );

    return exact.length > 0 ? exact : targetCredentials;
  }, [selectedTargetKind, targetCredentials]);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);

      try {
        const [connectorResult, credentialResult, projectResult] = await Promise.all([
          api.connectors(),
          api.credentials(),
          api.projects()
        ]);

        const supportedTargets = (connectorResult.targets ?? []).filter(isSupportedTaxonomyTarget);

        setTargets(supportedTargets);
        setCredentials(credentialResult ?? []);
        setProjects(projectResult ?? []);

        if (!targetType && supportedTargets.length > 0) {
          const firstTargetType = optionKey(supportedTargets[0]);
          setTargetType(firstTargetType);
          setFileName(buildDefaultFileName(firstTargetType));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        setLoading(false);
      }
    }

    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    setCredentialSetId("");
    setResult(null);
    setNotice(null);

    if (targetType) {
      setFileName(buildDefaultFileName(targetType));
    }
  }, [targetType]);

  useEffect(() => {
    if (!credentialSetId && matchingCredentials.length === 1) {
      setCredentialSetId(matchingCredentials[0].credentialSetId);
    }
  }, [credentialSetId, matchingCredentials]);

  async function buildArtifact() {
    setError(null);
    setNotice(null);
    setResult(null);

    if (!selectedTarget) {
      setNotice({ kind: "error", message: "Choose a target connector first." });
      return;
    }

    if (!credentialSetId) {
      setNotice({ kind: "error", message: "Choose target credentials before building the taxonomy workbook." });
      return;
    }

    setBuilding(true);

    try {
      const response = await buildTaxonomy({
        targetType,
        credentialSetId,
        includeOptions,
        includeRaw,
        projectId: projectId || null,
        fileName: fileName.trim() || null,
        description: `Generated by Taxonomy Builder from ${displayConnectorName(selectedTarget)}`
      });

      setResult(response);
      setNotice({
        kind: "success",
        message: `Taxonomy artifact created: ${response.fileName}.`
      });
    } catch (err) {
      setNotice({
        kind: "error",
        message: `Failed to build taxonomy artifact: ${err instanceof Error ? err.message : String(err)}`
      });
    } finally {
      setBuilding(false);
    }
  }

  const canBuild = Boolean(selectedTarget && credentialSetId && !building);
  const usingFallbackCredentials =
    Boolean(selectedTargetKind) &&
    targetCredentials.length > 0 &&
    targetCredentials.every(credential => targetKindForCredential(credential) !== selectedTargetKind);

  return (
    <div className="pageStack taxonomyBuilderPage">
      <div className="pageHeader">
        <div>
          <h1>Taxonomy Builder</h1>
          <p>
            Pull target metadata schema from Bynder, Cloudinary, or Aprimo and save it as an Excel taxonomy artifact.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      {notice && (
        <div className={noticeClassName(notice.kind)}>
          {notice.message}
        </div>
      )}

      {loading ? (
        <Card>
          <p>Loading taxonomy builder inputs…</p>
        </Card>
      ) : targets.length === 0 ? (
        <Card title="No supported target connectors">
          <p>
            Taxonomy Builder currently supports Bynder, Cloudinary, and Aprimo target connectors.
            None are registered in the connector catalog.
          </p>
        </Card>
      ) : (
        <Card
          title="Build Taxonomy Workbook"
          subtitle="Choose a target system and a target credential set. The backend will call the target API and save the result under Artifacts."
        >
          <div className="formGrid">
            <label>
              Target connector
              <select
                value={targetType}
                onChange={event => setTargetType(event.target.value)}
                disabled={building}
              >
                {targets.map(target => {
                  const value = optionKey(target);
                  return (
                    <option key={value} value={value}>
                      {displayConnectorName(target)}
                    </option>
                  );
                })}
              </select>
            </label>

            <label>
              Target credentials
              <select
                value={credentialSetId}
                onChange={event => setCredentialSetId(event.target.value)}
                disabled={building || matchingCredentials.length === 0}
              >
                <option value="">Choose target credentials</option>
                {matchingCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName} ({credential.connectorType})
                  </option>
                ))}
              </select>
            </label>

            <label>
              Project binding (optional)
              <select
                value={projectId}
                onChange={event => setProjectId(event.target.value)}
                disabled={building}
              >
                <option value="">Do not bind to a project</option>
                {projects.map(project => (
                  <option key={project.projectId} value={project.projectId}>
                    {project.displayName} ({project.sourceType} → {project.targetType})
                  </option>
                ))}
              </select>
            </label>

            <label>
              File name
              <input
                value={fileName}
                onChange={event => setFileName(event.target.value)}
                disabled={building}
                placeholder="bynder-taxonomy.xls"
              />
            </label>
          </div>

          {matchingCredentials.length === 0 && (
            <p className="helpText">
              No target credential sets are saved yet. Create target credentials first, then return here.
            </p>
          )}

          {usingFallbackCredentials && (
            <p className="helpText">
              No credentials exactly match this target connector type, so all target credential sets are shown.
              Choose the set that belongs to this target.
            </p>
          )}

          <div className="buttonRow">
            <label className="check">
              <input
                type="checkbox"
                checked={includeOptions}
                onChange={event => setIncludeOptions(event.target.checked)}
                disabled={building}
              />
              Include controlled values / options
            </label>

            <label className="check">
              <input
                type="checkbox"
                checked={includeRaw}
                onChange={event => setIncludeRaw(event.target.checked)}
                disabled={building}
              />
              Include raw response sheet
            </label>
          </div>

          <div className="buttonRow">
            <button
              type="button"
              className="primaryButton"
              onClick={() => void buildArtifact()}
              disabled={!canBuild}
            >
              {building ? "Building…" : "Build Excel Taxonomy Artifact"}
            </button>
          </div>
        </Card>
      )}

      {result && (
        <Card title="Created Taxonomy Artifact">
          <div className="detailGrid">
            <span>Artifact ID</span>
            <strong>{result.artifact.artifactId}</strong>

            <span>Target</span>
            <strong>{result.targetType}</strong>

            <span>File</span>
            <strong>{result.fileName}</strong>

            <span>Fields</span>
            <strong>{result.fieldCount}</strong>

            <span>Options</span>
            <strong>{result.optionCount}</strong>
          </div>

          <div className="buttonRow">
            <a className="secondaryButton" href={api.artifactDownloadUrl(result.artifact.artifactId)}>
              Download Taxonomy
            </a>
            <a className="secondaryButton" href="/artifacts">
              View in Artifacts
            </a>
          </div>
        </Card>
      )}
    </div>
  );
}
