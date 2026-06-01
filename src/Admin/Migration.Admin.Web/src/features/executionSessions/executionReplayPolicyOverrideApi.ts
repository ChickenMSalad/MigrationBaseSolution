import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionReplayPolicyOverrideHistoryResponse,
  ExecutionReplayPolicyOverrideResult,
  OverrideExecutionReplayPolicyRequest,
} from './executionReplayPolicyOverrideTypes';

export async function overrideExecutionReplayPolicy(
  request: OverrideExecutionReplayPolicyRequest,
): Promise<ExecutionReplayPolicyOverrideResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/policy/override`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayPolicyOverrideResult>;
}

export async function fetchExecutionReplayPolicyOverrideHistory(
  sourceExecutionSessionId: string,
  take = 25,
): Promise<ExecutionReplayPolicyOverrideHistoryResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-replay/${sourceExecutionSessionId}/policy/overrides?take=${take}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayPolicyOverrideHistoryResponse>;
}
