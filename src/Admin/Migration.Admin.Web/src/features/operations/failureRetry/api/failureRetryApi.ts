import { apiGet } from "../../../../api/core/adminApiClient";
import type { FailureRetryResponse } from "../types/failureRetry";

type RawRecord = Record<string, unknown>;

function asRecord(value: unknown): RawRecord {
  return value && typeof value === "object" ? (value as RawRecord) : {};
}

function asArray(value: unknown): RawRecord[] {
  return Array.isArray(value) ? value.map(asRecord) : [];
}

function toNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function normalizeResponse(raw: unknown): FailureRetryResponse {
  const record = asRecord(raw);
  const summary = asRecord(record.summary ?? record.Summary);
  const workItems = asArray(record.workItems ?? record.WorkItems);

  return {
    summary: {
      failed: toNumber(summary.failed ?? summary.Failed),
      retryable: toNumber(summary.retryable ?? summary.Retryable),
      retryQueued: toNumber(summary.retryQueued ?? summary.RetryQueued),
      lastUpdatedUtc: String(summary.lastUpdatedUtc ?? summary.LastUpdatedUtc ?? new Date().toISOString()),
    },
    workItems: workItems.map((item) => ({
      workItemId: toNumber(item.workItemId ?? item.WorkItemId),
      runId: String(item.runId ?? item.RunId ?? ""),
      workType: item.workType === undefined || item.workType === null ? null : String(item.workType),
      status: String(item.status ?? item.Status ?? "Unknown"),
      attemptCount: toNumber(item.attemptCount ?? item.AttemptCount),
      claimedBy: item.claimedBy === undefined || item.claimedBy === null ? null : String(item.claimedBy),
      createdAtUtc: item.createdAtUtc === undefined || item.createdAtUtc === null ? null : String(item.createdAtUtc),
      updatedAtUtc: item.updatedAtUtc === undefined || item.updatedAtUtc === null ? null : String(item.updatedAtUtc),
      completedAtUtc: item.completedAtUtc === undefined || item.completedAtUtc === null ? null : String(item.completedAtUtc),
      lastErrorMessage: item.lastErrorMessage === undefined || item.lastErrorMessage === null ? null : String(item.lastErrorMessage),
    })),
    message: record.message === undefined || record.message === null ? null : String(record.message),
  };
}

export const failureRetryApi = {
  recent: async (take = 50): Promise<FailureRetryResponse> => {
    const raw = await apiGet(`/api/runtime/dashboard/failures?take=${encodeURIComponent(String(take))}`);
    return normalizeResponse(raw);
  },
};
