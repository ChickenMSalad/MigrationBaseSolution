import { Navigate, Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { Artifacts } from './features/platform/artifacts/pages/Artifacts';
import { Connectors } from './features/connectors/catalog/pages/Connectors';
import { Credentials } from './features/security/credentials/pages/Credentials';
import { CredentialVault } from "./features/security/credentialVault/pages/CredentialVault";
import { Dashboard } from './features/platform/dashboard/pages/Dashboard';
import { RuntimeDashboard } from "./features/operations/runtimeDashboard/pages/RuntimeDashboard";
import { RuntimeRunDetail } from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail";
import { ExecutionSessions } from "./features/operations/executionSessions/pages/ExecutionSessions";
import { FailureRetry } from "./features/operations/failureRetry/pages/FailureRetry";
import { ManifestBuilder } from './features/platform/builders/manifest/pages/ManifestBuilder';
import { MappingBuilder } from './features/platform/builders/mapping/pages/MappingBuilder';
import { Preflight } from './features/operations/preflight/pages/Preflight';
import { ProjectDetail } from './features/platform/projects/pages/ProjectDetail';
import { Projects } from './features/platform/projects/pages/Projects';
import { RunDetail } from './features/operations/runs/pages/RunDetail';
import { Runs } from './features/operations/runs/pages/Runs';
import { TaxonomyBuilder } from './features/platform/builders/taxonomy/pages/TaxonomyBuilder';
import { ExecutionWorkerTelemetry } from "./features/operations/executionWorkerTelemetry/pages/ExecutionWorkerTelemetry";
import { NotificationRouting } from "./features/governance/notificationRouting/pages/NotificationRouting";
import { AuditTrail } from "./features/governance/auditTrail/pages/AuditTrail";
import { CommandCenter } from "./features/operations/commandCenter/pages/CommandCenter";
import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";
import { OperationalEvents } from "./features/operations/operationalEvents/pages/OperationalEvents";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="projects" element={<Projects />} />
        <Route path="projects/:projectId" element={<ProjectDetail />} />
        <Route path="projects/:projectId/preflight" element={<Preflight />} />
        <Route path="runs" element={<Runs />} />
        <Route path="runs/:runId" element={<RunDetail />} />
        <Route path="connectors" element={<Connectors />} />
        <Route path="/connector-configuration" element={<ConnectorConfiguration />} />
        <Route path="credentials" element={<Credentials />} />
        <Route path="/credential-vault" element={<CredentialVault />} />
        <Route path="artifacts" element={<Artifacts />} />
        <Route path="manifest-builder" element={<ManifestBuilder />} />
        <Route path="taxonomy-builder" element={<TaxonomyBuilder />} />
        <Route path="mapping-builder" element={<MappingBuilder />} />
        <Route path="/execution-sessions" element={<ExecutionSessions />} />
        <Route path="/failure-retry" element={<FailureRetry />} />
        <Route path="runtime-dashboard" element={<RuntimeDashboard />} />
  <Route path="runtime-runs/:runId" element={<RuntimeRunDetail />} />
  <Route path="execution-sessions" element={<ExecutionSessions />} />
  <Route path="failure-retry" element={<FailureRetry />} />
  <Route path="credential-vault" element={<CredentialVault />} />
  <Route path="execution-worker-telemetry" element={<ExecutionWorkerTelemetry />} />
  <Route path="notification-routing" element={<NotificationRouting />} />
  <Route path="audit-trail" element={<AuditTrail />} />
  <Route path="command-center" element={<CommandCenter />} />
          <Route path="/operations/operational-events" element={<OperationalEvents />} />
  <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
      <Route path="/runtime-dashboard" element={<RuntimeDashboard />} />
        <Route path="/runtime-dashboard/:runId" element={<RuntimeRunDetail />} />
              <Route path="/manifest-builder" element={<ManifestBuilder />} />
              <Route path="/taxonomy-builder" element={<TaxonomyBuilder />} />
              <Route path="/mapping-builder" element={<MappingBuilder />} />
      </Routes>
  ); };


