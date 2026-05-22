import { useEffect, useState } from 'react';
import {
  fetchConnectorCredentialCatalog,
  fetchConnectorCredentialVaultSummary,
  validateConnectorCredentialReference,
} from './credentialVaultApi';
import type {
  ConnectorCredentialCatalogItem,
  ConnectorCredentialValidationResponse,
  ConnectorCredentialVaultSummary,
} from './credentialVaultTypes';

const emptySummary: ConnectorCredentialVaultSummary = {
  registeredCredentialReferences: 0,
  missingSecretReferences: 0,
  connectorsRequiringCredentials: 0,
  supportedSecretProviders: [],
};

export function CredentialVaultWorkspace() {
  const [summary, setSummary] = useState<ConnectorCredentialVaultSummary>(emptySummary);
  const [catalog, setCatalog] = useState<ConnectorCredentialCatalogItem[]>([]);
  const [validation, setValidation] = useState<ConnectorCredentialValidationResponse | null>(null);
  const [connectorKey, setConnectorKey] = useState('target.bynder');
  const [secretProvider, setSecretProvider] = useState('AzureKeyVault');
  const [secretReferenceName, setSecretReferenceName] = useState('kv://migration-platform/bynder-client-secret');

  useEffect(() => {
    void fetchConnectorCredentialVaultSummary().then(setSummary);
    void fetchConnectorCredentialCatalog().then(setCatalog);
  }, []);

  async function validateReference() {
    const result = await validateConnectorCredentialReference({
      connectorKey,
      secretProvider,
      secretReferenceName,
    });

    setValidation(result);
  }

  return (
    <section className="workspace-card">
      <header>
        <p className="eyebrow">Connector security</p>
        <h2>Credential vault references</h2>
        <p>Register connector credential references without storing secret material in runtime configuration.</p>
      </header>

      <div className="metric-grid">
        <div>
          <strong>{summary.registeredCredentialReferences}</strong>
          <span>Registered references</span>
        </div>
        <div>
          <strong>{summary.missingSecretReferences}</strong>
          <span>Missing references</span>
        </div>
        <div>
          <strong>{summary.connectorsRequiringCredentials}</strong>
          <span>Credentialed connectors</span>
        </div>
      </div>

      <div className="form-grid">
        <label>
          Connector key
          <input value={connectorKey} onChange={(event) => setConnectorKey(event.target.value)} />
        </label>
        <label>
          Secret provider
          <input value={secretProvider} onChange={(event) => setSecretProvider(event.target.value)} />
        </label>
        <label>
          Secret reference
          <input value={secretReferenceName} onChange={(event) => setSecretReferenceName(event.target.value)} />
        </label>
        <button type="button" onClick={() => void validateReference()}>
          Validate reference
        </button>
      </div>

      {validation && (
        <div className="status-panel">
          <strong>{validation.isValid ? 'Valid' : 'Needs attention'}</strong>
          <p>{validation.message}</p>
          {validation.findings.length > 0 && (
            <ul>
              {validation.findings.map((finding) => (
                <li key={finding}>{finding}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      <div className="list-panel">
        <h3>Credential catalog</h3>
        {catalog.map((item) => (
          <article key={item.connectorKey}>
            <strong>{item.displayName}</strong>
            <p>{item.direction} · {item.connectorKey}</p>
            <small>{item.requiredSecretNames.join(', ')}</small>
          </article>
        ))}
      </div>
    </section>
  );
}
