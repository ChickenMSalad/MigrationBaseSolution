import { ExecutionProfileWorkspace } from './features/executionProfiles/ExecutionProfileWorkspace';
import { CredentialVaultWorkspace } from './features/credentials/CredentialVaultWorkspace';
import { WorkerTelemetryWorkspace } from './features/workers/WorkerTelemetryWorkspace';
import { useCallback, useMemo, useState } from 'react';

import { FailureRetryWorkspace } from './components/FailureRetryWorkspace';
import { RunLaunchPanel } from './components/RunLaunchPanel';
import { OperationalRuntimeDashboard } from './components/OperationalRuntimeDashboard';
import { ManifestImportPanel } from './components/ManifestImportPanel';
import { EndpointCard } from './components/EndpointCard';
import { EndpointProbe, adminApiBaseUrl, getJson } from './lib/adminApi';
import { ConnectorConfigurationWorkspace } from './features/connectors/ConnectorConfigurationWorkspace';


import './styles.css';
import { AuditTrailWorkspace } from './features/audit/AuditTrailWorkspace';
import { NotificationRoutingWorkspace } from './features/notifications/NotificationRoutingWorkspace';
import { CapacityForecastWorkspace } from './features/capacity/CapacityForecastWorkspace';
import { SlaSloPolicyWorkspace } from './features/slaSlo/SlaSloPolicyWorkspace';

const endpointCatalog: Array<Pick<EndpointProbe, 'label' | 'path'>> = [
  { label: 'System endpoints', path: '/api/system/endpoints' },
  { label: 'SQL backbone health', path: '/api/operational/sql-backbone/health' },
  { label: 'SQL backbone summary', path: '/api/operational/sql-backbone/summary' },
  { label: 'Queue readiness', path: '/api/operational/runtime-readiness/queue' },
  { label: 'Run readiness', path: '/api/operational/runtime-readiness/runs' },
  { label: 'Worker readiness', path: '/api/operational/runtime-readiness/workers' }
];

function createInitialProbes(): EndpointProbe[] {
  return endpointCatalog.map((probe) => ({ ...probe, status: { state: 'idle' } }));
}

export default function App() {
  const [probes, setProbes] = useState<EndpointProbe[]>(createInitialProbes);
  const [isChecking, setIsChecking] = useState(false);

  const summary = useMemo(() => {
    const success = probes.filter((probe) => probe.status.state === 'success').length;
    const error = probes.filter((probe) => probe.status.state === 'error').length;
    return { success, error, total: probes.length };
  }, [probes]);

  const runChecks = useCallback(async () => {
    setIsChecking(true);
    setProbes((current) => current.map((probe) => ({ ...probe, status: { state: 'loading' } })));

    const results = await Promise.all(
      endpointCatalog.map(async (probe): Promise<EndpointProbe> => {
        try {
          const value = await getJson<unknown>(probe.path);
          return { ...probe, status: { state: 'success', value } };
        } catch (error) {
          return {
            ...probe,
            status: {
              state: 'error',
              error: error instanceof Error ? error.message : 'Unknown request failure'
            }
          };
        }
      })
    );

    setProbes(results);
    setIsChecking(false);
  }, []);

  return (
    <main className="app-shell">
      <section className="hero">
        <div>
          <p className="eyebrow">MigrationBaseSolution</p>
          <h1>Operational Control Plane</h1>
          <p>
            First operator UI shell for SQL-backed runtime readiness, endpoint discovery,
            queue health, and run coordination visibility.
          </p>
        </div>
        <button type="button" onClick={runChecks} disabled={isChecking}>
          {isChecking ? 'Checkingâ€¦' : 'Run API checks'}
        </button>
      </section>

      <section className="status-strip" aria-label="Runtime status summary">
        <div>
          <span>{summary.success}</span>
          <p>healthy</p>
        </div>
        <div>
          <span>{summary.error}</span>
          <p>attention</p>
        </div>
        <div>
          <span>{summary.total}</span>
          <p>tracked endpoints</p>
        </div>
      </section>

      <section className="config-panel">
        <h2>Admin API base URL</h2>
        <code>{adminApiBaseUrl}</code>
        <p>Set <strong>VITE_ADMIN_API_BASE_URL</strong> in .env.local for local runs.</p>
      </section>

      <section className="endpoint-grid">
        {probes.map((probe) => (
          <EndpointCard key={probe.path} probe={probe} />
        ))}
      </section>
            <OperationalRuntimeDashboard />
      <ManifestImportPanel />
      <RunLaunchPanel />
      <FailureRetryWorkspace />
  <WorkerTelemetryWorkspace />
        <ConnectorConfigurationWorkspace />
  <CredentialVaultWorkspace />
  <ExecutionProfileWorkspace />
      <AuditTrailWorkspace />
          <NotificationRoutingWorkspace />
          <CapacityForecastWorkspace />
      <SlaSloPolicyWorkspace />
    </main>
  );
}




















