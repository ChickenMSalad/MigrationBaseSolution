import { adminApiBaseUrl } from '../../lib/adminApi';
import type { ExecutionReplayPolicyEvaluationResult } from './executionReplayPolicyTypes';

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
