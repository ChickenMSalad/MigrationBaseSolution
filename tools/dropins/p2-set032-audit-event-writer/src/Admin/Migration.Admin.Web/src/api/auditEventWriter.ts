import { apiPost } from './core/adminApiClient';
import type { AuditWriteResult } from './auditPersistence';

export type AuditEventWriteRequest = {
  workspaceId: string;
  category: string;
  eventName: string;
  severity: string;
  tenantId?: string | null;
  projectId?: string | null;
  runId?: string | null;
  correlationId?: string | null;
  actor?: string | null;
  properties?: Record<string, string> | null;
};

export type AuditEventWriterProbe = {
  request: AuditEventWriteRequest;
  result: AuditWriteResult;
};

export async function probeAuditEventWriter(): Promise<AuditEventWriterProbe> {
  return apiPost<AuditEventWriterProbe>('/api/cloud/audit/writer/probe', {});
}
