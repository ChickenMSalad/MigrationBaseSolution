import { apiGet } from "../../../../api/core/adminApiClient";
import type { OperationalEventTimelineItem, OperationalEventTimelineResponse } from "../types/operationalEvents";

type RawRecord = Record<string, unknown>;

function asRecord(value: unknown): RawRecord {
  return value && typeof value === "object" ? value as RawRecord : {};
}

function asArray(value: unknown): RawRecord[] {
  return Array.isArray(value) ? value.map(asRecord) : [];
}

function readFirst(record: RawRecord, keys: string[]): unknown {
  for (const key of keys) {
    if (Object.prototype.hasOwnProperty.call(record, key)) {
      return record[key];
    }
  }

  return undefined;
}

function stringValue(value: unknown, fallback = ""): string {
  return value === undefined || value === null ? fallback : String(value);
}

function nullableString(value: unknown): string | null {
  if (value === undefined || value === null || value === "") {
    return null;
  }

  return String(value);
}

function nullableNumber(value: unknown): number | null {
  if (value === undefined || value === null || value === "") {
    return null;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function normalizeEvent(record: RawRecord, index: number): OperationalEventTimelineItem {
  const createdAtUtc = stringValue(
    readFirst(record, ["createdAtUtc", "CreatedAtUtc", "createdUtc", "CreatedUtc", "timestampUtc", "TimestampUtc", "occurredAtUtc", "OccurredAtUtc"]),
    new Date().toISOString(),
  );

  const eventId = stringValue(
    readFirst(record, ["eventId", "EventId", "operationalEventId", "OperationalEventId", "id", "Id"]),
    `${createdAtUtc}-${index}`,
  );

  return {
    eventId,
    runId: nullableString(readFirst(record, ["runId", "RunId", "migrationRunId", "MigrationRunId"])),
    workItemId: nullableNumber(readFirst(record, ["workItemId", "WorkItemId"])),
    eventType: stringValue(readFirst(record, ["eventType", "EventType", "type", "Type"]), "OperationalEvent"),
    severity: stringValue(readFirst(record, ["severity", "Severity"]), "Information"),
    message: stringValue(readFirst(record, ["message", "Message"]), ""),
    source: nullableString(readFirst(record, ["source", "Source"])),
    createdAtUtc,
  };
}

function normalizeTimeline(raw: unknown): OperationalEventTimelineResponse {
  const record = asRecord(raw);
  const events = asArray(record.events ?? record.Events ?? record.items ?? record.Items)
    .map(normalizeEvent);

  return {
    generatedAtUtc: new Date().toISOString(),
    events,
  };
}

export async function getOperationalEventTimeline(runId?: string): Promise<OperationalEventTimelineResponse> {
  const selectedRunId = runId?.trim();
  const path = selectedRunId
    ? `/api/operational/events/query?migrationRunId=${encodeURIComponent(selectedRunId)}&take=100`
    : "/api/operational/events/recent?take=100";

  const raw = await apiGet<unknown>(path);
  return normalizeTimeline(raw);
}
