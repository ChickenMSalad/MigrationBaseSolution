import { useEffect, useMemo, useState } from "react";
import { api } from "../api/client";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { BuildSourceManifestResponse, CredentialSetSummary, ManifestBuilderSourceDescriptor } from "../types/api";

export function ManifestBuilder() {
  const [sources, setSources] = useState<ManifestBuilderSourceDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [sourceType, setSourceType] = useState("");
  const [serviceName, setServiceName] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [options, setOptions] = useState<Record<string, string>>({});
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
    () => credentials.filter(credential =>
      credential.connectorRole?.toLowerCase() === "source" &&
      credential.connectorType?.toLowerCase() === sourceType.toLowerCase()),
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

          if (firstSource.services.length > 0) {
            setServiceName(firstSource.services[0].serviceName);
          }
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

  return (
    <div className="pageStack manifestBuilder">
      <div className="pageHeader">
        <div>
          <h1>Manifest Builder</h1>
          <p className="muted">
            Choose a source, choose credentials, run a manifest service, and create a Manifest artifact.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      <Card title="Build Manifest">
        <p className="muted">
          Generated manifests are saved under <strong>Artifacts</strong> as kind <strong>Manifest</strong>.
          You can download the new manifest here immediately or manage it later from the Artifacts page.
        </p>

        {loading ? (
          <p className="muted">Loading manifest sources…</p>
        ) : sources.length === 0 ? (
          <p className="muted">No manifest builder services are registered.</p>
        ) : (
          <div className="formGrid">
            <label>
              Source
              <select value={sourceType} onChange={event => setSourceType(event.target.value)}>
                {sources.map(source => (
                  <option key={source.sourceType} value={source.sourceType}>
                    {source.displayName}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Service
              <select value={serviceName} onChange={event => setServiceName(event.target.value)}>
                {selectedSource?.services.map(service => (
                  <option key={service.serviceName} value={service.serviceName}>
                    {service.displayName}
                  </option>
                ))}
              </select>
              {selectedService?.description && (
                <span className="helpText">{selectedService.description}</span>
              )}
            </label>

            <label>
              Credentials
              <select
                value={credentialSetId}
                onChange={event => setCredentialSetId(event.target.value)}
              >
                <option value="">Use configured/default credentials</option>
                {matchingCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName}
                  </option>
                ))}
              </select>
              <span className="helpText">
                Choose a saved credential set for this source, or use configured/default credentials.
              </span>
            </label>

            {selectedService?.options.map(option => (
              <label key={option.name}>
                {option.label}
                <input
                  value={options[option.name] ?? ""}
                  onChange={event => setOption(option.name, event.target.value)}
                  placeholder={option.placeholder ?? ""}
                  required={option.required}
                />
                {option.description && <span className="helpText">{option.description}</span>}
              </label>
            ))}

            <div className="buttonRow">
              <button
                type="button"
                className="primaryButton"
                onClick={() => void buildManifest()}
                disabled={building || !sourceType || !serviceName}
              >
                {building ? "Building…" : "Build Manifest"}
              </button>
            </div>
          </div>
        )}
      </Card>

      {result && (
        <Card title="Manifest Artifact Created">
          <p className="successText">
            Manifest saved to Artifacts as <strong>{result.fileName}</strong>.
          </p>

          <div className="detailGrid">
            <span>Artifact ID</span>
            <strong>{result.artifactId || result.manifestId}</strong>

            <span>Source</span>
            <strong>{result.sourceType}</strong>

            <span>Service</span>
            <strong>{result.serviceName}</strong>

            <span>Rows</span>
            <strong>{result.rowCount}</strong>
          </div>

          <div className="buttonRow">
            <a className="primaryButton" href={result.downloadUrl}>
              Download Manifest
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