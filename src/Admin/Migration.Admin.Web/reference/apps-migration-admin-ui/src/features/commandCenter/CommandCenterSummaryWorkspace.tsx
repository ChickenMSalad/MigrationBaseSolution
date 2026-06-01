import { useEffect, useState } from 'react';
import { fetchCommandCenterSummary } from './commandCenterApi';
import type { CommandCenterSummary } from './commandCenterTypes';

export function CommandCenterSummaryWorkspace() {
  const [summary, setSummary] = useState<CommandCenterSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadSummary() {
      try {
        const response = await fetchCommandCenterSummary();

        if (!cancelled) {
          setSummary(response);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load command center summary.');
        }
      }
    }

    void loadSummary();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="workspace-card command-center-summary">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Command center</p>
          <h2>Operational command center</h2>
        </div>
        <span className="status-pill">{summary?.runtimeStatus ?? 'loading'}</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Active runs</span>
          <strong>{summary?.activeRuns ?? 0}</strong>
        </article>
        <article>
          <span>Queue depth</span>
          <strong>{summary?.queueDepth ?? 0}</strong>
        </article>
        <article>
          <span>Active workers</span>
          <strong>{summary?.activeWorkers ?? 0}</strong>
        </article>
        <article>
          <span>Critical alerts</span>
          <strong>{summary?.criticalAlerts ?? 0}</strong>
        </article>
        <article>
          <span>SLA/SLO breaches</span>
          <strong>{summary?.slaSloBreaches ?? 0}</strong>
        </article>
        <article>
          <span>Hours remaining</span>
          <strong>{summary?.estimatedHoursRemaining ?? 0}</strong>
        </article>
        <article>
          <span>Monthly cost</span>
          <strong>${summary?.estimatedMonthlyCost ?? 0}</strong>
        </article>
        <article>
          <span>Updated</span>
          <strong>{summary?.lastUpdatedUtc ? new Date(summary.lastUpdatedUtc).toLocaleTimeString() : '—'}</strong>
        </article>
      </div>
    </section>
  );
}
