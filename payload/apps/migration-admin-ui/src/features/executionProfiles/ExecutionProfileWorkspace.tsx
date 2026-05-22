import { useEffect, useState } from 'react';
import {
  fetchConnectorExecutionProfileCatalog,
  fetchConnectorExecutionProfileSummary,
  validateConnectorExecutionProfile,
} from './executionProfileApi';
import type {
  ConnectorExecutionProfileCatalogItem,
  ConnectorExecutionProfileSummary,
  ConnectorExecutionProfileValidationResponse,
} from './executionProfileTypes';

export function ExecutionProfileWorkspace() {
  const [summary, setSummary] = useState<ConnectorExecutionProfileSummary | null>(null);
  const [profiles, setProfiles] = useState<ConnectorExecutionProfileCatalogItem[]>([]);
  const [validation, setValidation] = useState<ConnectorExecutionProfileValidationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const [summaryResult, catalogResult] = await Promise.all([
          fetchConnectorExecutionProfileSummary(),
          fetchConnectorExecutionProfileCatalog(),
        ]);

        if (isMounted) {
          setSummary(summaryResult);
          setProfiles(catalogResult);
        }
      } catch (loadError) {
        if (isMounted) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load execution profiles.');
        }
      }
    }

    load();

    return () => {
      isMounted = false;
    };
  }, []);

  async function validateProfile(profile: ConnectorExecutionProfileCatalogItem) {
    setValidation(null);
    setError(null);

    try {
      const result = await validateConnectorExecutionProfile({
        profileId: profile.profileId,
        connectorScope: profile.connectorScope,
        maxConcurrency: profile.maxConcurrency,
        maxAttempts: profile.maxAttempts,
        retryDelaySeconds: profile.retryDelaySeconds,
        throttlePerMinute: profile.throttlePerMinute,
      });

      setValidation(result);
    } catch (validationError) {
      setError(validationError instanceof Error ? validationError.message : 'Unable to validate execution profile.');
    }
  }

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">P4.20</p>
          <h2>Connector execution profiles</h2>
          <p>Review connector throttling, retry, backoff, and concurrency policy presets.</p>
        </div>
      </div>

      {summary && (
        <div className="metric-grid">
          <div className="metric-card">
            <span>Total profiles</span>
            <strong>{summary.totalProfiles}</strong>
          </div>
          <div className="metric-card">
            <span>Source profiles</span>
            <strong>{summary.sourceProfiles}</strong>
          </div>
          <div className="metric-card">
            <span>Target profiles</span>
            <strong>{summary.targetProfiles}</strong>
          </div>
          <div className="metric-card">
            <span>Default</span>
            <strong>{summary.defaultProfileId}</strong>
          </div>
        </div>
      )}

      {error && <p className="error-text">{error}</p>}

      <div className="table-shell">
        <table>
          <thead>
            <tr>
              <th>Profile</th>
              <th>Scope</th>
              <th>Concurrency</th>
              <th>Attempts</th>
              <th>Retry delay</th>
              <th>Throttle/min</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {profiles.map((profile) => (
              <tr key={profile.profileId}>
                <td>
                  <strong>{profile.displayName}</strong>
                  {profile.isDefault && <span className="pill">Default</span>}
                </td>
                <td>{profile.connectorScope}</td>
                <td>{profile.maxConcurrency}</td>
                <td>{profile.maxAttempts}</td>
                <td>{profile.retryDelaySeconds}s</td>
                <td>{profile.throttlePerMinute}</td>
                <td>
                  <button type="button" onClick={() => validateProfile(profile)}>
                    Validate
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {validation && (
        <div className="callout">
          <strong>{validation.isValid ? 'Valid profile' : 'Profile needs review'}</strong>
          <ul>
            {validation.findings.map((finding) => (
              <li key={finding}>{finding}</li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
