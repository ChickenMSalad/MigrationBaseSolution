import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ReplayAdmissionManualDecisionRequest,
  ReplayAdmissionManualDecisionResult,
} from './executionReplayAdmissionManualTypes';

export async function forceAdmitExecutionReplay(
  request: ReplayAdmissionManualDecisionRequest,
): Promise<ReplayAdmissionManualDecisionResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/admission/force-admit`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ReplayAdmissionManualDecisionResult>;
}

export async function forceDeferExecutionReplay(
  request: ReplayAdmissionManualDecisionRequest,
): Promise<ReplayAdmissionManualDecisionResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/admission/force-defer`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ReplayAdmissionManualDecisionResult>;
}
