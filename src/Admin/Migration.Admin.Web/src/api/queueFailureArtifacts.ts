import { apiGet, apiPost } from './core/adminApiClient';
import type { QueuePoisonHandlingPlan } from './queuePoisonHandling';
import type { ArtifactStorageDescriptor } from './artifactStorage';
import type { ArtifactManifestIndex } from './artifactManifestIndex';

export type QueueFailureArtifactRequest = {
  workspaceId: string;
  projectId: string;
  runId: string;
  messageType: string;
  idempotencyKey: string;
  failureReason: string;
  exceptionType: string;
  exceptionMessage: string;
  attempt: number;
  failedUtc: string;
};

export type QueueFailureArtifactDescriptor = {
  workspaceId: string;
  artifactKind: string;
  artifactId: string;
  fileName: string;
  contentType: string;
  objectKey: string;
  recommendedAction: string;
};

export type QueueFailureArtifactPlan = {
  request: QueueFailureArtifactRequest;
  poisonPlan: QueuePoisonHandlingPlan;
  descriptor: QueueFailureArtifactDescriptor;
};

export type QueueFailureArtifactProbe = {
  descriptor: QueueFailureArtifactDescriptor;
  artifact: ArtifactStorageDescriptor;
  index: ArtifactManifestIndex;
};

export async function getQueueFailureArtifactPlan(): Promise<QueueFailureArtifactPlan> {
  return apiGet<QueueFailureArtifactPlan>('/api/cloud/queue/failure-artifact/plan');
}

export async function probeQueueFailureArtifact(): Promise<QueueFailureArtifactProbe> {
  return apiPost<QueueFailureArtifactProbe>('/api/cloud/queue/failure-artifact/probe', {});
}
