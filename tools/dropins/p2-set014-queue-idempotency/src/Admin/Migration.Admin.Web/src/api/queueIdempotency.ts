import { apiGet, apiPost } from './core/adminApiClient';
import type { QueueMessageEnvelope } from './queueContracts';

export type QueueIdempotencyPlan = {
  workspaceId: string;
  projectId: string;
  runId: string;
  messageType: string;
  idempotencyKey: string;
  hashedIdempotencyKey: string;
  leaseResource: string;
};

export type QueueEnvelopeSerializationProbe = {
  envelope: QueueMessageEnvelope;
  json: string;
  base64: string;
  roundTrip: QueueMessageEnvelope;
  roundTripMatches: boolean;
};

export async function getQueueIdempotencyPlan(
  projectId = 'sample-project',
  runId = 'sample-run',
  messageType = 'migration.run.execute'
): Promise<QueueIdempotencyPlan> {
  const params = new URLSearchParams({
    projectId,
    runId,
    messageType
  });

  return apiGet<QueueIdempotencyPlan>(`/api/cloud/queue/idempotency?${params}`);
}

export async function serializeQueueEnvelopeProbe(): Promise<QueueEnvelopeSerializationProbe> {
  return apiPost<QueueEnvelopeSerializationProbe>('/api/cloud/queue/envelope/serialize', {});
}
