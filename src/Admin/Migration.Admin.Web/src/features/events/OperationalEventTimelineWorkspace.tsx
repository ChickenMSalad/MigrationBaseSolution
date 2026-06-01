import { useEffect, useState } from 'react';
import { buildOperationalEventCsvExportUrl } from './operationalEventExportApi';
import {
  fetchOperationalEventAggregateSummary,
  queryOperationalEvents,
} from './operationalEventTimelineApi';
import type {
  OperationalEventAggregateSummary,
  OperationalEventRecord,
} from './operationalEventTimelineTypes';

const pageSize = 50;

function toUtcInputValue(value: string): string | undefined {
  if (!value) {
    return undefined;
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
}

export function OperationalEventTimelineWorkspace() {
  const [events, setEvents] = useState<OperationalEventRecord[]>([]);
  const [summary, setSummary] = useState<OperationalEventAggregateSummary | null>(null);
  const [severity, setSeverity] = useState('');
  const [category, setCategory] = useState('');
  const [eventType, setEventType] = useState('');
  const [fromUtc, setFromUtc] = useState('');
  const [toUtc, setToUtc] = useState('');
  const [skip, setSkip] = useState(0);
  const [returned, setReturned] = useState(0);
  const [error, setError] = useState<string | null>(null);

  async function loadEvents(nextSkip = skip) {
    const fromUtcValue = toUtcInputValue(fromUtc);
    const toUtcValue = toUtcInputValue(toUtc);

    try {
      const [eventResponse, summaryResponse] = await Promise.all([
        queryOperationalEvents({
          severity: severity || undefined,
          category: category || undefined,
          eventType: eventType || undefined,
          fromUtc: fromUtcValue,
          toUtc: toUtcValue,
          skip: nextSkip,
          take: pageSize,
        }),
        fetchOperationalEventAggregateSummary({
          fromUtc: fromUtcValue,
          toUtc: toUtcValue,
        }),
      ]);

      setEvents(eventResponse.events);
      setReturned(eventResponse.returned);
      setSkip(eventResponse.skip);
      setSummary(summaryResponse);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load operational events.');
    }
  }

  useEffect(() => {
    void loadEvents(0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function applyFilters() {
    void loadEvents(0);
  }

  function previousPage() {
    void loadEvents(Math.max(0, skip - pageSize));
  }

  function nextPage() {
    void loadEvents(skip + pageSize);
  }

  function exportCsv() {
    const url = buildOperationalEventCsvExportUrl({
      severity: severity || undefined,
      category: category || undefined,
      eventType: eventType || undefined,
      fromUtc: toUtcInputValue(fromUtc),
      toUtc: toUtcInputValue(toUtc),
      take: 250,
    });

    window.open(url, '_blank', 'noopener,noreferrer');
  }

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">History</p>
          <h2>Operational event timeline</h2>
        </div>
        <span className="status-pill">{returned} returned</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Total events</span>
          <strong>{summary?.totalEvents ?? 0}</strong>
        </article>
        <article>
          <span>Severity buckets</span>
          <strong>{summary?.bySeverity.length ?? 0}</strong>
        </article>
        <article>
          <span>Category buckets</span>
          <strong>{summary?.byCategory.length ?? 0}</strong>
        </article>
        <article>
          <span>Event type buckets</span>
          <strong>{summary?.byEventType.length ?? 0}</strong>
        </article>
      </div>

      <div className="filter-row">
        <label>
          Severity
          <input value={severity} onChange={(event) => setSeverity(event.target.value)} placeholder="critical" />
        </label>
        <label>
          Category
          <input value={category} onChange={(event) => setCategory(event.target.value)} placeholder="runtime" />
        </label>
        <label>
          Event type
          <input value={eventType} onChange={(event) => setEventType(event.target.value)} placeholder="OperationalMetricsSnapshot" />
        </label>
        <label>
          From
          <input type="datetime-local" value={fromUtc} onChange={(event) => setFromUtc(event.target.value)} />
        </label>
        <label>
          To
          <input type="datetime-local" value={toUtc} onChange={(event) => setToUtc(event.target.value)} />
        </label>
        <button type="button" onClick={applyFilters}>Apply</button>
        <button type="button" onClick={exportCsv}>Export CSV</button>
      </div>

      <div className="table-shell">
        <table>
          <thead>
            <tr>
              <th>Created</th>
              <th>Severity</th>
              <th>Category</th>
              <th>Event type</th>
              <th>Source</th>
              <th>Message</th>
            </tr>
          </thead>
          <tbody>
            {events.length === 0 ? (
              <tr>
                <td colSpan={6}>No operational events matched the current filters.</td>
              </tr>
            ) : (
              events.map((event) => (
                <tr key={event.operationalEventId}>
                  <td>{new Date(event.createdUtc).toLocaleString()}</td>
                  <td>{event.severity}</td>
                  <td>{event.category}</td>
                  <td>{event.eventType}</td>
                  <td>{event.source}</td>
                  <td>{event.message}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="button-row">
        <button type="button" onClick={previousPage} disabled={skip === 0}>Previous</button>
        <span>Offset {skip}</span>
        <button type="button" onClick={nextPage} disabled={returned < pageSize}>Next</button>
      </div>
    </section>
  );
}
