import { adminApiBaseUrl } from '../../lib/adminApi';
import type { OperationalEventQuery, OperationalEventQueryResponse } from './operationalEventTimelineTypes';

export async function queryOperationalEvents(
  query: OperationalEventQuery,
): Promise<OperationalEventQueryResponse> {
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

  parameters.set('skip', String(query.skip ?? 0));
  parameters.set('take', String(query.take ?? 50));

  const response = await fetch(`${adminApiBaseUrl}/api/operational/events/query?${parameters.toString()}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<OperationalEventQueryResponse>;
}
