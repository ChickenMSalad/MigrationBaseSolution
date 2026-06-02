import { apiGet, apiPost } from './core/adminApiClient';
import type { QueueMessageEnvelope } from './queueContracts';

export type QueueExecutionPlan = {
  messageType: string;
  workspaceId: string;
  tenantId?: string | null;
  projectId?: string | null;
  runId?: string | null;
  idempotencyKey: string;
  action: string;
  canExecute: boolean;
  requiresRunId: boolean;
  requiresProjectId: boolean;
  warnings: string[];
};

export type QueueExecutionPlanProbe = {
  envelope: QueueMessageEnvelope;
  plan: QueueExecutionPlan;
};

export type QueueExecutionMessageTypes = {
  supportedMessageTypes: string[];
};

export async function probeQueueExecutionPlan(): Promise<QueueExecutionPlanProbe> {
  return apiPost<unknown, QueueExecutionPlanProbe>('/api/cloud/queue/execution-plan/probe', {});
}

export async function getQueueExecutionMessageTypes(): Promise<QueueExecutionMessageTypes> {
  return apiGet<QueueExecutionMessageTypes>('/api/cloud/queue/execution-plan/message-types');
}
