import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  CreateExecutionSessionRequest,
  ExecutionSessionRecord,
  RecentExecutionSessionsResponse,
} from './executionSessionTypes';

export async function createExecutionSession(
  request: CreateExecutionSessionRequest,
): Promise<ExecutionSessionRecord> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-sessions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionSessionRecord>;
}

export async function fetchRecentExecutionSessions(take = 25): Promise<RecentExecutionSessionsResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-sessions/recent?take=${take}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<RecentExecutionSessionsResponse>;
}
