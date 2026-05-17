import { apiGet, apiPost } from './core/adminApiClient';
import type { AuditWriteResult } from './auditPersistence';
import type { QueueMessageEnvelope } from './queueContracts';
import type { QueueExecutionPlan } from './queueExecutionPlanner';

export type QueueAuditEventNames = {
  category: string;
  eventNames: string[];
};

export type QueueAuditProbe = {
  envelope: QueueMessageEnvelope;
  plan: QueueExecutionPlan;
  auditResults: AuditWriteResult[];
};

export async function getQueueAuditEventNames(): Promise<QueueAuditEventNames> {
  return apiGet<QueueAuditEventNames>('/api/cloud/queue/audit/event-names');
}

export async function probeQueueAuditEvents(): Promise<QueueAuditProbe> {
  return apiPost<QueueAuditProbe>('/api/cloud/queue/audit/probe', {});
}
