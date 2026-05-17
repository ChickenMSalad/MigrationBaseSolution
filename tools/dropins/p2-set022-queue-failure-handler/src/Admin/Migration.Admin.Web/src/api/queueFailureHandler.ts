import { apiPost } from './core/adminApiClient';
import type { QueuePoisonHandlingPlan } from './queuePoisonHandling';
import type { QueueFailureArtifactRequest } from './queueFailureArtifacts';

export type QueueFailureHandlingResult = {
  failureArtifactWritten: boolean;
  strategy: string;
  artifactObjectKey?: string | null;
  recommendedNextAction: string;
  warnings: string[];
};

export type QueueFailureHandlerProbe = {
  request: QueueFailureArtifactRequest;
  plan: QueuePoisonHandlingPlan;
  result: QueueFailureHandlingResult;
};

export async function probeQueueFailureHandler(): Promise<QueueFailureHandlerProbe> {
  return apiPost<QueueFailureHandlerProbe>('/api/cloud/queue/failure-handler/probe', {});
}
