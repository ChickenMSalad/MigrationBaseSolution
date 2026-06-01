import { useEffect, useState } from 'react';
import { fetchAuditTrailSummary, fetchRecentAuditTrailEvents } from './auditTrailApi';
import type { AuditTrailEvent, AuditTrailSummary } from './auditTrailTypes';

export function AuditTrailWorkspace() {
  const [summary, setSummary] = useState<AuditTrailSummary | null>(null);
  const [events, setEvents] = useState<AuditTrailEvent[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadAuditTrail() {
      try {
        const [summaryResponse, recentResponse] = await Promise.all([
          fetchAuditTrailSummary(),
          fetchRecentAuditTrailEvents(50),
        ]);

        if (!cancelled) {
          setSummary(summaryResponse);
          setEvents(recentResponse.events);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load audit trail.');
        }
      }
    }

    void loadAuditTrail();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Governance</p>
          <h2>Operational audit trail</h2>
        </div>
        <span className="status-pill">{summary?.status ?? 'loading'}</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Total events</span>
          <strong>{summary?.totalEvents ?? 0}</strong>
        </article>
        <article>
          <span>Security</span>
          <strong>{summary?.securityEvents ?? 0}</strong>
        </article>
        <article>
          <span>Runtime</span>
          <strong>{summary?.runtimeEvents ?? 0}</strong>
        </article>
        <article>
          <span>Configuration</span>
          <strong>{summary?.configurationEvents ?? 0}</strong>
        </article>
      </div>

      <div className="table-shell">
        <table>
          <thead>
            <tr>
              <th>Occurred</th>
              <th>Category</th>
              <th>Action</th>
              <th>Actor</th>
              <th>Resource</th>
              <th>Outcome</th>
            </tr>
          </thead>
          <tbody>
            {events.length === 0 ? (
              <tr>
                <td colSpan={6}>No audit events are available yet.</td>
              </tr>
            ) : (
              events.map((event) => (
                <tr key={event.eventId}>
                  <td>{new Date(event.occurredUtc).toLocaleString()}</td>
                  <td>{event.category}</td>
                  <td>{event.action}</td>
                  <td>{event.actor}</td>
                  <td>{event.resourceType}:{event.resourceId}</td>
                  <td>{event.outcome}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
