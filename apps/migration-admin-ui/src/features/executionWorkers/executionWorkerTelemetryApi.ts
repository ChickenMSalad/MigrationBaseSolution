import { adminApiBaseUrl } from '../../lib/adminApi';
import type { ExecutionWorkerTelemetrySummary } from './executionWorkerTelemetryTypes';

export async function fetchExecutionWorkerTelemetrySummary(
  staleAfterSeconds = 120,
): Promise<ExecutionWorkerTelemetrySummary> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/execution-workers/summary?staleAfterSeconds=${staleAfterSeconds}`,
  );

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ExecutionWorkerTelemetrySummary>;
}
