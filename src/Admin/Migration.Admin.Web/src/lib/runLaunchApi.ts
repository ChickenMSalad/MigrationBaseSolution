import { adminApiBaseUrl } from './adminApi';

export type RunExecutionMode = 'dry-run' | 'full-run';
export type QueueFanOutMode = 'single-batch' | 'balanced' | 'maximum-throughput';

export interface RunLaunchRequest {
  projectId: string;
  manifestImportId?: string;
  mappingProfileId?: string;
  executionMode: RunExecutionMode;
  queueFanOutMode: QueueFanOutMode;
  requestedConcurrency: number;
  notes?: string;
}

export interface RunLaunchResponse {
  runId: string;
  projectId?: string;
  status: string;
  queuedWorkItems?: number;
  message?: string;
}

export interface RunProgressSnapshot {
  runId: string;
  status: string;
  totalWorkItems?: number;
  queuedWorkItems?: number;
  leasedWorkItems?: number;
  completedWorkItems?: number;
  failedWorkItems?: number;
  cancelledWorkItems?: number;
  updatedAtUtc?: string;
  message?: string;
}

async function parseJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  return (text ? JSON.parse(text) : {}) as T;
}

export async function launchMigrationRun(
  request: RunLaunchRequest,
  signal?: AbortSignal
): Promise<RunLaunchResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/runs/launch`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request),
    signal
  });

  const parsed = await parseJson<RunLaunchResponse>(response);

  if (!response.ok) {
    throw new Error(parsed.message || `Run launch failed: ${response.status} ${response.statusText}`);
  }

  return parsed;
}

export async function getRunProgress(
  runId: string,
  signal?: AbortSignal
): Promise<RunProgressSnapshot> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/runs/${encodeURIComponent(runId)}/progress`, {
    method: 'GET',
    headers: {
      Accept: 'application/json'
    },
    signal
  });

  const parsed = await parseJson<RunProgressSnapshot>(response);

  if (!response.ok) {
    throw new Error(parsed.message || `Run progress failed: ${response.status} ${response.statusText}`);
  }

  return parsed;
}

export async function cancelMigrationRun(
  runId: string,
  signal?: AbortSignal
): Promise<RunProgressSnapshot> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/runs/${encodeURIComponent(runId)}/cancel`, {
    method: 'POST',
    headers: {
      Accept: 'application/json'
    },
    signal
  });

  const parsed = await parseJson<RunProgressSnapshot>(response);

  if (!response.ok) {
    throw new Error(parsed.message || `Run cancellation failed: ${response.status} ${response.statusText}`);
  }

  return parsed;
}
