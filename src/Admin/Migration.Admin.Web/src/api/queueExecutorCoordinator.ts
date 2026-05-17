import { apiGet, apiPost } from './core/adminApiClient';

export type QueueExecutorCoordinatorOptions = {
  dryRun: boolean;
  completeMessages: boolean;
  writeFailureArtifacts: boolean;
  maxMessages: number;
};

export type QueueExecutorMessageResult = {
  providerMessageId: string;
  messageType: string;
  projectId?: string | null;
  runId?: string | null;
  idempotencyKey: string;
  canExecute: boolean;
  action: string;
  completed: boolean;
  failureHandled: boolean;
  failureArtifactObjectKey?: string | null;
  warnings: string[];
};

export type QueueExecutorCoordinatorResult = {
  receivedCount: number;
  plannedCount: number;
  executableCount: number;
  completedCount: number;
  failureCount: number;
  messages: QueueExecutorMessageResult[];
  warnings: string[];
};

export type QueueExecutorCoordinatorProbe = {
  options: QueueExecutorCoordinatorOptions;
  result: QueueExecutorCoordinatorResult;
};

export async function getQueueExecutorCoordinatorOptions(): Promise<QueueExecutorCoordinatorOptions> {
  return apiGet<QueueExecutorCoordinatorOptions>('/api/cloud/queue/executor-coordinator/options');
}

export async function probeQueueExecutorCoordinator(): Promise<QueueExecutorCoordinatorProbe> {
  return apiPost<QueueExecutorCoordinatorProbe>('/api/cloud/queue/executor-coordinator/probe', {});
}
