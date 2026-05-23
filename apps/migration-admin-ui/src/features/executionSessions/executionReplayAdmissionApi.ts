import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  EvaluateExecutionReplayAdmissionRequest,
  ExecutionReplayAdmissionEvaluationResult,
} from './executionReplayAdmissionTypes';

export async function evaluateExecutionReplayAdmission(
  request: EvaluateExecutionReplayAdmissionRequest = {},
): Promise<ExecutionReplayAdmissionEvaluationResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/admission/evaluate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayAdmissionEvaluationResult>;
}
