export type RuntimeReadinessStatus = 'ready' | 'warning' | 'blocked' | 'unknown';

export interface RuntimeReadinessSummary {
  status: RuntimeReadinessStatus;
  checkedAtUtc?: string;
  message?: string;
  checks?: RuntimeReadinessCheck[];
}

export interface RuntimeReadinessCheck {
  name: string;
  status: RuntimeReadinessStatus;
  message?: string;
}

export interface OperationalRunSummary {
  runId: string;
  projectId?: string;
  status: string;
  createdAtUtc?: string;
  startedAtUtc?: string;
  completedAtUtc?: string;
  totalWorkItems?: number;
  completedWorkItems?: number;
  failedWorkItems?: number;
  leasedWorkItems?: number;
}

export interface WorkItemQueueSummary {
  queued?: number;
  leased?: number;
  completed?: number;
  failed?: number;
  retryPending?: number;
  deadLettered?: number;
}

export interface OperationalRuntimeDashboardModel {
  readiness: RuntimeReadinessSummary;
  runs: OperationalRunSummary[];
  queue: WorkItemQueueSummary;
}

const defaultBaseUrl =
  (import.meta.env.VITE_ADMIN_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ||
  '';

async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${defaultBaseUrl}${path}`, {
    method: 'GET',
    headers: {
      Accept: 'application/json'
    },
    signal
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}

async function tryGetJson<T>(path: string, fallback: T, signal?: AbortSignal): Promise<T> {
  try {
    return await getJson<T>(path, signal);
  } catch {
    return fallback;
  }
}

export async function getOperationalRuntimeDashboard(
  signal?: AbortSignal
): Promise<OperationalRuntimeDashboardModel> {
  const [readiness, runs, queue] = await Promise.all([
    tryGetJson<RuntimeReadinessSummary>(
      '/api/operational/runtime/readiness',
      {
        status: 'unknown',
        message: 'Runtime readiness endpoint is not available yet.'
      },
      signal
    ),
    tryGetJson<OperationalRunSummary[]>('/api/operational/runs', [], signal),
    tryGetJson<WorkItemQueueSummary>('/api/operational/work-items/summary', {}, signal)
  ]);

  return {
    readiness,
    runs,
    queue
  };
}
