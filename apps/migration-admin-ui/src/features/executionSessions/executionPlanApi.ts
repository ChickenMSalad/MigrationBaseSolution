import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionPlanResponse,
  SeedExecutionPlanRequest,
} from './executionPlanTypes';

export async function seedExecutionPlan(
  request: SeedExecutionPlanRequest,
): Promise<ExecutionPlanResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-plan/seed`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionPlanResponse>;
}

export async function fetchExecutionPlan(
  executionSessionId: string,
): Promise<ExecutionPlanResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-plan/${executionSessionId}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionPlanResponse>;
}
