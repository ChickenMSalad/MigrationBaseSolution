import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  EvaluateExecutionReplayAdmissionHealthRequest,
  ExecutionReplayAdmissionHealthResult,
} from './executionReplayAdmissionHealthTypes';

export async function evaluateExecutionReplayAdmissionHealth(
  request: EvaluateExecutionReplayAdmissionHealthRequest,
): Promise<ExecutionReplayAdmissionHealthResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/admission/health/evaluate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayAdmissionHealthResult>;
}
