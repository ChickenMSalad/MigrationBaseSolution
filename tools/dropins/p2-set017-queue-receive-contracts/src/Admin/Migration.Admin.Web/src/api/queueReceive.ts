import { apiGet, apiPost } from './core/adminApiClient';
import type { QueueMessageEnvelope } from './queueContracts';

export type QueueReceiveProviderDescriptor = {
  providerKind: string;
  logicalQueueName: string;
  isConfigured: boolean;
  supportsVisibilityTimeout: boolean;
  supportsNativeDequeueCount: boolean;
  supportsAbandon: boolean;
  warnings: string[];
};

export type QueueReceivedMessage = {
  providerKind: string;
  logicalQueueName: string;
  providerMessageId: string;
  popReceipt?: string | null;
  dequeueCount?: number | null;
  envelope: QueueMessageEnvelope;
  receivedUtc: string;
};

export type QueueReceiveProbeResponse = {
  provider: QueueReceiveProviderDescriptor;
  messageCount: number;
  messages: QueueReceivedMessage[];
};

export async function getQueueReceiveProvider(): Promise<QueueReceiveProviderDescriptor> {
  return apiGet<QueueReceiveProviderDescriptor>('/api/cloud/queue/receive/provider');
}

export async function probeQueueReceive(): Promise<QueueReceiveProbeResponse> {
  return apiPost<QueueReceiveProbeResponse>('/api/cloud/queue/receive/probe', {});
}
