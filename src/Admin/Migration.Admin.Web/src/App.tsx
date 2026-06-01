import { Navigate, Route, Routes } from "react-router-dom";

import { Layout } from "./components/Layout";
import { Artifacts } from "./pages/Artifacts";
import { Connectors } from "./pages/Connectors";
import { ConnectorConfiguration } from "./pages/ConnectorConfiguration";
import { Credentials } from "./pages/Credentials";
import { CredentialVault } from "./pages/CredentialVault";
import { Dashboard } from "./pages/Dashboard";
import { RuntimeDashboard } from "./features/operations/runtimeDashboard/pages/RuntimeDashboard";
import { RuntimeRunDetail } from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail";
import { ExecutionSessions } from "./features/operations/executionSessions/pages/ExecutionSessions";
import { FailureRetry } from "./features/operations/failureRetry/pages/FailureRetry";
import { ManifestBuilder } from "./pages/ManifestBuilder";
import { MappingBuilder } from "./pages/MappingBuilder";
import { Preflight } from "./pages/Preflight";
import { ProjectDetail } from "./pages/ProjectDetail";
import { Projects } from "./pages/Projects";
import { RunDetail } from "./pages/RunDetail";
import { Runs } from "./pages/Runs";
import { TaxonomyBuilder } from "./pages/TaxonomyBuilder";

import { ExecutionWorkerTelemetry } from "./pages/ExecutionWorkerTelemetry";
import { NotificationRouting } from "./pages/NotificationRouting";
import { AuditTrail } from "./pages/AuditTrail";
import { CommandCenter } from "./pages/CommandCenter";
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
  <Route path="connector-configuration" element={<ConnectorConfiguration />} />
  <Route path="execution-worker-telemetry" element={<ExecutionWorkerTelemetry />} />
  <Route path="notification-routing" element={<NotificationRouting />} />
  <Route path="audit-trail" element={<AuditTrail />} />
  <Route path="command-center" element={<CommandCenter />} />
  <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
      <Route path="/runtime-dashboard" element={<RuntimeDashboard />} />
        <Route path="/runtime-dashboard/:runId" element={<RuntimeRunDetail />} />
      </Routes>
  );
}







