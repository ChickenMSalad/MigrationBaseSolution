import { apiGet, apiPost } from './core/adminApiClient';

export type AuditRecord = {
  auditId: string;
  workspaceId: string;
  tenantId?: string | null;
  category: string;
  eventName: string;
  severity: string;
  correlationId: string;
  projectId?: string | null;
  runId?: string | null;
  actor: string;
  createdUtc: string;
  properties: Record<string, string>;
};

export type AuditPersistenceProviderDescriptor = {
  providerKind: string;
  isConfigured: boolean;
  isDurable: boolean;
  supportsQuery: boolean;
  supportsArtifactLinking: boolean;
  warnings: string[];
};

export type AuditWriteResult = {
  accepted: boolean;
  providerKind: string;
  auditId: string;
  writtenUtc: string;
  artifactObjectKey?: string | null;
};

export type AuditPersistenceProbe = {
  record: AuditRecord;
  result: AuditWriteResult;
};

export type RecentAuditRecordsResponse = {
  workspaceId: string;
  count: number;
  records: AuditRecord[];
};

export async function getAuditPersistenceProvider(): Promise<AuditPersistenceProviderDescriptor> {
  return apiGet<AuditPersistenceProviderDescriptor>('/api/cloud/audit/persistence/provider');
}

export async function probeAuditPersistence(): Promise<AuditPersistenceProbe> {
  return apiPost<unknown, AuditPersistenceProbe>('/api/cloud/audit/persistence/probe', {});
}

export async function getRecentAuditRecords(take = 25): Promise<RecentAuditRecordsResponse> {
  return apiGet<RecentAuditRecordsResponse>(`/api/cloud/audit/persistence/recent?take=${take}`);
}
