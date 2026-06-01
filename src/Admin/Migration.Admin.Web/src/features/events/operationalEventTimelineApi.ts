import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  OperationalEventAggregateSummary,
  OperationalEventQuery,
  OperationalEventQueryResponse,
} from './operationalEventTimelineTypes';

function appendQueryParameters(parameters: URLSearchParams, query: OperationalEventQuery) {
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

  if (query.executionSessionId) {
    parameters.set('executionSessionId', query.executionSessionId);
  }

  if (query.migrationRunId) {
    parameters.set('migrationRunId', query.migrationRunId);
  }
}

export async function queryOperationalEvents(
  query: OperationalEventQuery,
): Promise<OperationalEventQueryResponse> {
  const parameters = new URLSearchParams();

  appendQueryParameters(parameters, query);
  parameters.set('skip', String(query.skip ?? 0));
  parameters.set('take', String(query.take ?? 50));

  const response = await fetch(`${adminApiBaseUrl}/api/operational/events/query?${parameters.toString()}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<OperationalEventQueryResponse>;
}

export async function fetchOperationalEventAggregateSummary(
  query: Pick<OperationalEventQuery, 'fromUtc' | 'toUtc'>,
): Promise<OperationalEventAggregateSummary> {
  const parameters = new URLSearchParams();

  if (query.fromUtc) {
    parameters.set('fromUtc', query.fromUtc);
  }

  if (query.toUtc) {
    parameters.set('toUtc', query.toUtc);
  }

  const response = await fetch(`${adminApiBaseUrl}/api/operational/events/query/summary?${parameters.toString()}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<OperationalEventAggregateSummary>;
}
