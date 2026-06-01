import { useEffect, useMemo, useState } from "react";
import { credentialVaultApi } from "../api/credentialVaultApi";
import { Card, EmptyState, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type {
  ConnectorCredentialCatalogItem,
  ConnectorCredentialValidationResponse,
  ConnectorCredentialVaultSummary
} from "../types/credentialVault";

const emptySummary: ConnectorCredentialVaultSummary = {
  registeredCredentialReferences: 0,
  missingSecretReferences: 0,
  connectorsRequiringCredentials: 0,
  supportedSecretProviders: []
};

function formatNumber(value: number | undefined | null) {
  return value === undefined || value === null ? "—" : value.toLocaleString();
}

export function CredentialVault() {
  const [summary, setSummary] = useState<ConnectorCredentialVaultSummary>(emptySummary);
  const [catalog, setCatalog] = useState<ConnectorCredentialCatalogItem[]>([]);
  const [validation, setValidation] = useState<ConnectorCredentialValidationResponse | null>(null);
  const [connectorKey, setConnectorKey] = useState("target.bynder");
  const [secretProvider, setSecretProvider] = useState("AzureKeyVault");
  const [secretReferenceName, setSecretReferenceName] = useState("kv://migration-platform/bynder-client-secret");
  const [loading, setLoading] = useState(true);
  const [validating, setValidating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const [summaryResult, catalogResult] = await Promise.all([
        credentialVaultApi.summary(),
        credentialVaultApi.catalog()
      ]);
      setSummary(summaryResult ?? emptySummary);
      setCatalog(catalogResult ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const supportedProvidersText = useMemo(() => {
    if (!summary.supportedSecretProviders || summary.supportedSecretProviders.length === 0) {
      return "No providers reported";
    }

    return summary.supportedSecretProviders.join(", ");
  }, [summary.supportedSecretProviders]);

  async function validateReference() {
    setError(null);
    setValidation(null);
    setValidating(true);
    try {
      const result = await credentialVaultApi.validateReference({
        connectorKey,
        secretProvider,
        secretReferenceName
      });
      setValidation(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setValidating(false);
    }
  }

  if (loading) {
    return (
      <>
        <h1>Credential Vault</h1>
        <p>Loading connector credential vault state...</p>
      </>
    );
  }

  if (error) {
    return (
      <>
        <h1>Credential Vault</h1>
        <LoadingError message={error} onRetry={() => void load()} />
      </>
    );
  }

  return (
    <>
      <div className="pageHeader">
        <div>
          <h1>Credential Vault</h1>
          <p>Validate connector credential references without storing secret material in runtime configuration.</p>
        </div>
        <button type="button" onClick={() => void load()}>Refresh</button>
      </div>

      <div className="dashboardGrid">
        <Card title="Registered references">
          <strong>{formatNumber(summary.registeredCredentialReferences)}</strong>
        </Card>
        <Card title="Missing references">
          <strong>{formatNumber(summary.missingSecretReferences)}</strong>
        </Card>
        <Card title="Credentialed connectors">
          <strong>{formatNumber(summary.connectorsRequiringCredentials)}</strong>
        </Card>
        <Card title="Secret providers">
          <span>{supportedProvidersText}</span>
        </Card>
      </div>

      <Card title="Validate credential reference">
        <div className="formGrid">
          <label>
            Connector key
            <input value={connectorKey} onChange={event => setConnectorKey(event.target.value)} />
          </label>
          <label>
            Secret provider
            <input value={secretProvider} onChange={event => setSecretProvider(event.target.value)} />
          </label>
          <label>
            Secret reference
            <input value={secretReferenceName} onChange={event => setSecretReferenceName(event.target.value)} />
          </label>
        </div>
        <div className="buttonRow">
          <button type="button" onClick={() => void validateReference()} disabled={validating}>
            {validating ? "Validating..." : "Validate reference"}
          </button>
        </div>
        {validation && (
          <div className="detailPanel">
            <StatusPill value={validation.isValid ? "Valid" : "Needs attention"} />
            <p>{validation.message}</p>
            {validation.findings.length > 0 && (
              <ul>
                {validation.findings.map(finding => (
                  <li key={finding}>{finding}</li>
                ))}
              </ul>
            )}
          </div>
        )}
      </Card>

      <Card title="Credential catalog">
        {catalog.length === 0 ? (
          <EmptyState title="No credential catalog entries returned" />
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Connector</th>
                  <th>Direction</th>
                  <th>Required secrets</th>
                </tr>
              </thead>
              <tbody>
                {catalog.map(item => (
                  <tr key={item.connectorKey}>
                    <td>
                      <strong>{item.displayName}</strong>
                      <br />
                      <small>{item.connectorKey}</small>
                    </td>
                    <td>{item.direction}</td>
                    <td>
                      {item.requiredSecretNames.length === 0
                        ? <span className="muted">None</span>
                        : item.requiredSecretNames.map(name => <code key={name}>{name} </code>)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  );
}
