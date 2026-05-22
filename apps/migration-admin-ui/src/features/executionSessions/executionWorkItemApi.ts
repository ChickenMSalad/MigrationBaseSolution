import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionWorkItemListResponse,
  ExecutionWorkItemQueueSummary,
  ExpandExecutionPlanToWorkItemsRequest,
  LeaseExecutionWorkItemsRequest,
} from './executionWorkItemTypes';

export async function expandExecutionPlanToWorkItems(
  request: ExpandExecutionPlanToWorkItemsRequest,
): Promise<ExecutionWorkItemListResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-work-items/expand`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionWorkItemListResponse>;
}

export async function leaseExecutionWorkItems(
  request: LeaseExecutionWorkItemsRequest,
): Promise<ExecutionWorkItemListResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-work-items/lease`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionWorkItemListResponse>;
}

export async function fetchExecutionWorkItemQueueSummary(
  executionSessionId: string,
): Promise<ExecutionWorkItemQueueSummary> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-work-items/${executionSessionId}/summary`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionWorkItemQueueSummary>;
}

export async function fetchRecentExecutionWorkItems(
  executionSessionId: string,
  take = 100,
): Promise<ExecutionWorkItemListResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-work-items/${executionSessionId}/recent?take=${take}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionWorkItemListResponse>;
}
