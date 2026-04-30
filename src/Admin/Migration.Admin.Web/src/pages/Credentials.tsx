import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, EmptyState, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type Role = "Source" | "Target" | "Manifest";

type CredentialField = {
  key: string;
  label: string;
  description?: string;
  required: boolean;
  secret: boolean;
  configurationKey?: string;
  defaultValue?: unknown;
};

type DescriptorField = {
  name?: string;
  key?: string;
  id?: string;
  field?: string;
  propertyName?: string;
  optionName?: string;
  displayName?: string;
  label?: string;
  description?: string;
  helpText?: string;
  required?: boolean;
  isRequired?: boolean;
  secret?: boolean;
  isSecret?: boolean;
  configurationKey?: string;
  defaultValue?: unknown;
  sampleValue?: unknown;
  exampleValue?: unknown;
  example?: unknown;
  type?: string;
  valueType?: string;
  dataType?: string;
};

const emptyCredentialsJson = JSON.stringify({}, null, 2);

function flattenConnectors(data: {
  sources?: ConnectorDescriptor[];
  targets?: ConnectorDescriptor[];
  manifestProviders?: ConnectorDescriptor[];
} | null) {
  const result: Array<{ role: Role; connector: ConnectorDescriptor }> = [];

  for (const connector of data?.sources ?? []) {
    result.push({ role: "Source", connector });
  }

  for (const connector of data?.targets ?? []) {
    result.push({ role: "Target", connector });
  }

  for (const connector of data?.manifestProviders ?? []) {
    result.push({ role: "Manifest", connector });
  }

  return result;
}

function descriptorFields(value: unknown): DescriptorField[] {
  return Array.isArray(value) ? (value as DescriptorField[]) : [];
}

function getFieldKey(field: DescriptorField) {
  return (
    field.name ||
    field.key ||
    field.id ||
    field.field ||
    field.propertyName ||
    field.optionName ||
    ""
  ).trim();
}

function isSecretLikeKey(key: string) {
  const text = key.toLowerCase();

  return (
    text.includes("secret") ||
    text.includes("password") ||
    text.includes("token") ||
    text.includes("apikey") ||
    text.includes("api_key") ||
    text.includes("api key") ||
    text.includes("accesskey") ||
    text.includes("access_key") ||
    text.includes("privatekey") ||
    text.includes("private_key") ||
    text.includes("connectionstring") ||
    text.includes("connection_string")
  );
}

function normalizeCredentialFields(connector: ConnectorDescriptor | null): CredentialField[] {
  if (!connector) {
    return [];
  }

  return descriptorFields(connector.credentials)
    .map(field => {
      const key = getFieldKey(field);

      if (!key) {
        return null;
      }

      return {
        key,
        label: (field.displayName || field.label || key).trim(),
        description: (field.description || field.helpText || undefined)?.trim(),
        required: Boolean(field.required ?? field.isRequired ?? false),
        secret: Boolean(field.secret ?? field.isSecret ?? isSecretLikeKey(key)),
        configurationKey: field.configurationKey,
        defaultValue: field.defaultValue ?? field.sampleValue ?? field.exampleValue ?? field.example
      } satisfies CredentialField;
    })
    .filter((field): field is CredentialField => field !== null);
}

function sampleValueForCredential(field: CredentialField): unknown {
  if (field.defaultValue !== undefined && field.defaultValue !== null && field.defaultValue !== "") {
    return field.defaultValue;
  }

  const key = field.key.toLowerCase();

  if (key === "baseurl" || key === "base_url" || key.endsWith("url")) {
    return "https://example.invalid";
  }

  if (key.includes("clientid") || key.includes("client_id")) {
    return "your-client-id";
  }

  if (key.includes("consumerkey") || key.includes("consumer_key")) {
    return "your-consumer-key";
  }

  if (key.includes("consumersecret") || key.includes("consumer_secret")) {
    return "your-consumer-secret";
  }

  if (key.includes("clientsecret") || key.includes("client_secret")) {
    return "your-client-secret";
  }

  if (key.includes("refresh")) {
    return "your-refresh-token";
  }

  if (key.includes("token")) {
    return "your-token";
  }

  if (key.includes("password")) {
    return "your-password";
  }

  if (key.includes("username") || key.includes("user_name")) {
    return "your-username";
  }

  if (key.includes("apikey") || key.includes("api_key") || key === "key") {
    return "your-api-key";
  }

  if (key.includes("connectionstring") || key.includes("connection_string")) {
    return "UseDevelopmentStorage=true";
  }

  if (!field.required) {
    return "";
  }

  return `your-${field.key}`;
}

