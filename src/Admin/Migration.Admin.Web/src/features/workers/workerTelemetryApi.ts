import type {
  OperationalWorkerLeaseResponse,
  OperationalWorkerTelemetryResponse,
} from './workerTelemetryTypes';

const apiBaseUrl =
  (import.meta.env.VITE_ADMIN_API_BASE_URL as string | undefined)?.replace(/\/$/, '') || '';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}

export function getWorkerTelemetry(runId?: string): Promise<OperationalWorkerTelemetryResponse> {
  const query = runId ? `?runId=${encodeURIComponent(runId)}` : '';
  return getJson<OperationalWorkerTelemetryResponse>(`/api/operational/workers/telemetry${query}`);
}

export function getWorkerLeases(runId?: string): Promise<OperationalWorkerLeaseResponse> {
  const query = runId ? `?runId=${encodeURIComponent(runId)}` : '';
  return getJson<OperationalWorkerLeaseResponse>(`/api/operational/workers/leases${query}`);
}
