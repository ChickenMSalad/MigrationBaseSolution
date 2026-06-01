import { useEffect, useMemo, useState } from "react";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import { getOperationalEventTimeline } from "../api/operationalEventsApi";
import type { OperationalEventTimelineItem } from "../types/operationalEvents";

function formatDate(value: string): string {
  if (!value) {
    return "";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
}

export function OperationalEvents() {
  const [events, setEvents] = useState<OperationalEventTimelineItem[]>([]);
  const [runId, setRunId] = useState("");
  const [filter, setFilter] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadTimeline(selectedRunId?: string) {
    setLoading(true);
    setError(null);

    try {
      const response = await getOperationalEventTimeline(selectedRunId && selectedRunId.trim() ? selectedRunId.trim() : undefined);
      setEvents(response.events ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load operational events.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTimeline();
  }, []);

  const filteredEvents = useMemo(() => {
    const term = filter.trim().toLowerCase();
    if (!term) {
      return events;
    }

    return events.filter((event) => {
      return [event.eventType, event.severity, event.message, event.source, event.runId, String(event.workItemId ?? "")]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(term));
    });
  }, [events, filter]);

  return (
    <div className="page-stack">
      <section className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Operational Events</h1>
          <p className="muted">Review runtime timeline events emitted by migration execution and worker services.</p>
        </div>
      </section>

      <Card>
        <div className="toolbar">
          <label>
            Run ID
            <input value={runId} onChange={(event) => setRunId(event.target.value)} placeholder="Optional run id" />
          </label>
          <label>
            Filter
            <input value={filter} onChange={(event) => setFilter(event.target.value)} placeholder="Type, severity, message" />
          </label>
          <button type="button" onClick={() => void loadTimeline(runId)}>Refresh</button>
        </div>
      </Card>

      {loading ? <p>Loading operational events...</p> : null}
      {error ? <LoadingError message={error} /> : null}

      {!loading && !error ? (
        <Card>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Severity</th>
                  <th>Type</th>
                  <th>Run</th>
                  <th>Work Item</th>
                  <th>Message</th>
                </tr>
              </thead>
              <tbody>
                {filteredEvents.map((event) => (
                  <tr key={event.eventId}>
                    <td>{formatDate(event.createdAtUtc)}</td>
                    <td>{event.severity}</td>
                    <td>{event.eventType}</td>
                    <td>{event.runId ?? "-"}</td>
                    <td>{event.workItemId ?? "-"}</td>
                    <td>{event.message}</td>
                  </tr>
                ))}
                {filteredEvents.length === 0 ? (
                  <tr>
                    <td colSpan={6}>No operational events found.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </Card>
      ) : null}
    </div>
  );
}
