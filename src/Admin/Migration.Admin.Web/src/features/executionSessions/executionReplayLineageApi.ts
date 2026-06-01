import { adminApiBaseUrl } from '../../lib/adminApi';
import type { ExecutionReplayLineageResult } from './executionReplayLineageTypes';

export async function fetchExecutionReplayLineage(
  executionSessionId: string,
): Promise<ExecutionReplayLineageResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/${executionSessionId}/lineage`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayLineageResult>;
}
