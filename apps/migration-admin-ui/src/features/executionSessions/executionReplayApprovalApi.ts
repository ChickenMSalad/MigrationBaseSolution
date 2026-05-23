import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ApproveExecutionReplayRequest,
  ExecutionReplayApprovalResult,
} from './executionReplayApprovalTypes';

export async function approveExecutionReplay(
  request: ApproveExecutionReplayRequest,
): Promise<ExecutionReplayApprovalResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/approve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayApprovalResult>;
}
