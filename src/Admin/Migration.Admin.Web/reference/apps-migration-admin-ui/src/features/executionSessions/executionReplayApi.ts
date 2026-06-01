import { adminApiBaseUrl } from '../../lib/adminApi';
import type { ExecutionReplayAnalysisResult } from './executionReplayTypes';

export async function analyzeExecutionReplayReadiness(
  executionSessionId: string,
): Promise<ExecutionReplayAnalysisResult> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/${executionSessionId}/analysis`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayAnalysisResult>;
}
