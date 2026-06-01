import { useEffect, useState } from "react";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import { executionProfilesApi } from "../api/executionProfilesApi";
import type {
  ConnectorExecutionProfileCatalogItem,
  ConnectorExecutionProfileSummary,
  ConnectorExecutionProfileValidationResponse,
} from "../types/executionProfiles";

export function ExecutionProfiles() {
  const [summary, setSummary] = useState<ConnectorExecutionProfileSummary | null>(null);
  const [profiles, setProfiles] = useState<ConnectorExecutionProfileCatalogItem[]>([]);
  const [validation, setValidation] = useState<ConnectorExecutionProfileValidationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [validatingProfileId, setValidatingProfileId] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      setIsLoading(true);
      setError(null);

      try {
        const [summaryResult, catalogResult] = await Promise.all([
          executionProfilesApi.getSummary(),
          executionProfilesApi.getCatalog(),
        ]);

        if (isMounted) {
          setSummary(summaryResult);
          setProfiles(catalogResult);
        }
      } catch (loadError) {
        if (isMounted) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load execution profiles.");
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    void load();

    return () => {
      isMounted = false;
    };
  }, []);

  async function validateProfile(profile: ConnectorExecutionProfileCatalogItem) {
    setValidation(null);
    setError(null);
    setValidatingProfileId(profile.profileId);

    try {
      const result = await executionProfilesApi.validate({
        profileId: profile.profileId,
        connectorScope: profile.connectorScope,
        maxConcurrency: profile.maxConcurrency,
        maxAttempts: profile.maxAttempts,
        retryDelaySeconds: profile.retryDelaySeconds,
        throttlePerMinute: profile.throttlePerMinute,
      });

      setValidation(result);
    } catch (validationError) {
      setError(validationError instanceof Error ? validationError.message : "Unable to validate execution profile.");
    } finally {
      setValidatingProfileId(null);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Execution Profiles</h1>
          <p className="muted">
            Review connector throttling, retry, backoff, and concurrency policy presets from the canonical Admin Web.
          </p>
        </div>
      </div>

      {isLoading && <Card title="Loading execution profiles">Loading execution profile catalog...</Card>}
      {error && <LoadingError message={error} />}

      {summary && (
        <div className="metric-grid">
          <Card title="Total profiles"><strong>{summary.totalProfiles}</strong></Card>
          <Card title="Source profiles"><strong>{summary.sourceProfiles}</strong></Card>
          <Card title="Target profiles"><strong>{summary.targetProfiles}</strong></Card>
          <Card title="Default profile"><strong>{summary.defaultProfileId}</strong></Card>
        </div>
      )}

      <Card title="Connector execution profile catalog">
        <div className="table-wrap">
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
                    <button
                      className="button-secondary"
                      type="button"
                      disabled={validatingProfileId === profile.profileId}
                      onClick={() => void validateProfile(profile)}
                    >
                      {validatingProfileId === profile.profileId ? "Validating..." : "Validate"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {validation && (
        <Card title={validation.isValid ? "Valid profile" : "Profile needs review"}>
          {validation.findings.length === 0 ? (
            <p>No findings returned.</p>
          ) : (
            <ul>
              {validation.findings.map((finding) => (
                <li key={finding}>{finding}</li>
              ))}
            </ul>
          )}
        </Card>
      )}
    </section>
  );
}