function buildCredentialValuesJson(connector: ConnectorDescriptor | null) {
  const fields = normalizeCredentialFields(connector);

  if (fields.length === 0) {
    return emptyCredentialsJson;
  }

  const values = fields.reduce<Record<string, unknown>>((result, field) => {
    result[field.key] = sampleValueForCredential(field);
    return result;
  }, {});

  return JSON.stringify(values, null, 2);
}

function inferSecretKeys(fields: CredentialField[]) {
  return fields
    .filter(field => field.secret)
    .map(field => field.key);
}

function getConnectorOptions(connector: ConnectorDescriptor | null): DescriptorField[] {
  return descriptorFields(connector?.options);
}

export function Credentials() {
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [connectors, setConnectors] = useState<Array<{ role: Role; connector: ConnectorDescriptor }>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("Local test credentials");
  const [selectedConnector, setSelectedConnector] = useState("");
  const [valuesJson, setValuesJson] = useState(emptyCredentialsJson);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const [credentialResult, connectorResult] = await Promise.all([
        api.credentials(),
        api.connectors()
      ]);

      setCredentials(credentialResult);

      const flattened = flattenConnectors(connectorResult);
      setConnectors(flattened);

      if (!selectedConnector && flattened.length > 0) {
        const first = flattened[0];
        setSelectedConnector(`${first.role}|${connectorValue(first.connector)}`);
        setValuesJson(buildCredentialValuesJson(first.connector));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selected = useMemo(() => {
    const [role, type] = selectedConnector.split("|");

    return connectors.find(
      item => item.role === role && connectorValue(item.connector) === type
    ) ?? null;
  }, [connectors, selectedConnector]);

  const credentialFields = useMemo(
    () => normalizeCredentialFields(selected?.connector ?? null),
    [selected]
  );

  const requiredCredentialFields = useMemo(
    () => credentialFields.filter(field => field.required),
    [credentialFields]
  );

  const secretKeys = useMemo(
    () => inferSecretKeys(credentialFields),
    [credentialFields]
  );

  const connectorOptions = useMemo(
    () => getConnectorOptions(selected?.connector ?? null),
    [selected]
  );

  function selectConnector(value: string) {
    setSelectedConnector(value);
    setMessage(null);
    setError(null);

    const [role, type] = value.split("|");
    const next = connectors.find(
      item => item.role === role && connectorValue(item.connector) === type
    );

    setValuesJson(buildCredentialValuesJson(next?.connector ?? null));
  }

  function resetValuesFromSchema() {
    setValuesJson(buildCredentialValuesJson(selected?.connector ?? null));
    setMessage("Values JSON reset from selected connector credential schema.");
  }

  async function createCredential() {
    setError(null);
    setMessage(null);

    if (!selected) {
      setError("Select a connector first.");
      return;
    }

    let values: Record<string, unknown>;

    try {
      values = JSON.parse(valuesJson);
    } catch (err) {
      setError(`Credential values must be valid JSON: ${err instanceof Error ? err.message : String(err)}`);
      return;
    }

    const missingRequired = requiredCredentialFields.filter(field => {
      const value = values[field.key];
      return value === undefined || value === null || value === "";
    });

    if (missingRequired.length > 0) {
      setError(
        `Missing required credential value(s): ${missingRequired.map(field => field.key).join(", ")}`
      );
      return;
    }

try {
  const created = await api.createCredential({
    displayName,
    connectorType: connectorValue(selected.connector),
    connectorRole: selected.role,
    values,
    secretKeys
  });

  setMessage(`Created credential set ${created.credentialSetId}.`);
  await load();
} catch (err) {
  setError(err instanceof Error ? err.message : String(err));
}

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
    <div className="pageStack credentialsPage">
      <div className="pageHeader">
        <div>
          <h1>Credentials</h1>
          <p className="muted">
            Credential sets should contain only the values required to authenticate/connect to a connector.
            Connector options belong in project, run, or settings profiles.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      {message && (
        <div className="successBanner">
          {message}
        </div>
      )}

      <Card title="Create credential set">
        <div className="credentialsForm">
          <div className="credentialsFormTopRow">
            <label>
              Display name
              <input value={displayName} onChange={e => setDisplayName(e.target.value)} />
            </label>

            <label>
              Connector
              <select value={selectedConnector} onChange={e => selectConnector(e.target.value)}>
                {connectors.map(item => (
                  <option
                    key={`${item.role}|${connectorValue(item.connector)}`}
                    value={`${item.role}|${connectorValue(item.connector)}`}
                  >
                    {item.role}: {displayConnectorName(item.connector)}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {credentialFields.length === 0 ? (
            <div className="credentialNotice">
              <strong>No credentials required.</strong>
              <p className="muted">
                This connector does not declare credential fields. Its configurable values are connector options,
                not credential values.
              </p>
            </div>
          ) : (
            <>
              <label className="credentialsJsonField">
                Values JSON
                <textarea
                  rows={14}
                  value={valuesJson}
                  onChange={e => setValuesJson(e.target.value)}
                  spellCheck={false}
                />
              </label>

              <div className="credentialSchemaSummary">
                <section>
                  <h3>Required credential values</h3>
                  {requiredCredentialFields.length === 0 ? (
                    <p className="muted">No required credential values are declared.</p>
                  ) : (
                    <ul>
                      {requiredCredentialFields.map(field => (
                        <li key={field.key}>
                          <code>{field.key}</code>
                          {field.description ? <> — {field.description}</> : null}
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section>
                  <h3>Secret fields</h3>
                  {secretKeys.length === 0 ? (
                    <p className="muted">No secret credential fields are declared.</p>
                  ) : (
                    <ul>
                      {secretKeys.map(key => (
                        <li key={key}>
                          <code>{key}</code>
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section>
                  <h3>Connector options</h3>
                  {connectorOptions.length === 0 ? (
                    <p className="muted">No separate connector options are declared.</p>
                  ) : (
                    <p className="muted">
                      This connector also declares {connectorOptions.length} option(s). These are intentionally not
                      included in credential values.
                    </p>
                  )}
                </section>
              </div>
            </>
          )}

          <div className="buttonRow">
            <button
              className="primaryButton"
              onClick={createCredential}
              disabled={loading || !selected || credentialFields.length === 0}
            >
              Save credential set
            </button>

            <button
              className="secondaryButton"
              onClick={resetValuesFromSchema}
              type="button"
              disabled={!selected}
            >
              Reset JSON from credentials schema
            </button>
          </div>
        </div>
      </Card>

      <Card title="Saved credential sets">
        {credentials.length === 0 ? (
          <EmptyState title="No credential sets saved yet" />
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Connector</th>
                  <th>Role</th>
                  <th>Updated</th>
                  <th>Values</th>
                  <th>Secret keys</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {credentials.map(item => (
                  <tr key={item.credentialSetId}>
                    <td>
                      <strong>{item.displayName}</strong>
                      <br />
                      <small>{item.credentialSetId}</small>
                    </td>
                    <td>{item.connectorType}</td>
                    <td>{item.connectorRole}</td>
                    <td>{new Date(item.updatedUtc).toLocaleString()}</td>
                    <td><JsonBlock value={item.values} /></td>
                    <td>
                      {item.secretKeys?.length > 0 ? (
                        item.secretKeys.map(key => <code key={key}>{key} </code>)
                      ) : (
                        <span className="muted">None</span>
                      )}
                    </td>
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
