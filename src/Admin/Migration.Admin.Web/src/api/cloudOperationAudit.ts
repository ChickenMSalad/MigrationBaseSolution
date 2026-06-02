import { apiGet, apiPost } from './core/adminApiClient';
import type { AuditWriteResult } from './auditPersistence';

export type CloudOperationAuditEventNames = {
  category: string;
  eventNames: string[];
};

export type CloudOperationAuditProbe = {
  workspaceId: string;
  eventCount: number;
  results: AuditWriteResult[];
};

export async function getCloudOperationAuditEventNames(): Promise<CloudOperationAuditEventNames> {
  return apiGet<CloudOperationAuditEventNames>('/api/cloud/audit/operation/event-names');
}

export async function probeCloudOperationAudit(): Promise<CloudOperationAuditProbe> {
  return apiPost<unknown, CloudOperationAuditProbe>('/api/cloud/audit/operation/probe', {});
}
