import { apiGet } from './core/adminApiClient';

export type QueueWorkerLoopDescriptor = {
  enabled: boolean;
  dryRun: boolean;
  maxMessages: number;
  pollIntervalSeconds: number;
  visibilityTimeoutSeconds: number;
  receiveProviderKind: string;
  logicalQueueName: string;
  receiveProviderConfigured: boolean;
  warnings: string[];
};

export type QueueWorkerLoopSafety = {
  canRun: boolean;
  safeToStart: boolean;
  requiresExplicitEnablement: boolean;
  requiresProviderConfiguration: boolean;
  willExecuteRuns: boolean;
  willCompleteMessages: boolean;
  descriptor: QueueWorkerLoopDescriptor;
};

export async function getQueueWorkerLoopPlan(): Promise<QueueWorkerLoopDescriptor> {
  return apiGet<QueueWorkerLoopDescriptor>('/api/cloud/queue/worker-loop');
}

export async function getQueueWorkerLoopSafety(): Promise<QueueWorkerLoopSafety> {
  return apiGet<QueueWorkerLoopSafety>('/api/cloud/queue/worker-loop/safety');
}
