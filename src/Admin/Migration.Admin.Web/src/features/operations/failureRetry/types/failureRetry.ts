export type FailureRetryStatus = "Failed" | "FailedRetryable" | "RetryQueued" | "Completed" | "Unknown";

export type FailureRetryWorkItem = {
  workItemId: number;
  runId: string;
  workType?: string | null;
  status: FailureRetryStatus | string;
  attemptCount?: number | null;
  claimedBy?: string | null;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  lastErrorMessage?: string | null;
};

export type FailureRetrySummary = {
  failed: number;
  retryable: number;
  retryQueued: number;
  lastUpdatedUtc?: string | null;
};

export type FailureRetryResponse = {
  summary: FailureRetrySummary;
  workItems: FailureRetryWorkItem[];
};
