import { useEffect, useMemo, useState } from 'react';
import {
  fetchConnectorConfigurationCatalog,
  fetchConnectorConfigurationSummary,
  validateConnectorConfiguration,
} from './connectorConfigurationApi';
import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorDirection,
} from './connectorConfigurationTypes';

interface EditableConnectorState {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  settings: Record<string, string>;
}

const emptySummary: ConnectorConfigurationSummary = {
  registeredConnectors: 0,
  readyConnectors: 0,
  sourceConnectors: 0,
  targetConnectors: 0,
  attentionRequired: 0,
  lastUpdatedUtc: '',
  notes: [],
};

export function ConnectorConfigurationWorkspace() {
  const [summary, setSummary] = useState<ConnectorConfigurationSummary>(emptySummary);
  const [catalog, setCatalog] = useState<ConnectorConfigurationCatalogItem[]>([]);
  const [selectedKey, setSelectedKey] = useState<string>('');
  const [draft, setDraft] = useState<EditableConnectorState | null>(null);
  const [validationMessage, setValidationMessage] = useState<string>('Not validated yet.');
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const [summaryResponse, catalogResponse] = await Promise.all([
          fetchConnectorConfigurationSummary(),
          fetchConnectorConfigurationCatalog(),
        ]);

        if (!isMounted) {
          return;
        }

        setSummary(summaryResponse);
        setCatalog(catalogResponse);

        const firstRecommended = catalogResponse.find((item) => item.recommendedForFirstProductionLane) ?? catalogResponse[0];
        if (firstRecommended) {
          setSelectedKey(firstRecommended.connectorKey);
          setDraft(createDraft(firstRecommended));
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    load();

    return () => {
      isMounted = false;
    };
  }, []);

  const selectedConnector = useMemo(
    () => catalog.find((item) => item.connectorKey === selectedKey),
    [catalog, selectedKey],
  );

  function onConnectorChange(connectorKey: string) {
    setSelectedKey(connectorKey);
    const connector = catalog.find((item) => item.connectorKey === connectorKey);
    setDraft(connector ? createDraft(connector) : null);
    setValidationMessage('Not validated yet.');
  }

  async function onValidate() {
    if (!draft) {
      return;
    }

    const response = await validateConnectorConfiguration({
      connectorKey: draft.connectorKey,
      displayName: draft.displayName,
      direction: draft.direction,
      settings: Object.fromEntries(Object.entries(draft.settings).map(([key, value]) => [key, value || null])),
    });

    setValidationMessage(
      response.isValid
        ? `Configuration shape is valid as of ${new Date(response.validatedAtUtc).toLocaleString()}.`
        : response.errors.join(' '),
    );
  }

  if (isLoading) {
    return (
      <section className="panel">
        <h2>Connector Configuration</h2>
        <p>Loading connector configuration workspace...</p>
      </section>
    );
  }

  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <p className="eyebrow">P4.18</p>
          <h2>Connector Configuration</h2>
          <p>
            Register and validate source/target connector configuration before a migration run is launched.
            Secret persistence remains behind the control-plane credential boundary.
          </p>
        </div>
        <div className="metric-grid">
          <Metric label="Registered" value={summary.registeredConnectors} />
          <Metric label="Ready" value={summary.readyConnectors} />
          <Metric label="Attention" value={summary.attentionRequired} />
        </div>
      </div>

      <div className="workspace-grid">
        <div className="card">
          <h3>Connector catalog</h3>
          <select value={selectedKey} onChange={(event) => onConnectorChange(event.target.value)}>
            {catalog.map((item) => (
              <option key={item.connectorKey} value={item.connectorKey}>
                {item.displayName} ({item.direction})
              </option>
            ))}
          </select>

          {selectedConnector ? (
            <ul className="compact-list">
              <li>Key: {selectedConnector.connectorKey}</li>
              <li>Direction: {selectedConnector.direction}</li>
              <li>
                First lane candidate:{' '}
                {selectedConnector.recommendedForFirstProductionLane ? 'Yes' : 'No'}
              </li>
            </ul>
          ) : null}
        </div>

        <div className="card">
          <h3>Configuration draft</h3>
          {draft ? (
            <>
              <label>
                Display name
                <input
                  value={draft.displayName}
                  onChange={(event) => setDraft({ ...draft, displayName: event.target.value })}
                />
              </label>

              {Object.keys(draft.settings).map((settingKey) => (
                <label key={settingKey}>
                  {settingKey}
                  <input
                    value={draft.settings[settingKey]}
                    onChange={(event) =>
                      setDraft({
                        ...draft,
                        settings: { ...draft.settings, [settingKey]: event.target.value },
                      })
                    }
                  />
                </label>
              ))}

              <button type="button" onClick={onValidate}>
                Validate configuration
              </button>
              <p className="status-line">{validationMessage}</p>
            </>
          ) : (
            <p>No connector selected.</p>
          )}
        </div>
      </div>

      {summary.notes.length > 0 ? (
        <div className="card">
          <h3>Operator notes</h3>
          <ul className="compact-list">
            {summary.notes.map((note) => (
              <li key={note}>{note}</li>
            ))}
          </ul>
        </div>
      ) : null}
    </section>
  );
}

function createDraft(connector: ConnectorConfigurationCatalogItem): EditableConnectorState {
  return {
    connectorKey: connector.connectorKey,
    displayName: connector.displayName,
    direction: connector.direction,
    settings: Object.fromEntries(connector.requiredSettings.map((setting) => [setting, ''])),
  };
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
