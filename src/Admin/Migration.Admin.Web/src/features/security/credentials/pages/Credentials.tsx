import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../../../../api/client";
import { Card, EmptyState, JsonBlock } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary, JsonObject } from "../../../../types/api";
import "./Credentials.css";

type Role = "Source" | "Target" | "Manifest";
type NoticeKind = "success" | "error" | "info";
type PageNotice = { kind: NoticeKind; message: string };

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
};

type ConnectorListResponse = {
  sources?: ConnectorDescriptor[];
  targets?: ConnectorDescriptor[];
  manifestProviders?: ConnectorDescriptor[];
};

const emptyCredentialsJson = JSON.stringify({}, null, 2);

const bynderScopes =
  "offline admin.profile:read admin.user:read admin.user:write current.profile:read current.user:read asset:read asset:write asset.usage:read asset.usage:write collection:read collection:write meta.assetbank:read meta.assetbank:write meta.workflow:read workflow.campaign:read workflow.campaign:write workflow.group:read workflow.group:write workflow.job:read workflow.job:write workflow.preset:read brandstore.order:read brandstore.order:write";

const bynderTargetCredentialFields: CredentialField[] = [
  {
    key: "BaseUrl",
    label: "Base URL",
    description: "Bynder portal URL, for example https://yourbrand.bynder.com/.",
    required: true,
    secret: false,
    configurationKey: "Bynder:Client:BaseUrl",
    defaultValue: "https://example.bynder.com/"
  },
  {
    key: "ClientId",
    label: "Client ID",
    description: "OAuth client id from Bynder.",
    required: true,
    secret: true,
    configurationKey: "Bynder:Client:ClientId"
  },
  {
    key: "ClientSecret",
    label: "Client Secret",
    description: "OAuth client secret from Bynder.",
    required: true,
    secret: true,
    configurationKey: "Bynder:Client:ClientSecret"
  },
  {
    key: "Scopes",
    label: "Scopes",
    description: "OAuth scopes used by the Bynder client.",
    required: true,
    secret: false,
    configurationKey: "Bynder:Client:Scopes",
    defaultValue: bynderScopes
  },
  {
    key: "BrandStoreId",
    label: "Brand Store ID",
    description: "Bynder brand store id used by the target connector.",
    required: true,
    secret: false,
    configurationKey: "Bynder:BrandStoreId",
    defaultValue: "your-brandstore-id"
  }
];

const cloudinaryTargetCredentialFields: CredentialField[] = [
  {
    key: "CloudName",
    label: "Cloud name",
    description: "Cloudinary cloud name.",
    required: true,
    secret: false,
    defaultValue: "your-cloud-name"
  },
  {
    key: "ApiKey",
    label: "API key",
    description: "Cloudinary API key.",
    required: true,
    secret: true
  },
  {
    key: "ApiSecret",
    label: "API secret",
    description: "Cloudinary API secret.",
    required: true,
    secret: true
  },
  {
    key: "CLOUDINARY_URL",
    label: "Cloudinary URL",
    description: "Optional alternative format: cloudinary://apiKey:apiSecret@cloudName.",
    required: false,
    secret: true
  }
];

function flattenConnectors(data: ConnectorListResponse | null) {
  const result: Array<{ role: Role; connector: ConnectorDescriptor }> = [];
  for (const connector of data?.sources ?? []) result.push({ role: "Source", connector });
  for (const connector of data?.targets ?? []) result.push({ role: "Target", connector });
  for (const connector of data?.manifestProviders ?? []) result.push({ role: "Manifest", connector });
  return result;
}

function descriptorFields(value: unknown): DescriptorField[] {
  return Array.isArray(value) ? (value as DescriptorField[]) : [];
}

