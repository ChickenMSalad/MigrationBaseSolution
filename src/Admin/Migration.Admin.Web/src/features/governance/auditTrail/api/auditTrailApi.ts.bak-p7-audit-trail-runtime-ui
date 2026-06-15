import { apiGet } from '../../../../api/core/adminApiClient';
import type { AuditTrailRecentResponse, AuditTrailSummary } from '../types/auditTrail';

export function getAuditTrailSummary(): Promise<AuditTrailSummary> {
  return apiGet<AuditTrailSummary>('/api/operational/audit-trail/summary');
}

export function getRecentAuditTrailEvents(take = 50): Promise<AuditTrailRecentResponse> {
  const normalizedTake = Number.isFinite(take) && take > 0 ? Math.min(Math.floor(take), 250) : 50;
  return apiGet<AuditTrailRecentResponse>(`/api/operational/audit-trail/recent?take=${normalizedTake}`);
}
