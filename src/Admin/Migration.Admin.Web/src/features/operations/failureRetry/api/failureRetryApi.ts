import { apiGet, apiPost } from "../../../../api/core/adminApiClient";
import type { FailureRetryResponse } from "../types/failureRetry";

export type RetryWorkItemResponse = {
  success?: boolean;
  message?: string | null;
  currentStatus?: string | null;
  workItemId?: number;
};

export const failureRetryApi = {
  recent: async (take = 50): Promise<FailureRetryResponse> => {
    const query = Number.isFinite(take) && take > 0 ? `?take=${Math.floor(take)}` : "";
    return apiGet<FailureRetryResponse>(`/api/runtime/dashboard/failures${query}`);
  },

  retryWorkItem: async (workItemId: number, reason?: string): Promise<RetryWorkItemResponse> => {
    if (!Number.isFinite(workItemId) || workItemId <= 0) {
      throw new Error("A valid work item id is required.");
    }

    return apiPost<RetryWorkItemResponse>(`/api/operational/work-items/${Math.trunc(workItemId)}/reset`, {
      requestedBy: "admin-ui",
      reason: reason?.trim() || "Retry requested from Failure Retry page.",
    });
  },
};
