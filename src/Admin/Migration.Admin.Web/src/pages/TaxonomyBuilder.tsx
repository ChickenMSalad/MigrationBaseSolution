import { useEffect, useMemo, useState } from "react";

import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord, ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type NoticeKind = "success" | "error" | "info";

type PageNotice = {
  kind: NoticeKind;
  message: string;
};

type TaxonomyBuildResponse = {
  artifact: ArtifactRecord;
  targetType: string;
  fieldCount: number;
  optionCount: number;
  fileName: string;
};

function noticeClassName(kind: NoticeKind) {
  return `taxonomyNotice taxonomyNotice--${kind}`;
}

function sameConnectorType(a: string | undefined | null, b: string | undefined | null) {
  return String(a ?? "").trim().toLowerCase() === String(b ?? "").trim().toLowerCase();
}

function targetSupportsTaxonomy(target: ConnectorDescriptor) {
  const value = connectorValue(target).toLowerCase();
  return value.includes("bynder") || value.includes("cloudinary") || value.includes("aprimo");
}

async function readError(response: Response) {
  try {
    const body = await response.json();
    return body?.error ?? body?.message ?? JSON.stringify(body);
  } catch {
    try {
      return await response.text();
    } catch {
      return `${response.status} ${response.statusText}`;
    }
  }
}

export function TaxonomyBuilder() {
  const [targets, setTargets] = useState<ConnectorDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [targetType, setTargetType] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [includeOptions, setIncludeOptions] = useState(true);
  const [includeRaw, setIncludeRaw] = useState(true);
  const [loading, setLoading] = useState(true);
  const [building, setBuilding] = useState(false);
  const [notice, setNotice] = useState<PageNotice | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<TaxonomyBuildResponse | null>(null);

  const taxonomyTargets = useMemo(
    () => targets.filter(targetSupportsTaxonomy),
    [targets]
  );

  const selectedTarget = useMemo(
    () => taxonomyTargets.find(target => sameConnectorType(connectorValue(target), targetType)) ?? null,
    [taxonomyTargets, targetType]
  );

  const matchingCredentials = useMemo(
    () => credentials.filter(credential =>
      credential.connectorRole?.toLowerCase() === "target" &&
      sameConnectorType(credential.connectorType, targetType)),
    [credentials, targetType]
  );

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [connectorResult, credentialResult] = await Promise.all([
          api.connectors(),
          api.credentials()
        ]);

        const availableTargets = connectorResult.targets ?? [];
        const taxonomyCapableTargets = availableTargets.filter(targetSupportsTaxonomy);

        setTargets(availableTargets);
        setCredentials(credentialResult);

        if (taxonomyCapableTargets.length > 0) {
          setTargetType(connectorValue(taxonomyCapableTargets[0]));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        setLoading(false);
      }
    }

    void load();
  }, []);

  useEffect(() => {
    setCredentialSetId(matchingCredentials[0]?.credentialSetId ?? "");
    setResult(null);
    setNotice(null);
  }, [targetType, matchingCredentials]);

  async function buildTaxonomyArtifact() {
    setError(null);
    setNotice(null);
    setResult(null);

    if (!selectedTarget) {
      setNotice({ kind: "error", message: "Choose Bynder, Cloudinary, or Aprimo first." });
      return;
    }

    if (!credentialSetId) {
      setNotice({ kind: "error", message: "Choose a target credential set first." });
      return;
    }

    setBuilding(true);
    try {
      const response = await fetch("/api/taxonomy-builder/build", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          targetType,
          credentialSetId,
          includeOptions,
          includeRaw
        })
      });

      if (!response.ok) {
        throw new Error(await readError(response));
      }

      const buildResult = await response.json() as TaxonomyBuildResponse;
      setResult(buildResult);
      setNotice({
        kind: "success",
        message: `Created ${buildResult.fileName} with ${buildResult.fieldCount} fields and ${buildResult.optionCount} options.`
      });
    } catch (err) {
      setNotice({
        kind: "error",
        message: `Failed to build taxonomy workbook: ${err instanceof Error ? err.message : String(err)}`
      });
    } finally {
      setBuilding(false);
    }
  }

  return (
    <div className="page taxonomyBuilderPage">
      <div className="pageTitle">
        <div>
          <h1>Taxonomy Builder</h1>
          <p>
            Pull target metadata schema from Bynder, Cloudinary, or Aprimo and save it as an Excel taxonomy artifact.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}
      {notice && <div className={noticeClassName(notice.kind)}>{notice.message}</div>}

      <div className="formGrid">
        <Card title="Build Taxonomy Workbook">
          {loading ? (
            <p className="muted">Loading target connectors…</p>
          ) : taxonomyTargets.length === 0 ? (
            <p className="muted">No Bynder, Cloudinary, or Aprimo target connectors are registered.</p>
          ) : (
            <div className="formGrid">
              <label>
                Target connector
                <select
                  value={targetType}
                  onChange={event => setTargetType(event.target.value)}
                >
                  {taxonomyTargets.map(target => (
                    <option key={connectorValue(target)} value={connectorValue(target)}>
                      {displayConnectorName(target)}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                Target credentials
                <select
                  value={credentialSetId}
                  onChange={event => setCredentialSetId(event.target.value)}
                >
                  <option value="">Choose credentials</option>
                  {matchingCredentials.map(credential => (
                    <option key={credential.credentialSetId} value={credential.credentialSetId}>
                      {credential.displayName}
                    </option>
                  ))}
                </select>
              </label>

              <label className="checkboxRow">
                <input
                  type="checkbox"
                  checked={includeOptions}
                  onChange={event => setIncludeOptions(event.target.checked)}
                />
                Include controlled values / options
              </label>

              <label className="checkboxRow">
                <input
                  type="checkbox"
                  checked={includeRaw}
                  onChange={event => setIncludeRaw(event.target.checked)}
                />
                Include raw response sheet
              </label>

              <button
                className="primaryButton"
                type="button"
                onClick={() => void buildTaxonomyArtifact()}
                disabled={building || !selectedTarget || !credentialSetId}
              >
                {building ? "Building…" : "Build Excel Taxonomy Artifact"}
              </button>

              {matchingCredentials.length === 0 && targetType && (
                <p className="muted">
                  No target credential sets match this connector type. Create target credentials first, then return here.
                </p>
              )}
            </div>
          )}
        </Card>

        <Card title="Output">
          {result ? (
            <div className="stack">
              <dl className="detailsList">
                <div>
                  <dt>File</dt>
                  <dd>{result.fileName}</dd>
                </div>
                <div>
                  <dt>Target</dt>
                  <dd>{result.targetType}</dd>
                </div>
                <div>
                  <dt>Fields</dt>
                  <dd>{result.fieldCount}</dd>
                </div>
                <div>
                  <dt>Options</dt>
                  <dd>{result.optionCount}</dd>
                </div>
                <div>
                  <dt>Artifact ID</dt>
                  <dd><code>{result.artifact.artifactId}</code></dd>
                </div>
              </dl>

              <div className="buttonRow">
                <a
                  className="primaryButton"
                  href={`/api/artifacts/${encodeURIComponent(result.artifact.artifactId)}`}
                >
                  Download Excel
                </a>
                <a className="secondaryButton" href="/artifacts">
                  View in Artifacts
                </a>
              </div>
            </div>
          ) : (
            <p className="muted">
              The workbook will be saved as a Taxonomy artifact and will include Fields, Options, and optional Raw sheets.
            </p>
          )}
        </Card>
      </div>
    </div>
  );
}
