import { apiGet, apiPost } from './core/adminApiClient';

export type QueueProviderDescriptor = {
  providerKind: string;
  supportsDeadLettering: boolean;
  supportsSessions: boolean;
  supportsScheduledMessages: boolean;
  recommendedProperties: string[];
  warnings: string[];
};

export type QueueMessageEnvelope = {
  messageId: string;
  messageType: string;
  workspaceId: string;
  tenantId?: string | null;
  projectId?: string | null;
  runId?: string | null;
  idempotencyKey: string;
  createdUtc: string;
  properties: Record<string, string>;
};

export async function getQueueProviderDescriptor(): Promise<QueueProviderDescriptor> {
  return apiGet<QueueProviderDescriptor>('/api/cloud/queue/provider');
}

export async function probeQueueEnvelope(): Promise<QueueMessageEnvelope> {
  return apiPost<unknown, QueueMessageEnvelope>('/api/cloud/queue/envelope/probe', {});
}
