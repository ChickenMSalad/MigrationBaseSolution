import { useEffect, useState } from 'react';
import {
  fetchSlaSloBreachPreview,
  fetchSlaSloPolicies,
  fetchSlaSloSummary,
} from './slaSloApi';
import type {
  SlaSloBreachPreviewItem,
  SlaSloPolicy,
  SlaSloSummary,
} from './slaSloTypes';

export function SlaSloPolicyWorkspace() {
  const [summary, setSummary] = useState<SlaSloSummary | null>(null);
  const [policies, setPolicies] = useState<SlaSloPolicy[]>([]);
  const [breaches, setBreaches] = useState<SlaSloBreachPreviewItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadSlaSlo() {
      try {
        const [summaryResponse, policyResponse, breachResponse] = await Promise.all([
          fetchSlaSloSummary(),
          fetchSlaSloPolicies(),
          fetchSlaSloBreachPreview(),
        ]);

        if (!cancelled) {
          setSummary(summaryResponse);
          setPolicies(policyResponse.policies);
          setBreaches(breachResponse.breaches);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load SLA/SLO policy data.');
        }
      }
    }

    void loadSlaSlo();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Governance</p>
          <h2>SLA/SLO policies</h2>
        </div>
        <span className="status-pill">{summary?.status ?? 'loading'}</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Total policies</span>
          <strong>{summary?.totalPolicies ?? 0}</strong>
        </article>
        <article>
          <span>Active policies</span>
          <strong>{summary?.activePolicies ?? 0}</strong>
        </article>
        <article>
          <span>Warning breaches</span>
          <strong>{summary?.warningBreaches ?? 0}</strong>
        </article>
        <article>
          <span>Critical breaches</span>
          <strong>{summary?.criticalBreaches ?? 0}</strong>
        </article>
      </div>

      <div className="table-shell">
        <h3>Policy catalog</h3>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Metric</th>
              <th>Threshold</th>
              <th>Severity</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {policies.length === 0 ? (
              <tr>
                <td colSpan={5}>No SLA/SLO policies are configured yet.</td>
              </tr>
            ) : (
              policies.map((policy) => (
                <tr key={policy.policyId}>
                  <td>{policy.name}</td>
                  <td>{policy.metric}</td>
                  <td>{policy.threshold}</td>
                  <td>{policy.severity}</td>
                  <td>{policy.enabled ? 'Enabled' : 'Disabled'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="table-shell">
        <h3>Breach preview</h3>
        <table>
          <thead>
            <tr>
              <th>Detected</th>
              <th>Severity</th>
              <th>Metric</th>
              <th>Observed</th>
              <th>Scope</th>
            </tr>
          </thead>
          <tbody>
            {breaches.length === 0 ? (
              <tr>
                <td colSpan={5}>No current SLA/SLO breaches are available.</td>
              </tr>
            ) : (
              breaches.map((breach) => (
                <tr key={breach.breachId}>
                  <td>{new Date(breach.detectedUtc).toLocaleString()}</td>
                  <td>{breach.severity}</td>
                  <td>{breach.metric}</td>
                  <td>{breach.observedValue}</td>
                  <td>{breach.scope}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
