import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card, EmptyState, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type Role = "Source" | "Target" | "Manifest";

type ConnectorField = {
  key: string;
  label: string;
  description?: string;
  required: boolean;
  type?: string;
  source: "credentials" | "options";
};

type ConnectorFieldCandidate = {
  name?: string;
  key?: string;
  id?: string;
  field?: string;
  propertyName?: string;
  optionName?: string;
  label?: string;
  displayName?: string;
  description?: string;
  helpText?: string;
  required?: boolean;
  isRequired?: boolean;
  type?: string;
  valueType?: string;
  dataType?: string;
  defaultValue?: unknown;
  sampleValue?: unknown;
  exampleValue?: unknown;
  example?: unknown;
};

const fallbackStarterValues = JSON.stringify(
  {
    BaseUrl: "https://example.invalid",
    ClientId: "your-client-id",
    ClientSecret: "your-client-secret"
  },
  null,
  2
);

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

function asArray(value: unknown): ConnectorFieldCandidate[] {
  return Array.isArray(value) ? (value as ConnectorFieldCandidate[]) : [];
}

function fieldKey(field: ConnectorFieldCandidate): string {
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

function fieldLabel(field: ConnectorFieldCandidate, key: string): string {
  return (field.label || field.displayName || key).trim();
}

function fieldDescription(field: ConnectorFieldCandidate): string | undefined {
  return (field.description || field.helpText || undefined)?.trim();
}

function fieldType(field: ConnectorFieldCandidate): string | undefined {
  return (field.type || field.valueType || field.dataType || undefined)?.trim();
}

function normalizeFields(connector: ConnectorDescriptor | null): ConnectorField[] {
  if (!connector) {
    return [];
  }

  const fields: ConnectorField[] = [];

  function addFields(rawFields: unknown, source: "credentials" | "options") {
    for (const raw of asArray(rawFields)) {
      const key = fieldKey(raw);

      if (!key) {
        continue;
      }

      if (fields.some(existing => existing.key.toLowerCase() === key.toLowerCase())) {
        continue;
      }

      fields.push({
        key,
        label: fieldLabel(raw, key),
        description: fieldDescription(raw),
        required: Boolean(raw.required ?? raw.isRequired ?? false),
        type: fieldType(raw),
        source
      });
    }
  }

  addFields(connector.credentials, "credentials");
  addFields(connector.options, "options");

  return fields;
}

function isSecretField(field: ConnectorField): boolean {
  const haystack = `${field.key} ${field.label} ${field.type ?? ""}`.toLowerCase();

  return (
    haystack.includes("secret") ||
    haystack.includes("password") ||
    haystack.includes("token") ||
    haystack.includes("api key") ||
    haystack.includes("apikey") ||
    haystack.includes("access key") ||
    haystack.includes("private key") ||
    haystack.includes("connectionstring") ||
    haystack.includes("connection string") ||
    haystack.includes("clientsecret")
  );
}

function sampleValueForField(field: ConnectorField): unknown {
  const key = field.key.toLowerCase();
  const type = (field.type ?? "").toLowerCase();

  if (key.includes("baseurl") || key.includes("base_url") || key === "url" || key.endsWith("url")) {
    return "https://example.invalid";
  }

  if (key.includes("tenant")) {
    return "your-tenant";
  }

  if (key.includes("clientid") || key.includes("client_id")) {
    return "your-client-id";
  }

  if (key.includes("secret")) {
    return "your-client-secret";
  }

  if (key.includes("password")) {
    return "your-password";
  }

  if (key.includes("token")) {
    return "your-token";
  }

  if (key.includes("apikey") || key.includes("api_key") || key.includes("key")) {
    return "your-api-key";
  }

  if (key.includes("connectionstring") || key.includes("connection_string")) {
    return "UseDevelopmentStorage=true";
  }

  if (type.includes("bool")) {
    return false;
  }

  if (type.includes("int") || type.includes("number") || type.includes("decimal")) {
    return 0;
  }

  if (type.includes("array") || type.includes("list")) {
    return [];
  }

  if (type.includes("object")) {
    return {};
  }

  return field.required ? `your-${field.key}` : "";
}

function buildValuesTemplate(connector: ConnectorDescriptor | null): string {
  const fields = normalizeFields(connector);

  if (fields.length === 0) {
    return fallbackStarterValues;
  }

  const values = fields.reduce<Record<string, unknown>>((result, field) => {
    result[field.key] = sampleValueForField(field);
    return result;
  }, {});

  return JSON.stringify(values, null, 2);
}

function inferSecretKeys(connector: ConnectorDescriptor | null, values: Record<string, unknown>): string[] {
  const fields = normalizeFields(connector);
  const fromSchema = fields.filter(isSecretField).map(field => field.key);

  const fromValues = Object.keys(values).filter(key => {
    const field: ConnectorField = {
      key,
      label: key,
      required: false,
      source: "credentials"
    };

    return isSecretField(field);
  });

  return Array.from(new Set([...fromSchema, ...fromValues]));
}

export function Credentials() {
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [connectors, setConnectors] = useState<Array<{ role: Role; connector: ConnectorDescriptor }>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("Local test credentials");
  const [selectedConnector, setSelectedConnector] = useState("");
  const [valuesJson, setValuesJson] = useState(fallbackStarterValues);

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
        const connectorKey = `${first.role}|${connectorValue(first.connector)}`;
        setSelectedConnector(connectorKey);
        setValuesJson(buildValuesTemplate(first.connector));
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

  const selectedFields = useMemo(
    () => normalizeFields(selected?.connector ?? null),
    [selected]
  );

  const selectedSecretKeys = useMemo(() => {
    let values: Record<string, unknown> = {};

    try {
      values = JSON.parse(valuesJson);
    } catch {
      values = {};
    }

    return inferSecretKeys(selected?.connector ?? null, values);
  }, [selected, valuesJson]);

  function selectConnector(value: string) {
    setSelectedConnector(value);
    setMessage(null);
    setError(null);

    const [role, type] = value.split("|");
    const next = connectors.find(
      item => item.role === role && connectorValue(item.connector) === type
    );

    setValuesJson(buildValuesTemplate(next?.connector ?? null));
  }

  function resetValuesFromSchema() {
    setValuesJson(buildValuesTemplate(selected?.connector ?? null));
    setMessage("Values JSON reset from selected connector schema.");
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

    const missingRequired = selectedFields
      .filter(field => field.required)
      .filter(field => {
        const value = values[field.key];
        return value === undefined || value === null || value === "";
      });

    if (missingRequired.length > 0) {
      setError(
        `Missing required credential value(s): ${missingRequired.map(field => field.key).join(", ")}`
      );
      return;
    }

    const created = await api.createCredential({
      displayName,
      connectorType: connectorValue(selected.connector),
      connectorRole: selected.role,
      values,
      secretKeys: inferSecretKeys(selected.connector, values)
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
    <div className="pageStack credentialsPage">
      <div className="pageHeader">
        <div>
          <h1>Credentials</h1>
          <p className="muted">
            Local control-plane credential sets. Select a connector to generate a sample values JSON payload.
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
        <div className="formGrid credentialsForm">
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

          <label className="fullWidth">
            Values JSON
            <textarea
              rows={14}
              value={valuesJson}
              onChange={e => setValuesJson(e.target.value)}
              spellCheck={false}
            />
          </label>

          <div className="fullWidth credentialSchemaHelp">
            <div>
              <strong>Required values</strong>
              {selectedFields.filter(field => field.required).length === 0 ? (
                <p className="muted">No required values are declared by this connector schema.</p>
              ) : (
                <ul>
                  {selectedFields.filter(field => field.required).map(field => (
                    <li key={field.key}>
                      <code>{field.key}</code>
                      {field.description ? <> — {field.description}</> : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div>
              <strong>Inferred secret keys</strong>
              {selectedSecretKeys.length === 0 ? (
                <p className="muted">No secret-like fields were detected.</p>
              ) : (
                <p className="muted">
                  {selectedSecretKeys.map(key => <code key={key}>{key}</code>).reduce<React.ReactNode[]>((nodes, node, index) => {
                    if (index > 0) nodes.push(", ");
                    nodes.push(node);
                    return nodes;
                  }, [])}
                </p>
              )}
            </div>
          </div>

          <div className="buttonRow fullWidth">
            <button className="primaryButton" onClick={createCredential} disabled={loading}>
              Save credential set
            </button>

            <button className="secondaryButton" onClick={resetValuesFromSchema} type="button">
              Reset JSON from schema
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
