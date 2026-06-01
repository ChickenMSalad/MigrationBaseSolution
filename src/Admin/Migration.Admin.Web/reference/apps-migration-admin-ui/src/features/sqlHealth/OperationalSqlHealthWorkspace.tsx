import { useEffect, useState } from 'react';
import { fetchOperationalSqlHealth } from './sqlHealthApi';
import type { OperationalSqlHealth } from './sqlHealthTypes';

export function OperationalSqlHealthWorkspace() {
  const [health, setHealth] = useState<OperationalSqlHealth | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadHealth() {
      try {
        const response = await fetchOperationalSqlHealth();

        if (!cancelled) {
          setHealth(response);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load SQL health.');
        }
      }
    }

    void loadHealth();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Runtime health</p>
          <h2>Operational SQL health</h2>
        </div>
        <span className="status-pill">{health?.status ?? 'loading'}</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <p>{health?.message ?? 'Checking operational SQL health...'}</p>

      <div className="metric-grid">
        <article>
          <span>Database</span>
          <strong>{health?.databaseName ?? '—'}</strong>
        </article>
        <article>
          <span>Verified tables</span>
          <strong>{health?.verifiedTables.length ?? 0}</strong>
        </article>
        <article>
          <span>Missing tables</span>
          <strong>{health?.missingTables.length ?? 0}</strong>
        </article>
      </div>
    </section>
  );
}
