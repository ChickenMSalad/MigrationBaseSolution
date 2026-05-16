const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? '').replace(/\/$/, '');

export type RunExecutionPolicyDescriptor = {
  runId: string;
  jobName: string;
  status: string;
  lifecycleStage: string;
  isTerminal: boolean;
  idempotencyKey: string;
  leaseResource: string;
  leaseDurationSeconds: number;
  heartbeatIntervalSeconds: number;
  maxAttempts: number;
  canAcquireLease: boolean;
  canRetry: boolean;
  canRetryFailures: boolean;
  canResume: boolean;
  shouldDeadLetterOnMaxAttempts: boolean;
  poisonHandlingMode: string;
  recommendedWorkerActions: string[];
  updatedUtc: string;
  completedUtc?: string | null;
};

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`);

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function getRunExecutionPolicy(runId: string): Promise<RunExecutionPolicyDescriptor> {
  return getJson<RunExecutionPolicyDescriptor>(
    `/api/runs/${encodeURIComponent(runId)}/execution-policy`
  );
}
