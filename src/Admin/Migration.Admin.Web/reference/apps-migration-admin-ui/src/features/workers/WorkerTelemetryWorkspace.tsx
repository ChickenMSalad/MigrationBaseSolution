import { useEffect, useState } from 'react';
import { getWorkerLeases, getWorkerTelemetry } from './workerTelemetryApi';
import type {
  OperationalWorkerLeaseResponse,
  OperationalWorkerTelemetryResponse,
} from './workerTelemetryTypes';

export function WorkerTelemetryWorkspace() {
  const [telemetry, setTelemetry] = useState<OperationalWorkerTelemetryResponse | null>(null);
  const [leases, setLeases] = useState<OperationalWorkerLeaseResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    try {
      setError(null);
      const [workerTelemetry, workerLeases] = await Promise.all([
        getWorkerTelemetry(),
        getWorkerLeases(),
      ]);
      setTelemetry(workerTelemetry);
      setLeases(workerLeases);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load worker telemetry.');
    }
  }

  useEffect(() => {
    void refresh();
    const handle = window.setInterval(() => void refresh(), 15000);
    return () => window.clearInterval(handle);
  }, []);

  return (
    <section className="panel worker-telemetry-workspace">
      <div className="panel-header">
        <div>
          <p className="eyebrow">P4.17</p>
          <h2>Live Worker Telemetry</h2>
          <p>Monitor dispatcher and executor heartbeat, lease ownership, and SQL queue pressure.</p>
        </div>
        <button type="button" onClick={() => void refresh()}>Refresh</button>
      </div>

      {error && <div className="alert error">{error}</div>}

      {telemetry && (
        <div className="metric-grid">
          <Metric label="Ready" value={telemetry.queue.ready} />
          <Metric label="Leased" value={telemetry.queue.leased} />
          <Metric label="In flight" value={telemetry.queue.inFlight} />
          <Metric label="Failed" value={telemetry.queue.failed} />
        </div>
      )}

      <div className="table-card">
        <h3>Workers</h3>
        <table>
          <thead>
            <tr>
              <th>Worker</th>
              <th>Status</th>
              <th>Role</th>
              <th>Leases</th>
              <th>In flight</th>
              <th>Heartbeat</th>
            </tr>
          </thead>
          <tbody>
            {(telemetry?.workers ?? []).map((worker) => (
              <tr key={worker.workerId}>
                <td>{worker.workerId}</td>
                <td>{worker.status}</td>
                <td>{worker.role}</td>
                <td>{worker.activeLeases}</td>
                <td>{worker.inFlightWorkItems}</td>
                <td>{new Date(worker.lastHeartbeatUtc).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="table-card">
        <h3>Lease Ownership</h3>
        <table>
          <thead>
            <tr>
              <th>Lease</th>
              <th>Worker</th>
              <th>Status</th>
              <th>Expires</th>
              <th>Seconds left</th>
            </tr>
          </thead>
          <tbody>
            {(leases?.leases ?? []).map((lease) => (
              <tr key={lease.leaseId}>
                <td>{lease.leaseId}</td>
                <td>{lease.workerId}</td>
                <td>{lease.status}</td>
                <td>{new Date(lease.expiresUtc).toLocaleString()}</td>
                <td>{lease.secondsRemaining}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {telemetry?.warnings?.map((warning) => (
        <div className="alert" key={warning}>{warning}</div>
      ))}
    </section>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
