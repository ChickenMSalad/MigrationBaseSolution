import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionPhaseCatalogResponse,
  ExecutionPhaseHistoryResponse,
  TransitionExecutionPhaseRequest,
  ExecutionPhaseHistoryRecord,
} from './executionLifecycleTypes';

export async function fetchExecutionPhases(): Promise<ExecutionPhaseCatalogResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-lifecycle/phases`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionPhaseCatalogResponse>;
}

export async function transitionExecutionPhase(
  request: TransitionExecutionPhaseRequest,
): Promise<ExecutionPhaseHistoryRecord> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-lifecycle/transition`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionPhaseHistoryRecord>;
}

export async function fetchExecutionPhaseHistory(
  executionSessionId: string,
  take = 25,
): Promise<ExecutionPhaseHistoryResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-lifecycle/${executionSessionId}/history?take=${take}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionPhaseHistoryResponse>;
}
