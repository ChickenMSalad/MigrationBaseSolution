import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ExecutionReplayMaterializationResult,
  MaterializeExecutionReplayRequest,
} from './executionReplayMaterializationTypes';

export async function materializeExecutionReplay(
  request: MaterializeExecutionReplayRequest,
): Promise<ExecutionReplayMaterializationResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/materialize`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayMaterializationResult>;
}
