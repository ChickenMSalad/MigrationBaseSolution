import { useEffect, useMemo, useState } from "react";
import { api } from "../../../../../api/client";
import { Card } from "../../../../../components/Card";
import { LoadingError } from "../../../../../components/LoadingError";
import type {
  BuildSourceManifestResponse,
  CredentialSetSummary,
  ManifestBuilderSourceDescriptor
} from "../../../../../types/api";

type ManifestOptions = Record<string, string>;

export function ManifestBuilder() {
  const [sources, setSources] = useState<ManifestBuilderSourceDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [sourceType, setSourceType] = useState("");
  const [serviceName, setServiceName] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [options, setOptions] = useState<ManifestOptions>({});
  const [loading, setLoading] = useState(true);
  const [building, setBuilding] = useState(false);
  const [result, setResult] = useState<BuildSourceManifestResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedSource = useMemo(
    () => sources.find(source => source.sourceType === sourceType),
    [sources, sourceType]
  );

  const selectedService = useMemo(
    () => selectedSource?.services.find(service => service.serviceName === serviceName) ?? null,
    [selectedSource, serviceName]
  );

  const matchingCredentials = useMemo(
    () => credentials.filter(credential => credentialMatchesSource(credential, sourceType)),
    [credentials, sourceType]
  );

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);

      try {
        const [loadedSources, loadedCredentials] = await Promise.all([
          api.manifestBuilderSources(),
          api.credentials()
        ]);

        setSources(loadedSources);
        setCredentials(loadedCredentials);

        if (loadedSources.length > 0) {
          const firstSource = loadedSources[0];
          setSourceType(firstSource.sourceType);
          setServiceName(firstSource.services[0]?.serviceName ?? "");
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
    if (!selectedSource) {
      setServiceName("");
      return;
    }

    if (!selectedSource.services.some(service => service.serviceName === serviceName)) {
      setServiceName(selectedSource.services[0]?.serviceName ?? "");
    }

    setCredentialSetId("");
    setOptions({});
    setResult(null);
  }, [selectedSource, serviceName]);

  function setOption(name: string, value: string) {
    setOptions(current => ({ ...current, [name]: value }));
  }

  function isFolderListOption(name: string) {
    const normalizedName = name.toLowerCase();
    return sourceType.toLowerCase() === "aem" &&
      (normalizedName === "folders" ||
        normalizedName === "folderpaths" ||
        normalizedName === "exportfolders" ||
        normalizedName === "export.folders");
  }

  async function buildManifest() {
    setBuilding(true);
    setError(null);
    setResult(null);

    try {
      const response = await api.buildManifest({
        sourceType,
        serviceName,
        credentialSetId: credentialSetId.trim() || null,
        options
      });

      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBuilding(false);
    }
  }

  const artifactId = result?.artifactId ?? result?.manifestId ?? result?.artifact?.artifactId ?? "";
  const downloadUrl = artifactId ? api.artifactDownloadUrl(artifactId) : "";
  const fileName = result?.fileName ?? result?.artifact?.fileName ?? "manifest";

  return (
    <div className="pageStack  manifestBuilder">
      <div className="pageHeader">
        <h1>Manifest Builder</h1>
        <p>Choose a source, choose credentials, run a manifest service, and create a Manifest artifact.</p>
      </div>

      {error && <LoadingError message={error} />}

      <Card title="Build Manifest">
        <p className="mutedText">
          Generated manifests are persisted under <strong>Artifacts</strong> as kind <strong>Manifest</strong>. The response only returns artifact metadata so large manifests are not pushed back through the browser payload.
        </p>

        {loading ? (
          <p>Loading manifest sources...</p>
        ) : sources.length === 0 ? (
          <p>No manifest builder services are registered.</p>
        ) : (
                <>
          <div className="formGrid">
            <label>
              Source
              <select value={sourceType} onChange={event => setSourceType(event.target.value)}>
                {sources.map(source => (
                  <option key={source.sourceType} value={source.sourceType}>{source.displayName}</option>
                ))}
              </select>
            </label>

            <label>
              Service
              <select value={serviceName} onChange={event => setServiceName(event.target.value)}>
                {selectedSource?.services.map(service => (
                  <option key={service.serviceName} value={service.serviceName}>{service.displayName}</option>
                ))}
              </select>
              {selectedService?.description && <span className="helpText">{selectedService.description}</span>}
            </label>

            <label>
              Credentials
              <select value={credentialSetId} onChange={event => setCredentialSetId(event.target.value)}>
                <option value="">Use configured/default credentials</option>
                {matchingCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName}
                  </option>
                ))}
              </select>
              <span className="helpText">Choose a saved credential set for this source, or use configured/default credentials.</span>
            </label>

            {selectedService?.options.map(option => (
              <label key={option.name}>
                {option.label ?? option.name}
                {isFolderListOption(option.name) ? (
                  <textarea
                    value={options[option.name] ?? ""}
                    onChange={event => setOption(option.name, event.target.value)}
                    placeholder={option.placeholder ?? "/content/dam/example-folder"}
                    required={option.required}
                    rows={6}
                  />
                ) : (
                  <input
                    value={options[option.name] ?? ""}
                    onChange={event => setOption(option.name, event.target.value)}
                    placeholder={option.placeholder ?? ""}
                    required={option.required}
                  />
                )}
                {option.description && <span className="helpText">{option.description}</span>}
              </label>
            ))}
          </div>
          <div className="buttonRow">
                <button
                    type="button"
                    className="primaryButton"
                    onClick={() => void buildManifest()}
                    disabled={building || !sourceType || !serviceName}
                >
                    {building ? "Building..." : "Build Manifest"}
                </button>
          </div>
          </>
        )}
      </Card>

      {result && (
        <Card title="Manifest Artifact Created">
          <p className="successText">
            Manifest saved to Artifacts as <strong>{fileName}</strong>.
          </p>
          <div className="detailGrid">
            <span>Artifact ID</span>
            <strong>{artifactId || "Not returned"}</strong>
            <span>Source</span>
            <strong>{result.sourceType}</strong>
            <span>Service</span>
            <strong>{result.serviceName}</strong>
            <span>Rows</span>
            <strong>{result.rowCount}</strong>
          </div>
          <div className="buttonRow">
            {downloadUrl ? (
              <a className="primaryButton" href={downloadUrl} download={fileName}>
                Download Manifest
              </a>
            ) : (
              <button type="button" className="primaryButton" disabled>
                Download unavailable
              </button>
            )}
            <a className="secondaryButton" href="/artifacts">View in Artifacts</a>
          </div>
        </Card>
      )}
    </div>
  );
}

function credentialMatchesSource(credential: CredentialSetSummary, selectedSourceType: string) {
  const credentialType = credential.connectorType?.toLowerCase();
  const credentialRole = credential.connectorRole?.toLowerCase();
  const normalizedSourceType = selectedSourceType.toLowerCase();

  if (credentialType !== normalizedSourceType) {
    return false;
  }

  if (normalizedSourceType === "bynder" || normalizedSourceType === "webdam") {
    return credentialRole === "source" || credentialRole === "target";
  }

  return credentialRole === "source";
}

