import { useEffect, useMemo, useState } from "react";
import { connectorConfigurationApi } from "../api/connectorConfigurationApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorDirection,
  ConnectorConfigurationValidationResponse
} from "../types/connectorConfiguration";

type EditableConnectorState = {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  settings: Record<string, string>;
};

const emptySummary: ConnectorConfigurationSummary = {
  registeredConnectors: 0,
  readyConnectors: 0,
  sourceConnectors: 0,
  targetConnectors: 0,
  attentionRequired: 0,
  lastUpdatedUtc: null,
  notes: []
};

function formatNumber(value: number | undefined | null) {
  return value === undefined || value === null ? "—" : value.toLocaleString();
}

function formatDate(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function createDraft(connector: ConnectorConfigurationCatalogItem): EditableConnectorState {
  return {
    connectorKey: connector.connectorKey,
    displayName: connector.displayName,
    direction: connector.direction,
    settings: Object.fromEntries(connector.requiredSettings.map(setting => [setting, ""]))
  };
}

export function ConnectorConfiguration() {
  const [summary, setSummary] = useState<ConnectorConfigurationSummary>(emptySummary);
  const [catalog, setCatalog] = useState<ConnectorConfigurationCatalogItem[]>([]);
  const [selectedKey, setSelectedKey] = useState("");
  const [draft, setDraft] = useState<EditableConnectorState | null>(null);
  const [validation, setValidation] = useState<ConnectorConfigurationValidationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [validating, setValidating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const [summaryResult, catalogResult] = await Promise.all([
        connectorConfigurationApi.summary(),
        connectorConfigurationApi.catalog()
      ]);
      setSummary(summaryResult ?? emptySummary);
      setCatalog(catalogResult ?? []);
      const firstConnector = (catalogResult ?? []).find(item => item.recommendedForFirstProductionLane) ?? (catalogResult ?? [])[0];
      if (firstConnector) {
        setSelectedKey(firstConnector.connectorKey);
        setDraft(createDraft(firstConnector));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const selectedConnector = useMemo(
    () => catalog.find(item => item.connectorKey === selectedKey) ?? null,
    [catalog, selectedKey]
  );

  function onConnectorChange(connectorKey: string) {
    setSelectedKey(connectorKey);
    const connector = catalog.find(item => item.connectorKey === connectorKey) ?? null;
    setDraft(connector ? createDraft(connector) : null);
    setValidation(null);
  }

  async function validateDraft() {
    if (!draft) {
      return;
    }

    setValidating(true);
    setError(null);
    setValidation(null);

    try {
      const result = await connectorConfigurationApi.validate({
        connectorKey: draft.connectorKey,
        displayName: draft.displayName,
        direction: draft.direction,
        settings: Object.fromEntries(
          Object.entries(draft.settings).map(([key, value]) => [key, value.trim() === "" ? null : value])
        )
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
        <h1>Connector Configuration</h1>
        <Card title="Loading">Loading connector configuration workspace...</Card>
      </>
    );
  }

  if (error) {
    return (
      <>
        <h1>Connector Configuration</h1>
        <LoadingError message={error} onRetry={() => void load()} />
      </>
    );
  }

  return (
    <>
      <h1>Connector Configuration</h1>
      <p>Register and validate source/target connector configuration before a migration run is launched.</p>
      <button type="button" onClick={() => void load()}>Refresh</button>

      <div className="grid three">
        <Card title="Registered connectors">{formatNumber(summary.registeredConnectors)}</Card>
        <Card title="Ready connectors">{formatNumber(summary.readyConnectors)}</Card>
        <Card title="Attention required">{formatNumber(summary.attentionRequired)}</Card>
        <Card title="Source connectors">{formatNumber(summary.sourceConnectors)}</Card>
        <Card title="Target connectors">{formatNumber(summary.targetConnectors)}</Card>
        <Card title="Last updated">{formatDate(summary.lastUpdatedUtc)}</Card>
      </div>

      <Card title="Connector catalog">
        {catalog.length === 0 ? (
          <EmptyState title="No connectors" description="No connector configuration catalog entries were returned." />
        ) : (
          <select value={selectedKey} onChange={event => onConnectorChange(event.target.value)}>
            {catalog.map(item => (
              <option key={item.connectorKey} value={item.connectorKey}>
                {item.displayName} ({item.direction})
              </option>
            ))}
          </select>
        )}

        {selectedConnector && (
          <div className="metadataList">
            <div><strong>Key:</strong> {selectedConnector.connectorKey}</div>
            <div><strong>Direction:</strong> {selectedConnector.direction}</div>
            <div><strong>First lane candidate:</strong> {selectedConnector.recommendedForFirstProductionLane ? "Yes" : "No"}</div>
          </div>
        )}
      </Card>

      <Card title="Configuration draft">
        {draft ? (
          <>
            <label>
              Display name
              <input value={draft.displayName} onChange={event => setDraft({ ...draft, displayName: event.target.value })} />
            </label>

            {Object.keys(draft.settings).length === 0 ? (
              <p>No required settings reported for this connector.</p>
            ) : (
              Object.keys(draft.settings).map(settingKey => (
                <label key={settingKey}>
                  {settingKey}
                  <input
                    value={draft.settings[settingKey]}
                    onChange={event => setDraft({
                      ...draft,
                      settings: {
                        ...draft.settings,
                        [settingKey]: event.target.value
                      }
                    })}
                  />
                </label>
              ))
            )}

            <button type="button" onClick={() => void validateDraft()} disabled={validating}>
              {validating ? "Validating..." : "Validate configuration"}
            </button>

            {validation && (
              <div className="metadataList">
                <StatusPill status={validation.isValid ? "Valid" : "Invalid"} />
                <div><strong>Validated:</strong> {formatDate(validation.validatedAtUtc)}</div>
                {validation.errors.length > 0 && (
                  <ul>
                    {validation.errors.map(errorText => <li key={errorText}>{errorText}</li>)}
                  </ul>
                )}
              </div>
            )}
          </>
        ) : (
          <EmptyState title="No connector selected" description="Select a connector to draft configuration values." />
        )}
      </Card>

      {summary.notes.length > 0 && (
        <Card title="Operator notes">
          <ul>
            {summary.notes.map(note => <li key={note}>{note}</li>)}
          </ul>
        </Card>
      )}
    </>
  );
}

