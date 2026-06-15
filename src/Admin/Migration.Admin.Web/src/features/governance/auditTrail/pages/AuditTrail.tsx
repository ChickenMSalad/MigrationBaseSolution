import { useCallback, useEffect, useMemo, useState } from 'react';
import { getAuditTrailSummary, getRecentAuditTrailEvents } from '../api/auditTrailApi';
import type { AuditTrailEvent, AuditTrailSummary } from '../types/auditTrail';

type AuditFilter = 'all' | 'runtime' | 'configuration' | 'security' | 'infrastructure';

type AuditLoadState = {
  summary: AuditTrailSummary | null;
  events: AuditTrailEvent[];
  isLoading: boolean;
  error: string | null;
  lastRefreshUtc: string | null;
};

const refreshIntervalMs = 10000;

function formatDate(value: string | null | undefined): string {
  if (!value) {
    return 'n/a';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
}

function getCategory(event: AuditTrailEvent): string {
  return (event.category || 'runtime').toLowerCase();
}

function getOutcome(event: AuditTrailEvent): string {
  return event.outcome || 'observed';
}

export function AuditTrail() {
  const [state, setState] = useState<AuditLoadState>({
    summary: null,
    events: [],
    isLoading: true,
    error: null,
    lastRefreshUtc: null,
  });
  const [filter, setFilter] = useState<AuditFilter>('all');

  const refresh = useCallback(async () => {
    setState((current) => ({ ...current, isLoading: true }));

    try {
      const [summaryResponse, recentResponse] = await Promise.all([
        getAuditTrailSummary(),
        getRecentAuditTrailEvents(100),
      ]);

      setState({
        summary: summaryResponse,
        events: recentResponse.events ?? [],
        isLoading: false,
        error: null,
        lastRefreshUtc: new Date().toISOString(),
      });
    } catch (err) {
      setState((current) => ({
        ...current,
        isLoading: false,
        error: err instanceof Error ? err.message : 'Failed to load audit trail.',
      }));
    }
  }, []);

  useEffect(() => {
    void refresh();
    const handle = window.setInterval(() => {
      void refresh();
    }, refreshIntervalMs);

    return () => window.clearInterval(handle);
  }, [refresh]);

  const filteredEvents = useMemo(() => {
    if (filter === 'all') {
      return state.events;
    }

    return state.events.filter((event) => getCategory(event) === filter);
  }, [filter, state.events]);

  const summary = state.summary;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-semibold uppercase tracking-wide text-slate-500">Governance</p>
          <h1 className="text-3xl font-semibold text-slate-900">Operational audit trail</h1>
          <p className="mt-2 max-w-3xl text-sm text-slate-600">
            Review recent SQL-backed runtime, configuration, infrastructure, and security audit signals.
          </p>
        </div>
        <div className="flex flex-col items-end gap-2 text-sm text-slate-500">
          <button
            type="button"
            onClick={() => void refresh()}
            className="rounded-md border border-slate-300 px-3 py-2 font-medium text-slate-700 hover:bg-slate-50"
            disabled={state.isLoading}
          >
            {state.isLoading ? 'Refreshing' : 'Refresh'}
          </button>
          <span>Auto refresh: 10 seconds</span>
          <span>Last refresh: {formatDate(state.lastRefreshUtc)}</span>
        </div>
      </div>

      {state.error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {state.error}
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-5">
        <MetricCard label="Status" value={summary?.status ?? (state.isLoading ? 'loading' : 'unknown')} />
        <MetricCard label="Total events" value={summary?.totalEvents ?? 0} />
        <MetricCard label="Runtime" value={summary?.runtimeEvents ?? 0} />
        <MetricCard label="Configuration" value={summary?.configurationEvents ?? 0} />
        <MetricCard label="Security" value={summary?.securityEvents ?? 0} />
      </div>

      <section className="rounded-lg border border-slate-200 bg-white shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-4 py-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Recent audit events</h2>
            <p className="text-sm text-slate-500">Last event: {formatDate(summary?.lastEventUtc)}</p>
          </div>
          <select
            value={filter}
            onChange={(event) => setFilter(event.target.value as AuditFilter)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700"
          >
            <option value="all">All categories</option>
            <option value="runtime">Runtime</option>
            <option value="configuration">Configuration</option>
            <option value="infrastructure">Infrastructure</option>
            <option value="security">Security</option>
          </select>
        </div>

        {state.isLoading && state.events.length === 0 ? (
          <div className="p-6 text-sm text-slate-500">Loading audit trail...</div>
        ) : filteredEvents.length === 0 ? (
          <div className="p-6 text-sm text-slate-500">No audit events match the selected filter.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3">Occurred</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Action</th>
                  <th className="px-4 py-3">Actor</th>
                  <th className="px-4 py-3">Resource</th>
                  <th className="px-4 py-3">Outcome</th>
                  <th className="px-4 py-3">Message</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredEvents.map((event) => (
                  <tr key={event.eventId} className="align-top">
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDate(event.occurredUtc)}</td>
                    <td className="px-4 py-3 font-medium text-slate-900">{getCategory(event)}</td>
                    <td className="px-4 py-3 text-slate-700">{event.action}</td>
                    <td className="px-4 py-3 text-slate-600">{event.actor}</td>
                    <td className="px-4 py-3 text-slate-600">
                      <div>{event.resourceType}</div>
                      <div className="text-xs text-slate-400">{event.resourceId}</div>
                    </td>
                    <td className="px-4 py-3 text-slate-700">{getOutcome(event)}</td>
                    <td className="max-w-xl px-4 py-3 text-slate-600">{event.message ?? 'n/a'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</div>
      <div className="mt-2 text-2xl font-semibold text-slate-900">{value}</div>
    </div>
  );
}
