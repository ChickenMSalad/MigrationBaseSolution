import { useEffect, useState } from 'react';
import { notificationRoutingApi } from '../api/notificationRoutingApi';
import type {
  AlertPreviewItem,
  NotificationRoute,
  NotificationSummary,
} from '../types/notificationRouting';

type LoadState = 'loading' | 'loaded' | 'failed';

export function NotificationRouting() {
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [summary, setSummary] = useState<NotificationSummary | null>(null);
  const [routes, setRoutes] = useState<NotificationRoute[]>([]);
  const [alerts, setAlerts] = useState<AlertPreviewItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadNotificationRouting() {
      try {
        setLoadState('loading');
        const [summaryResponse, routesResponse, alertResponse] = await Promise.all([
          notificationRoutingApi.getSummary(),
          notificationRoutingApi.getRoutes(),
          notificationRoutingApi.getAlertPreview(),
        ]);

        if (!cancelled) {
          setSummary(summaryResponse);
          setRoutes(routesResponse.routes ?? []);
          setAlerts(alertResponse.alerts ?? []);
          setError(null);
          setLoadState('loaded');
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load notification routing data.');
          setLoadState('failed');
        }
      }
    }

    void loadNotificationRouting();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="page-stack">
      <header className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Notification Routing</h1>
          <p>
            Review configured notification routes, alert previews, and routing readiness for the operational runtime.
          </p>
        </div>
        <span className="status-pill">{summary?.status ?? loadState}</span>
      </header>

      {error ? <div className="error-banner">{error}</div> : null}

      <div className="metric-grid">
        <article className="metric-card">
          <span>Total routes</span>
          <strong>{summary?.totalRoutes ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Enabled routes</span>
          <strong>{summary?.enabledRoutes ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Pending alerts</span>
          <strong>{summary?.pendingAlerts ?? 0}</strong>
        </article>
        <article className="metric-card">
          <span>Critical alerts</span>
          <strong>{summary?.criticalAlerts ?? 0}</strong>
        </article>
      </div>

      <article className="panel-card">
        <h2>Routes</h2>
        {routes.length === 0 ? (
          <p>No notification routes are configured yet.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Severity</th>
                  <th>Channel</th>
                  <th>Destination</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {routes.map((route) => (
                  <tr key={route.routeId}>
                    <td>{route.name}</td>
                    <td>{route.severity}</td>
                    <td>{route.channel}</td>
                    <td>{route.destinationReference}</td>
                    <td>{route.enabled ? 'Enabled' : 'Disabled'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>

      <article className="panel-card">
        <h2>Alert preview</h2>
        {alerts.length === 0 ? (
          <p>No active alert previews are available.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Created</th>
                  <th>Severity</th>
                  <th>Category</th>
                  <th>Title</th>
                  <th>Source</th>
                </tr>
              </thead>
              <tbody>
                {alerts.map((alert) => (
                  <tr key={alert.alertId}>
                    <td>{new Date(alert.createdUtc).toLocaleString()}</td>
                    <td>{alert.severity}</td>
                    <td>{alert.category}</td>
                    <td>{alert.title}</td>
                    <td>{alert.source}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>
    </section>
  );
}
