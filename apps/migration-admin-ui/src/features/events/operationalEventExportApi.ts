import { adminApiBaseUrl } from '../../lib/adminApi';
import type { OperationalEventQuery } from './operationalEventTimelineTypes';

export function buildOperationalEventCsvExportUrl(query: OperationalEventQuery): string {
  const parameters = new URLSearchParams();

  if (query.severity) {
    parameters.set('severity', query.severity);
  }

  if (query.category) {
    parameters.set('category', query.category);
  }

  if (query.eventType) {
    parameters.set('eventType', query.eventType);
  }

  if (query.fromUtc) {
    parameters.set('fromUtc', query.fromUtc);
  }

  if (query.toUtc) {
    parameters.set('toUtc', query.toUtc);
  }

  parameters.set('take', String(query.take ?? 250));

  return `${adminApiBaseUrl}/api/operational/events/export/csv?${parameters.toString()}`;
}
