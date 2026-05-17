import { apiGet } from './core/adminApiClient';

export type QueueExecutionObservabilitySnapshot = {
  generatedUtc: string;
  providerKind: string;
  queueName: string;
  receiveProviderConfigured: boolean;
  workerLoopEnabled: boolean;
  workerLoopDryRun: boolean;
  coordinatorDryRun: boolean;
  completeMessages: boolean;
  maxMessages: number;
  supportedMessageTypes: string[];
  warnings: string[];
};

export async function getQueueExecutionObservability():
  Promise<QueueExecutionObservabilitySnapshot> {
  return apiGet<QueueExecutionObservabilitySnapshot>(
    '/api/cloud/queue/execution-observability');
}
