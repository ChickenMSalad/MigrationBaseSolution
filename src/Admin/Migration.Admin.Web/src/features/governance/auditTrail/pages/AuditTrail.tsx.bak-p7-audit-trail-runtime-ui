import { useEffect, useState } from 'react';
import { getAuditTrailSummary, getRecentAuditTrailEvents } from '../api/auditTrailApi';
import type { AuditTrailEvent, AuditTrailSummary } from '../types/auditTrail';

function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
}

export function AuditTrail() {
  const [summary, setSummary] = useState<AuditTrailSummary | null>(null);
  const [events, setEvents] = useState<AuditTrailEvent[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadAuditTrail(): Promise<void> {
      setIsLoading(true);
      try {
        const [summaryResponse, recentResponse] = await Promise.all([
          getAuditTrailSummary(),
          getRecentAuditTrailEvents(50),
        ]);

        if (!cancelled) {
          setSummary(summaryResponse);
          setEvents(recentResponse.events ?? []);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load audit trail.');
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadAuditTrail();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main className="page-shell">
      <section className="page-header">
        <div>
          <p className="eyebrow">Governance</p>
          <h1>Operational audit trail</h1>
          <p className="page-description">
            Review recent security, runtime, and configuration activity reported by the Admin API.
          </p>
        </div>
        <span className="status-pill">{summary?.status ?? (isLoading ? 'loading' : 'unknown')}</span>
      </section>

      {error ? <div className="error-banner">{error}</div> : null}

      <section className="metric-grid">
        <article className="metric-card">
          <span>Total events</span>
          <strong>{summary?.totalEvents ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Security</span>
          <strong>{summary?.securityEvents ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Runtime</span>
          <strong>{summary?.runtimeEvents ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Configuration</span>
          <strong>{summary?.configurationEvents ?? 0}</strong>
        </article>
      </section>

      <section className="content-card">
        <div className="section-heading">
          <h2>Recent audit events</h2>
          <span>Last event: {formatDate(summary?.lastEventUtc)}</span>
        </div>

        {isLoading ? (
          <p>Loading audit trail…</p>
        ) : events.length === 0 ? (
          <p>No audit events are available yet.</p>
        ) : (
          <div className="table-scroll">
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
                {events.map((event) => (
                  <tr key={event.eventId}>
                    <td>{formatDate(event.occurredUtc)}</td>
                    <td>{event.category}</td>
                    <td>{event.action}</td>
                    <td>{event.actor}</td>
                    <td>{event.resourceType}:{event.resourceId}</td>
                    <td>{event.outcome}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
