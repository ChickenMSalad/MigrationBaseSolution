import { apiGet, apiPost } from './core/adminApiClient';
import type { QueueMessageEnvelope } from './queueContracts';

export type QueueDispatchProviderDescriptor = {
  providerKind: string;
  logicalQueueName: string;
  isConfigured: boolean;
  supportsNativeVisibilityTimeout: boolean;
  supportsNativePoisonHandling: boolean;
  supportsNativeMessageProperties: boolean;
  warnings: string[];
};

export type QueueDispatchResult = {
  accepted: boolean;
  providerKind: string;
  logicalQueueName: string;
  messageId: string;
  idempotencyKey: string;
  dispatchedUtc: string;
  providerMessageId?: string | null;
  properties?: Record<string, string> | null;
};

export type QueueDispatchProbeResponse = {
  envelope: QueueMessageEnvelope;
  result: QueueDispatchResult;
};

export async function getQueueDispatchProvider(): Promise<QueueDispatchProviderDescriptor> {
  return apiGet<QueueDispatchProviderDescriptor>('/api/cloud/queue/dispatch/provider');
}

export async function probeQueueDispatch(): Promise<QueueDispatchProbeResponse> {
  return apiPost<unknown, QueueDispatchProbeResponse>('/api/cloud/queue/dispatch/probe', {});
}
