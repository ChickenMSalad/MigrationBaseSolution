import { useEffect, useState } from 'react';
import {
  fetchAlertPreview,
  fetchNotificationRoutes,
  fetchNotificationSummary,
} from './notificationApi';
import type {
  AlertPreviewItem,
  NotificationRoute,
  NotificationSummary,
} from './notificationTypes';

export function NotificationRoutingWorkspace() {
  const [summary, setSummary] = useState<NotificationSummary | null>(null);
  const [routes, setRoutes] = useState<NotificationRoute[]>([]);
  const [alerts, setAlerts] = useState<AlertPreviewItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadNotifications() {
      try {
        const [summaryResponse, routesResponse, alertResponse] = await Promise.all([
          fetchNotificationSummary(),
          fetchNotificationRoutes(),
          fetchAlertPreview(),
        ]);

        if (!cancelled) {
          setSummary(summaryResponse);
          setRoutes(routesResponse.routes);
          setAlerts(alertResponse.alerts);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load notification routing data.');
        }
      }
    }

    void loadNotifications();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="workspace-card">
      <div className="workspace-card__header">
        <div>
          <p className="eyebrow">Operations</p>
          <h2>Notifications and alert routing</h2>
        </div>
        <span className="status-pill">{summary?.status ?? 'loading'}</span>
      </div>

      {error ? <p className="error-text">{error}</p> : null}

      <div className="metric-grid">
        <article>
          <span>Total routes</span>
          <strong>{summary?.totalRoutes ?? 0}</strong>
        </article>
        <article>
          <span>Enabled routes</span>
          <strong>{summary?.enabledRoutes ?? 0}</strong>
        </article>
        <article>
          <span>Pending alerts</span>
          <strong>{summary?.pendingAlerts ?? 0}</strong>
        </article>
        <article>
          <span>Critical alerts</span>
          <strong>{summary?.criticalAlerts ?? 0}</strong>
        </article>
      </div>

      <div className="table-shell">
        <h3>Routes</h3>
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
            {routes.length === 0 ? (
              <tr>
                <td colSpan={5}>No notification routes are configured yet.</td>
              </tr>
            ) : (
              routes.map((route) => (
                <tr key={route.routeId}>
                  <td>{route.name}</td>
                  <td>{route.severity}</td>
                  <td>{route.channel}</td>
                  <td>{route.destinationReference}</td>
                  <td>{route.enabled ? 'Enabled' : 'Disabled'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="table-shell">
        <h3>Alert preview</h3>
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
            {alerts.length === 0 ? (
              <tr>
                <td colSpan={5}>No active alert previews are available.</td>
              </tr>
            ) : (
              alerts.map((alert) => (
                <tr key={alert.alertId}>
                  <td>{new Date(alert.createdUtc).toLocaleString()}</td>
                  <td>{alert.severity}</td>
                  <td>{alert.category}</td>
                  <td>{alert.title}</td>
                  <td>{alert.source}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
