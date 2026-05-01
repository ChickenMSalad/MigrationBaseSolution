import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type NoticeKind = "success" | "error" | "info";

type PageNotice = {
  kind: NoticeKind;
  message: string;
};

function noticeClassName(kind: NoticeKind) {
  return `taxonomyNotice taxonomyNotice--${kind}`;
}

function safeFilePart(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, "-")
    .replace(/^-+|-+$/g, "") || "connector";
}

function descriptorArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

function buildTaxonomyPayload(connector: ConnectorDescriptor, credentialSetId: string | null) {
  const now = new Date().toISOString();
  const connectorType = connectorValue(connector);

  return {
    artifactType: "Taxonomy",
    generatedBy: "TaxonomyBuilder",
    generatedUtc: now,
    source: {
      connectorType,
      displayName: displayConnectorName(connector),
      credentialSetId
    },
    taxonomy: {
      // Phase 1: catalog-based taxonomy/field contract.
      // Later connector-specific services can replace this with live target DAM metaproperties.
      fields: descriptorArray(connector.options),
      mappingFields: descriptorArray(connector.mappingFields),
      manifestColumns: descriptorArray(connector.manifestColumns),
      metadata: connector.metadata ?? {}
    },
    connectorSchema: connector
  };
}

async function uploadTaxonomyFile(file: File, description: string) {
  const form = new FormData();
  form.append("file", file);
  form.append("description", description);

  // Preferred endpoint from the backend patch.
  let response = await fetch("/api/artifacts/taxonomies", {
    method: "POST",
    body: form
  });

  if (response.status === 404) {
    // Fallback while backend patch is being applied.
    const fallback = new FormData();
    fallback.append("file", file);
    fallback.append("kind", "Taxonomy");
    fallback.append("description", description);

    response = await fetch("/api/artifacts", {
      method: "POST",
      body: fallback
    });
  }

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;

    try {
      const body = await response.json();
      message = body?.error ?? JSON.stringify(body);
    } catch {
      try {
        message = await response.text();
      } catch {
        // keep default
      }
    }

    throw new Error(message);
  }

  return await response.json();
}

export function TaxonomyBuilder() {
  const [targets, setTargets] = useState<ConnectorDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [targetType, setTargetType] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [loading, setLoading] = useState(true);
  const [building, setBuilding] = useState(false);
  const [notice, setNotice] = useState<PageNotice | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [createdArtifact, setCreatedArtifact] = useState<{ artifactId: string; fileName: string } | null>(null);

  const selectedTarget = useMemo(
    () => targets.find(target => connectorValue(target).toLowerCase() === targetType.toLowerCase()) ?? null,
    [targets, targetType]
  );

  const matchingCredentials = useMemo(
    () => credentials.filter(credential =>
      credential.connectorRole?.toLowerCase() === "target" &&
      credential.connectorType?.toLowerCase() === targetType.toLowerCase()),
    [credentials, targetType]
  );

  const previewPayload = useMemo(
    () => selectedTarget ? buildTaxonomyPayload(selectedTarget, credentialSetId || null) : null,
    [selectedTarget, credentialSetId]
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

        setTargets(connectorResult.targets ?? []);
        setCredentials(credentialResult);

        if ((connectorResult.targets ?? []).length > 0) {
          setTargetType(connectorValue(connectorResult.targets[0]));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        setLoading(false);
      }
    }

    void load();
  }, []);

  async function createTaxonomyArtifact() {
    setError(null);
    setNotice(null);
    setCreatedArtifact(null);

    if (!selectedTarget) {
      setNotice({ kind: "error", message: "Choose a target connector first." });
      return;
    }

    setBuilding(true);

    try {
      const payload = buildTaxonomyPayload(selectedTarget, credentialSetId || null);
      const connectorType = safeFilePart(connectorValue(selectedTarget));
      const timestamp = new Date().toISOString().replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");
      const fileName = `${connectorType}-taxonomy-${timestamp}.json`;
      const file = new File(
        [JSON.stringify(payload, null, 2)],
        fileName,
        { type: "application/json" }
      );

      const artifact = await uploadTaxonomyFile(
        file,
        `Generated by Taxonomy Builder from ${displayConnectorName(selectedTarget)}`
      );

      setCreatedArtifact({
        artifactId: artifact.artifactId,
        fileName: artifact.fileName
      });

      setNotice({
        kind: "success",
        message: `Taxonomy artifact created: ${artifact.fileName}. It is available under Artifacts.`
      });
    } catch (err) {
      setNotice({
        kind: "error",
        message: `Failed to create taxonomy artifact: ${err instanceof Error ? err.message : String(err)}`
      });
    } finally {
      setBuilding(false);
    }
  }

  return (
    <div className="pageStack taxonomyBuilderPage">
      <div className="pageHeader">
        <div>
          <h1>Taxonomy Builder</h1>
          <p className="muted">
            Create a taxonomy/metaproperty artifact for a target DAM. Phase 1 uses the connector catalog schema;
            connector-specific live taxonomy pulls can be added behind this same page later.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      {notice && (
        <div className={noticeClassName(notice.kind)}>
          {notice.message}
        </div>
      )}

      <Card title="Build Taxonomy Artifact">
        {loading ? (
          <p className="muted">Loading target connectors…</p>
        ) : targets.length === 0 ? (
          <p className="muted">No target connectors are registered.</p>
        ) : (
          <div className="formGrid">
            <label>
              Target connector
              <select value={targetType} onChange={event => {
                setTargetType(event.target.value);
                setCredentialSetId("");
                setCreatedArtifact(null);
                setNotice(null);
              }}>
                {targets.map(target => (
                  <option key={connectorValue(target)} value={connectorValue(target)}>
                    {displayConnectorName(target)}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Credentials
              <select value={credentialSetId} onChange={event => setCredentialSetId(event.target.value)}>
                <option value="">No credential set selected</option>
                {matchingCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName}
                  </option>
                ))}
              </select>
              <span className="helpText">
                This catalog-based export does not call the target API yet, but the selected credential is recorded in the artifact.
              </span>
            </label>

            <div className="buttonRow">
              <button
                type="button"
                className="primaryButton"
                onClick={() => void createTaxonomyArtifact()}
                disabled={building || !selectedTarget}
              >
                {building ? "Creating…" : "Create Taxonomy Artifact"}
              </button>

              {createdArtifact && (
                <>
                  <a className="secondaryButton" href={`/api/artifacts/${encodeURIComponent(createdArtifact.artifactId)}/download`}>
                    Download Taxonomy
                  </a>
                  <a className="secondaryButton" href="/artifacts">
                    View in Artifacts
                  </a>
                </>
              )}
            </div>
          </div>
        )}
      </Card>

      {previewPayload && (
        <Card title="Taxonomy Preview">
          <p className="muted">
            This is the JSON that will be saved as the taxonomy artifact.
          </p>
          <JsonBlock value={previewPayload} />
        </Card>
      )}
    </div>
  );
}