import { apiGet } from './core/adminApiClient';

export type QueuePoisonHandlingPlan = {
  providerKind: string;
  logicalQueueName: string;
  maxAttempts: number;
  poisonStrategy: string;
  deadLetterQueueName?: string | null;
  nativeDeadLetterSupported: boolean;
  persistFailureArtifact: boolean;
  failureArtifactKind: string;
  warnings: string[];
};

export type QueuePoisonHandlingRecommendation = {
  recommendation: string;
  plan: QueuePoisonHandlingPlan;
};

export async function getQueuePoisonHandlingPlan(): Promise<QueuePoisonHandlingPlan> {
  return apiGet<QueuePoisonHandlingPlan>('/api/cloud/queue/poison-handling');
}

export async function getQueuePoisonHandlingRecommendation(): Promise<QueuePoisonHandlingRecommendation> {
  return apiGet<QueuePoisonHandlingRecommendation>('/api/cloud/queue/poison-handling/recommendation');
}