function getFieldKey(field: DescriptorField) {
  return (field.name || field.key || field.id || field.field || field.propertyName || field.optionName || "").trim();
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

function connectorTypeText(connector: ConnectorDescriptor | null | undefined) {
  return `${connector?.type ?? ""} ${connector?.name ?? ""} ${connector?.displayName ?? ""}`.toLowerCase();
}

function isBynderTarget(role: Role | undefined, connector: ConnectorDescriptor | null | undefined) {
  return role === "Target" && connectorTypeText(connector).includes("bynder");
}

function isCloudinaryTarget(role: Role | undefined, connector: ConnectorDescriptor | null | undefined) {
  return role === "Target" && connectorTypeText(connector).includes("cloudinary");
}

function isOptionalCredentialOverride(connector: ConnectorDescriptor | null, key: string) {
  const connectorType = connectorValue(connector).toLowerCase();
  const normalizedKey = key.toLowerCase();
  if (connectorType === "webdam") {
    return normalizedKey === "accesstoken" || normalizedKey === "refreshtoken";
  }

  return false;
}

function normalizeCredentialFields(role: Role | undefined, connector: ConnectorDescriptor | null): CredentialField[] {
  if (!connector) return [];

  if (isBynderTarget(role, connector)) {
    return bynderTargetCredentialFields;
  }

  if (isCloudinaryTarget(role, connector)) {
    return cloudinaryTargetCredentialFields;
  }

  return descriptorFields(connector.credentials)
      .map((field): CredentialField | null => {
      const key = getFieldKey(field);
      if (!key) return null;
      const requiredFromSchema = Boolean(field.required ?? field.isRequired ?? false);
      return {
        key,
        label: (field.displayName || field.label || key).trim(),
        description: (field.description || field.helpText || undefined)?.trim(),
        required: isOptionalCredentialOverride(connector, key) ? false : requiredFromSchema,
        secret: Boolean(field.secret ?? field.isSecret ?? isSecretLikeKey(key)),
        configurationKey: field.configurationKey,
        defaultValue: field.defaultValue ?? field.sampleValue ?? field.exampleValue ?? field.example
      } satisfies CredentialField;
    })
    .filter((field): field is CredentialField => field !== null);
}

function referenceNameForCredential(connector: ConnectorDescriptor | null, field: CredentialField) {
  const connectorKey = connectorValue(connector)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  const fieldKey = field.key
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return `kv://${connectorKey || "connector"}-${fieldKey || "secret"}`;
}

function sampleValueForCredential(connector: ConnectorDescriptor | null, field: CredentialField): unknown {
  if (field.secret) {
    return referenceNameForCredential(connector, field);
  }

  if (field.defaultValue !== undefined && field.defaultValue !== null && field.defaultValue !== "") {
    return field.defaultValue;
  }

  const key = field.key.toLowerCase();
  if (key === "baseurl" || key === "base_url" || key.endsWith("url")) return "https://example.invalid";
  if (key === "scopes" || key === "scope") return bynderScopes;
  if (key.includes("brandstore")) return "your-brandstore-id";
  if (key.includes("cloudname") || key.includes("cloud_name")) return "your-cloud-name";
  if (key.includes("region")) return "us-east-1";
  if (key.includes("bucket")) return "your-bucket-name";
  if (key.includes("username") || key.includes("user_name")) return "your-username";
  if (!field.required) return "";
  return `your-${field.key}`;
}

function buildCredentialValuesJson(role: Role | undefined, connector: ConnectorDescriptor | null) {
  const fields = normalizeCredentialFields(role, connector);
  if (fields.length === 0) return emptyCredentialsJson;

  const values = fields.reduce<Record<string, unknown>>((result, field) => {
    result[field.key] = sampleValueForCredential(connector, field);
    return result;
  }, {});

  return JSON.stringify(values, null, 2);
}

function inferSecretKeys(fields: CredentialField[]) {
  return fields.filter(field => field.secret).map(field => field.key);
}

function getConnectorOptions(connector: ConnectorDescriptor | null): DescriptorField[] {
  return descriptorFields(connector?.options);
}

function isMissingRequiredValue(value: unknown) {
  return value === undefined || value === null || (typeof value === "string" && value.trim() === "");
}

function isSecretReference(value: unknown) {
  if (typeof value !== "string") return false;
  const text = value.trim().toLowerCase();
  return text.startsWith("kv://")
    || text.startsWith("keyvault://")
    || text.startsWith("env://")
    || (text.startsWith("https://") && text.includes(".vault.azure.net/secrets/"));
}

function noticeClassName(kind: NoticeKind) {
  return `credentialsNotice credentialsNotice--${kind}`;
}

export function Credentials() {
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [connectors, setConnectors] = useState<Array<{ role: Role; connector: ConnectorDescriptor }>>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testingCredentialId, setTestingCredentialId] = useState<string | null>(null);
  const [deletingCredentialId, setDeletingCredentialId] = useState<string | null>(null);
  const [formNotice, setFormNotice] = useState<PageNotice | null>(null);
  const [savedSetsNotice, setSavedSetsNotice] = useState<PageNotice | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("Bynder target credentials");
  const [selectedConnector, setSelectedConnector] = useState("");
  const [valuesJson, setValuesJson] = useState(emptyCredentialsJson);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [credentialResult, connectorResult] = await Promise.all([api.credentials(), api.connectors()]);
      setCredentials(credentialResult ?? []);
      const flattened = flattenConnectors(connectorResult as ConnectorListResponse);
      setConnectors(flattened);

      if (!selectedConnector && flattened.length > 0) {
        const bynderTarget = flattened.find(item => isBynderTarget(item.role, item.connector));
        const cloudinaryTarget = flattened.find(item => isCloudinaryTarget(item.role, item.connector));
        const first = bynderTarget ?? cloudinaryTarget ?? flattened[0];
        setSelectedConnector(`${first.role}|${connectorValue(first.connector)}`);
        setValuesJson(buildCredentialValuesJson(first.role, first.connector));
        if (bynderTarget) setDisplayName("Bynder target credentials");
        else if (cloudinaryTarget) setDisplayName("Cloudinary target credentials");
        else setDisplayName(`${displayConnectorName(first.connector)} ${first.role.toLowerCase()} credentials`);
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
    const [role, type] = selectedConnector.split("|") as [Role | undefined, string | undefined];
    return connectors.find(item => item.role === role && connectorValue(item.connector) === type) ?? null;
  }, [connectors, selectedConnector]);

  const credentialFields = useMemo(
    () => normalizeCredentialFields(selected?.role, selected?.connector ?? null),
    [selected]
  );

  const requiredCredentialFields = useMemo(
    () => credentialFields.filter(field => field.required),
    [credentialFields]
  );

  const optionalCredentialFields = useMemo(
    () => credentialFields.filter(field => !field.required),
    [credentialFields]
  );

  const secretKeys = useMemo(() => inferSecretKeys(credentialFields), [credentialFields]);
  const connectorOptions = useMemo(() => getConnectorOptions(selected?.connector ?? null), [selected]);

  function selectConnector(value: string) {
    setSelectedConnector(value);
    setFormNotice(null);
    setSavedSetsNotice(null);
    setError(null);

    const [role, type] = value.split("|") as [Role | undefined, string | undefined];
    const next = connectors.find(item => item.role === role && connectorValue(item.connector) === type);
    setValuesJson(buildCredentialValuesJson(next?.role, next?.connector ?? null));

    if (next && isBynderTarget(next.role, next.connector)) {
      setDisplayName("Bynder target credentials");
    } else if (next && isCloudinaryTarget(next.role, next.connector)) {
      setDisplayName("Cloudinary target credentials");
    } else if (next) {
      setDisplayName(`${displayConnectorName(next.connector)} ${next.role.toLowerCase()} credentials`);
    }
  }

  function resetValuesFromSchema() {
    setValuesJson(buildCredentialValuesJson(selected?.role, selected?.connector ?? null));
    setFormNotice({ kind: "info", message: "Values JSON reset from selected connector credential schema using Key Vault references for secret fields." });
  }

  async function createCredential() {
    setError(null);
    setFormNotice(null);
    setSavedSetsNotice(null);

    if (!selected) {
      setFormNotice({ kind: "error", message: "Select a connector before saving a credential set." });
      return;
    }

    if (credentialFields.length === 0) {
      setFormNotice({ kind: "error", message: "This connector does not declare credential fields." });
      return;
    }

    let values: Record<string, unknown>;
    try {
      values = JSON.parse(valuesJson) as Record<string, unknown>;
    } catch (err) {
      setFormNotice({ kind: "error", message: `Values JSON is invalid: ${err instanceof Error ? err.message : String(err)}` });
      return;
    }

    const missingRequired = requiredCredentialFields.filter(field => isMissingRequiredValue(values[field.key]));
    if (missingRequired.length > 0) {
      setFormNotice({
        kind: "error",
        message: `Missing required credential value(s): ${missingRequired.map(field => field.key).join(", ")}. Optional fields may be left blank.`
      });
      return;
    }

    const rawSecretFields = credentialFields.filter(field => field.secret && !isMissingRequiredValue(values[field.key]) && !isSecretReference(values[field.key]));
    if (rawSecretFields.length > 0) {
      setFormNotice({
        kind: "error",
        message: `Secret fields must be references, not raw values: ${rawSecretFields.map(field => field.key).join(", ")}. Use kv://secret-name or a full Key Vault secret URI.`
      });
      return;
    }

    setSaving(true);
    try {
      const created = await api.createCredential({
        displayName,
        connectorType: connectorValue(selected.connector),
        connectorRole: selected.role,
        values: values as JsonObject,
        secretKeys
      });
      setFormNotice({ kind: "success", message: `Created credential set ${created.credentialSetId}.` });
      await load();
    } catch (err) {
      setFormNotice({ kind: "error", message: `Failed to save credential set: ${err instanceof Error ? err.message : String(err)}` });
    } finally {
      setSaving(false);
    }
  }

  async function testCredential(credentialSetId: string) {
    setError(null);
    setFormNotice(null);
    setSavedSetsNotice(null);
    setTestingCredentialId(credentialSetId);
    try {
      const result = await api.testCredential(credentialSetId);
      setSavedSetsNotice({
        kind: result.success ? "success" : "error",
        message: `${result.success ? "Credential test passed" : "Credential test failed"} for ${credentialSetId}: ${result.message}`
      });
    } catch (err) {
      setSavedSetsNotice({ kind: "error", message: `Credential test failed for ${credentialSetId}: ${err instanceof Error ? err.message : String(err)}` });
    } finally {
      setTestingCredentialId(null);
    }
  }

  async function deleteCredential(credentialSetId: string) {
    setError(null);
    setFormNotice(null);
    setSavedSetsNotice(null);
    setDeletingCredentialId(credentialSetId);
    try {
      await api.deleteCredential(credentialSetId);
      setSavedSetsNotice({ kind: "success", message: `Deleted credential set ${credentialSetId}.` });
      await load();
    } catch (err) {
      setSavedSetsNotice({ kind: "error", message: `Failed to delete credential set ${credentialSetId}: ${err instanceof Error ? err.message : String(err)}` });
    } finally {
      setDeletingCredentialId(null);
    }
  }

  if (loading) {
    return (
      <div className="credentialsPage">
        <h1>Credentials</h1>
        <p className="muted">Loading credential sets and connector schemas...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="credentialsPage">
        <h1>Credentials</h1>
        <LoadingError message={error} onRetry={() => void load()} />
      </div>
    );
  }

  return (
    <div className="credentialsPage">
      <div className="pageHeader">
        <div>
          <h1>Credentials</h1>
          <p>
            Create connector credential sets for migration projects. Secret fields are stored as references only; put the actual secret values in Azure Key Vault.
          </p>
        </div>
        <button type="button" onClick={() => void load()}>Refresh</button>
      </div>

      <Card title="Create credential set">
        <div className="credentialsCallout">
          <strong>Cloud-safe mode:</strong> secret fields must use <code>kv://secret-name</code>, <code>env://VARIABLE_NAME</code>, or a full Key Vault secret URI. Raw secrets are rejected outside local development.
        </div>

        <div className="credentialsFormGrid">
          <label>
            Display name
            <input value={displayName} onChange={event => setDisplayName(event.target.value)} />
          </label>
          <label>
            Connector
            <select value={selectedConnector} onChange={event => selectConnector(event.target.value)}>
              {connectors.map(item => {
                const value = `${item.role}|${connectorValue(item.connector)}`;
                return (
                  <option key={value} value={value}>
                    {item.role}: {displayConnectorName(item.connector)}
                  </option>
                );
              })}
            </select>
          </label>
        </div>

        {formNotice && <div className={noticeClassName(formNotice.kind)}>{formNotice.message}</div>}

        {credentialFields.length === 0 ? (
          <EmptyState title="No credential schema" message="This connector does not declare credential fields." />
        ) : (
          <>
            <label className="credentialsJsonLabel">
              Values JSON
              <textarea
                className="credentialsJsonTextarea"
                value={valuesJson}
                onChange={event => setValuesJson(event.target.value)}
                spellCheck={false}
                rows={18}
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
                        {field.secret ? <span className="credentialSecretBadge">secret reference</span> : null}
                        {field.description ? <> - {field.description}</> : null}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h3>Optional credential values</h3>
                {optionalCredentialFields.length === 0 ? (
                  <p className="muted">No optional credential values are declared.</p>
                ) : (
                  <ul>
                    {optionalCredentialFields.map(field => (
                      <li key={field.key}>
                        <code>{field.key}</code>
                        {field.secret ? <span className="credentialSecretBadge">secret reference</span> : null}
                        {field.description ? <> - {field.description}</> : null}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h3>Secret reference fields</h3>
                {secretKeys.length === 0 ? (
                  <p className="muted">No secret credential fields are declared.</p>
                ) : (
                  <ul>
                    {secretKeys.map(key => (
                      <li key={key}><code>{key}</code></li>
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
                    This connector also declares {connectorOptions.length} option(s). These are intentionally not included in credential values.
                  </p>
                )}
              </section>
            </div>
          </>
        )}

        <div className="buttonRow">
          <button type="button" className="primaryButton" onClick={() => void createCredential()} disabled={loading || saving || !selected || credentialFields.length === 0}>
            {saving ? "Saving..." : "Save credential set"}
          </button>
          <button type="button" className="secondaryButton" onClick={resetValuesFromSchema} disabled={!selected || saving}>
            Reset JSON from schema
          </button>
        </div>
      </Card>

      <Card title="Saved credential sets">
        {savedSetsNotice && <div className={noticeClassName(savedSetsNotice.kind)}>{savedSetsNotice.message}</div>}
        {credentials.length === 0 ? (
          <EmptyState title="No credential sets saved yet" />
        ) : (
          <div className="tableWrap credentialsTableWrap">
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
                      {item.secretKeys?.length > 0 ? item.secretKeys.map(key => <code key={key}>{key} </code>) : <span className="muted">None</span>}
                    </td>
                    <td>
                      <div className="credentialsActionRow">
                        <button type="button" onClick={() => void testCredential(item.credentialSetId)} disabled={testingCredentialId === item.credentialSetId}>
                          {testingCredentialId === item.credentialSetId ? "Testing..." : "Test"}
                        </button>
                        <button type="button" onClick={() => void deleteCredential(item.credentialSetId)} disabled={deletingCredentialId === item.credentialSetId}>
                          {deletingCredentialId === item.credentialSetId ? "Deleting..." : "Delete"}
                        </button>
                      </div>
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
          <div className="credentialsSchemaBlock">
            <JsonBlock value={selected.connector} />
          </div>
        </Card>
      )}
    </div>
  );
}
