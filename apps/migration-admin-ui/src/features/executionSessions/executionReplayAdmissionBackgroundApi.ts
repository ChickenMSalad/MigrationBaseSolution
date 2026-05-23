import { adminApiBaseUrl } from '../../lib/adminApi';
import type { ExecutionReplayAdmissionBackgroundStatus } from './executionReplayAdmissionBackgroundTypes';

export async function fetchExecutionReplayAdmissionBackgroundStatus(): Promise<ExecutionReplayAdmissionBackgroundStatus> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-replay/admission/background/status`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionReplayAdmissionBackgroundStatus>;
}
