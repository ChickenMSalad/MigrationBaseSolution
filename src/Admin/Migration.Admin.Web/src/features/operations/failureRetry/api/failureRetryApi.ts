import { apiGet } from "../../../../api/core/adminApiClient";
import type { FailureRetryResponse, FailureRetryWorkItem } from "../types/failureRetry";

type RawRecord = Record<string, unknown>;

function asRecord(value: unknown): RawRecord {
  return value && typeof value === "object" ? value as RawRecord : {};
}

function asArray(value: unknown): RawRecord[] {
  return Array.isArray(value) ? value.map(asRecord) : [];
}

function stringValue(value: unknown, fallback = ""): string {
  return value === undefined || value === null ? fallback : String(value);
}

function numberValue(value: unknown): number | null {
  if (value === undefined || value === null || value === "") {
    return null;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function nullableString(value: unknown): string | null {
  if (value === undefined || value === null || value === "") {
    return null;
  }

  return String(value);
}

function readFirst(record: RawRecord, keys: string[]): unknown {
  for (const key of keys) {
    if (Object.prototype.hasOwnProperty.call(record, key)) {
      return record[key];
    }
  }

  return undefined;
}

function normalizeWorkItem(record: RawRecord): FailureRetryWorkItem {
  return {
    workItemId: numberValue(readFirst(record, ["workItemId", "WorkItemId", "id", "Id"])) ?? 0,
    runId: stringValue(readFirst(record, ["runId", "RunId", "migrationRunId", "MigrationRunId"])),
    workType: nullableString(readFirst(record, ["workType", "WorkType", "type", "Type"])),
    status: stringValue(readFirst(record, ["status", "Status"]), "Unknown"),
    attemptCount: numberValue(readFirst(record, ["attemptCount", "AttemptCount", "attempts", "Attempts"])),
    claimedBy: nullableString(readFirst(record, ["claimedBy", "ClaimedBy", "worker", "Worker"])),
    createdAtUtc: nullableString(readFirst(record, ["createdAtUtc", "CreatedAtUtc", "createdUtc", "CreatedUtc"])),
    updatedAtUtc: nullableString(readFirst(record, ["updatedAtUtc", "UpdatedAtUtc", "lastUpdatedUtc", "LastUpdatedUtc"])),
    completedAtUtc: nullableString(readFirst(record, ["completedAtUtc", "CompletedAtUtc"])),
    lastErrorMessage: nullableString(readFirst(record, ["lastErrorMessage", "LastErrorMessage", "error", "Error", "message", "Message"])),
  };
}

function normalizeFailureRetryResponse(raw: unknown): FailureRetryResponse {
  const record = asRecord(raw);
  const summary = asRecord(record.summary ?? record.Summary);
  const rawItems = asArray(
    record.workItems ??
    record.WorkItems ??
    record.failures ??
    record.Failures ??
    record.items ??
    record.Items ??
    record.recentFailures ??
    record.RecentFailures,
  );

  const workItems = rawItems.map(normalizeWorkItem);

  const failed = numberValue(summary.failed ?? summary.Failed) ?? workItems.length;
  const retryable = numberValue(summary.retryable ?? summary.Retryable) ??
    workItems.filter((item) => String(item.status ?? "").toLowerCase().includes("retry")).length;
  const retryQueued = numberValue(summary.retryQueued ?? summary.RetryQueued) ??
    workItems.filter((item) => String(item.status ?? "").toLowerCase().includes("queue")).length;

  return {
    summary: {
      failed,
      retryable,
      retryQueued,
      lastUpdatedUtc: nullableString(summary.lastUpdatedUtc ?? summary.LastUpdatedUtc) ?? new Date().toISOString(),
    },
    workItems,
  };
}

export const failureRetryApi = {
  recent: async (take = 50): Promise<FailureRetryResponse> => {
    const limit = Math.max(1, Math.min(take, 250));
    const raw = await apiGet<unknown>(`/api/operational/failures/recent?limit=${encodeURIComponent(String(limit))}`);
    return normalizeFailureRetryResponse(raw);
  },
};
