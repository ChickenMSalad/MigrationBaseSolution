import { adminApiBaseUrl } from '../../lib/adminApi';
import type { AuditTrailRecentResponse, AuditTrailSummary } from './auditTrailTypes';

async function readJson<T>(path: string): Promise<T> {
  const response = await fetch(`${adminApiBaseUrl}${path}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function fetchAuditTrailSummary(): Promise<AuditTrailSummary> {
  return readJson<AuditTrailSummary>('/api/operational/audit-trail/summary');
}

export async function fetchRecentAuditTrailEvents(take = 50): Promise<AuditTrailRecentResponse> {
  return readJson<AuditTrailRecentResponse>(`/api/operational/audit-trail/recent?take=${take}`);
}
