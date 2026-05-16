import { apiGet } from './core/adminApiClient';

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

export async function getRunExecutionPolicy(
  runId: string
): Promise<RunExecutionPolicyDescriptor> {
  return apiGet<RunExecutionPolicyDescriptor>(
    `/api/runs/${encodeURIComponent(runId)}/execution-policy`
  );
}
