import { apiGet } from './core/adminApiClient';

export type QueueProviderPlanDescriptor = {
  environmentName: string;
  workspaceId: string;
  queueProvider: string;
  providerKind: string;
  logicalQueueName: string;
  workspaceQueueName: string;
  serviceBusNamespace?: string | null;
  storageAccountName?: string | null;
  usesInMemory: boolean;
  usesAzureStorageQueue: boolean;
  usesServiceBus: boolean;
  requiresManagedIdentity: boolean;
  supportsDeadLettering: boolean;
  supportsSessions: boolean;
  supportsScheduledMessages: boolean;
  recommendedMessageProperties: string[];
  warnings: string[];
};

export async function getQueueProviderPlan(): Promise<QueueProviderPlanDescriptor> {
  return apiGet<QueueProviderPlanDescriptor>('/api/cloud/queue-provider-plan');
}
