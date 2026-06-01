import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionReplayPreparationResult,
  PrepareExecutionReplayRequest,
} from './executionReplayPreparationTypes';

export async function prepareExecutionReplayManifest(
  request: PrepareExecutionReplayRequest,
): Promise<ExecutionReplayPreparationResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/prepare`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayPreparationResult>;
}
