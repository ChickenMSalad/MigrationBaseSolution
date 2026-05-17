import { apiGet } from './core/adminApiClient';
import type { QueueDispatchProviderDescriptor } from './queueDispatch';
import type { QueueReceiveProviderDescriptor } from './queueReceive';
import type { QueueWorkerLoopDescriptor } from './queueWorkerLoop';
import type { QueuePoisonHandlingPlan } from './queuePoisonHandling';
import type { QueueExecutionObservabilitySnapshot } from './queueExecutionObservability';

export type QueueExecutionReadinessSnapshot = {
  generatedUtc: string;
  isReadyForDryRun: boolean;
  isReadyForLiveExecution: boolean;
  dispatchProvider: QueueDispatchProviderDescriptor;
  receiveProvider: QueueReceiveProviderDescriptor;
  workerLoop: QueueWorkerLoopDescriptor;
  poisonHandling: QueuePoisonHandlingPlan;
  observability: QueueExecutionObservabilitySnapshot;
  blockingIssues: string[];
  warnings: string[];
};

export async function getQueueExecutionReadiness(): Promise<QueueExecutionReadinessSnapshot> {
  return apiGet<QueueExecutionReadinessSnapshot>('/api/cloud/queue/execution-readiness');
}
