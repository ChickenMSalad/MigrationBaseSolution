import { apiGet } from "../../../../api/core/adminApiClient";
import type { FailureRetryResponse } from "../types/failureRetry";

type RuntimeDashboardSummary = {
  failedWorkItemCount?: number;
  failed?: number;
  retryableWorkItemCount?: number;
  retryQueuedWorkItemCount?: number;
  updatedUtc?: string | null;
};

function toNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function fromSummary(summary: RuntimeDashboardSummary): FailureRetryResponse {
  const failed = toNumber(summary.failedWorkItemCount ?? summary.failed);

  return {
    summary: {
      failed,
      retryable: toNumber(summary.retryableWorkItemCount),
      retryQueued: toNumber(summary.retryQueuedWorkItemCount),
      lastUpdatedUtc: summary.updatedUtc ?? new Date().toISOString(),
    },
    workItems: [],
    message:
      failed > 0
        ? "Failure summary is available. Detailed retry rows require the backend failure-detail endpoint to be reconciled."
        : "No failed work items were reported by the runtime dashboard summary.",
  };
}

export const failureRetryApi = {
  recent: async (_take = 50): Promise<FailureRetryResponse> => {
    const summary = await apiGet<RuntimeDashboardSummary>("/api/runtime/dashboard/summary");
    return fromSummary(summary);
  },
};
