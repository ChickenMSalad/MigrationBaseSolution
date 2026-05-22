import { OperationalSqlHealthWorkspace } from '../sqlHealth/OperationalSqlHealthWorkspace';
import { CommandCenterSummaryWorkspace } from '../commandCenter/CommandCenterSummaryWorkspace';
import { OperationalRuntimeDashboard } from '../../components/OperationalRuntimeDashboard';
import { ManifestImportPanel } from '../../components/ManifestImportPanel';
import { RunLaunchPanel } from '../../components/RunLaunchPanel';
import { FailureRetryWorkspace } from '../../components/FailureRetryWorkspace';
import { WorkerTelemetryWorkspace } from '../workers/WorkerTelemetryWorkspace';
import { ConnectorConfigurationWorkspace } from '../connectors/ConnectorConfigurationWorkspace';
import { CredentialVaultWorkspace } from '../credentials/CredentialVaultWorkspace';
import { ExecutionProfileWorkspace } from '../executionProfiles/ExecutionProfileWorkspace';
import { NotificationRoutingWorkspace } from '../notifications/NotificationRoutingWorkspace';
import { SlaSloPolicyWorkspace } from '../slaSlo/SlaSloPolicyWorkspace';
import { CapacityForecastWorkspace } from '../capacity/CapacityForecastWorkspace';
import { CostAnalyticsWorkspace } from '../cost/CostAnalyticsWorkspace';
import { AuditTrailWorkspace } from '../audit/AuditTrailWorkspace';

export function OperationalWorkspaceComposition() {
  return (
    <>
      <OperationalSqlHealthWorkspace />
      <CommandCenterSummaryWorkspace />
      <OperationalRuntimeDashboard />
      <ManifestImportPanel />
      <RunLaunchPanel />
      <FailureRetryWorkspace />
      <WorkerTelemetryWorkspace />
      <ConnectorConfigurationWorkspace />
      <CredentialVaultWorkspace />
      <ExecutionProfileWorkspace />
      <NotificationRoutingWorkspace />
      <SlaSloPolicyWorkspace />
      <CapacityForecastWorkspace />
      <CostAnalyticsWorkspace />
      <AuditTrailWorkspace />
    </>
  );
}




