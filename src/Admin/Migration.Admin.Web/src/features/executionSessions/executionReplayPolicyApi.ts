import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionReplayPolicyEvaluationHistoryResponse,
  ExecutionReplayPolicyEvaluationResult,
} from './executionReplayPolicyTypes';

export async function evaluateExecutionReplayPolicy(
  sourceExecutionSessionId: string,
  scope: string,
): Promise<ExecutionReplayPolicyEvaluationResult> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-replay/${sourceExecutionSessionId}/policy?scope=${encodeURIComponent(scope)}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayPolicyEvaluationResult>;
}

export async function fetchExecutionReplayPolicyHistory(
  sourceExecutionSessionId: string,
  take = 25,
): Promise<ExecutionReplayPolicyEvaluationHistoryResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-replay/${sourceExecutionSessionId}/policy/history?take=${take}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayPolicyEvaluationHistoryResponse>;
}
