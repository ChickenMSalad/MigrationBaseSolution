import { apiGet } from './core/adminApiClient';
import type { AuditPersistenceProviderDescriptor } from './auditPersistence';
import type { TelemetryProviderDescriptor } from './telemetrySink';
import type { QueueExecutionReadinessSnapshot } from './queueExecutionReadiness';

export type OperationalReadinessSnapshot = {
  generatedUtc: string;
  isOperationallyReady: boolean;
  isReadyForLiveQueueExecution: boolean;
  audit: AuditPersistenceProviderDescriptor;
  telemetry: TelemetryProviderDescriptor;
  queueExecution: QueueExecutionReadinessSnapshot;
  blockingIssues: string[];
  warnings: string[];
};

export async function getOperationalReadiness(): Promise<OperationalReadinessSnapshot> {
  return apiGet<OperationalReadinessSnapshot>('/api/cloud/operations/readiness');
}
