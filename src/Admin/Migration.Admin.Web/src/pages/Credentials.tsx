import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, EmptyState, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type Role = "Source" | "Target" | "Manifest";

const starterValues = JSON.stringify({
  BaseUrl: "",
  ClientId: "",
  ClientSecret: ""
}, null, 2);

function flattenConnectors(data: { sources?: ConnectorDescriptor[]; targets?: ConnectorDescriptor[]; manifestProviders?: ConnectorDescriptor[] } | null) {
  const result: Array<{ role: Role; connector: ConnectorDescriptor }> = [];
  for (const connector of data?.sources ?? []) result.push({ role: "Source", connector });
  for (const connector of data?.targets ?? []) result.push({ role: "Target", connector });
  for (const connector of data?.manifestProviders ?? []) result.push({ role: "Manifest", connector });
  return result;
}

export function Credentials() {
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [connectors, setConnectors] = useState<Array<{ role: Role; connector: ConnectorDescriptor }>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState("Local test credentials");
  const [selectedConnector, setSelectedConnector] = useState("");
  const [valuesJson, setValuesJson] = useState(starterValues);
  const [secretKeys, setSecretKeys] = useState("ClientSecret,ApiSecret,Password,Token,ConnectionString");

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [credentialResult, connectorResult] = await Promise.all([api.credentials(), api.connectors()]);
      setCredentials(credentialResult);
      const flattened = flattenConnectors(connectorResult);
      setConnectors(flattened);
      if (!selectedConnector && flattened.length > 0) {
        const first = flattened[0];
        setSelectedConnector(`${first.role}|${connectorValue(first.connector)}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selected = useMemo(() => {
    const [role, type] = selectedConnector.split("|");
    return connectors.find(x => x.role === role && connectorValue(x.connector) === type) ?? null;
  }, [connectors, selectedConnector]);

  async function createCredential() {
    setError(null);
    setMessage(null);

    if (!selected) {
      setError("Select a connector first.");
      return;
    }

    let values: Record<string, string | null>;
    try {
      values = JSON.parse(valuesJson);
    } catch (err) {
      setError(`Credential values must be valid JSON: ${err instanceof Error ? err.message : String(err)}`);
      return;
    }

    const created = await api.createCredential({
      displayName,
      connectorType: connectorValue(selected.connector),
      connectorRole: selected.role,
      values,
      secretKeys: secretKeys.split(",").map(x => x.trim()).filter(Boolean)
    });

    setMessage(`Created credential set ${created.credentialSetId}.`);
    await load();
  }

  async function testCredential(credentialSetId: string) {
    setError(null);
    setMessage(null);
    const result = await api.testCredential(credentialSetId);
    setMessage(`${result.success ? "Passed" : "Failed"}: ${result.message}`);
  }

  async function deleteCredential(credentialSetId: string) {
    setError(null);
    setMessage(null);
    await api.deleteCredential(credentialSetId);
    setMessage(`Deleted credential set ${credentialSetId}.`);
    await load();
  }

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <h1>Credentials</h1>
          <p>Local control-plane credential sets. Legacy hosts still use their existing appsettings and user-secrets flow.</p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />
      {message && <div className="notice">{message}</div>}

      <Card title="Create credential set">
        <div className="formGrid">
          <label>
            Display name
            <input value={displayName} onChange={e => setDisplayName(e.target.value)} />
          </label>
          <label>
            Connector
            <select value={selectedConnector} onChange={e => setSelectedConnector(e.target.value)}>
              {connectors.map(item => (
                <option key={`${item.role}|${connectorValue(item.connector)}`} value={`${item.role}|${connectorValue(item.connector)}`}>
                  {item.role}: {displayConnectorName(item.connector)}
                </option>
              ))}
            </select>
          </label>
        </div>
        <label>
          Secret keys (comma-separated)
          <input value={secretKeys} onChange={e => setSecretKeys(e.target.value)} />
        </label>
        <label>
          Values JSON
          <textarea rows={10} value={valuesJson} onChange={e => setValuesJson(e.target.value)} />
        </label>
        <button className="primaryButton" onClick={createCredential}>Save credential set</button>
      </Card>

      <Card title="Saved credential sets">
        {credentials.length === 0 ? <EmptyState title="No credential sets saved yet" /> : (
          <div className="tableWrap">
            <table>
              <thead><tr><th>Name</th><th>Connector</th><th>Role</th><th>Updated</th><th>Values</th><th>Actions</th></tr></thead>
              <tbody>
                {credentials.map(item => (
                  <tr key={item.credentialSetId}>
                    <td><strong>{item.displayName}</strong><br /><small>{item.credentialSetId}</small></td>
                    <td>{item.connectorType}</td>
                    <td>{item.connectorRole}</td>
                    <td>{new Date(item.updatedUtc).toLocaleString()}</td>
                    <td><JsonBlock value={item.values} /></td>
                    <td>
                      <button onClick={() => testCredential(item.credentialSetId)}>Test</button>{" "}
                      <button onClick={() => deleteCredential(item.credentialSetId)}>Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {selected && (
        <Card title="Selected connector schema">
          <JsonBlock value={selected.connector} />
        </Card>
      )}
    </div>
  );
}
