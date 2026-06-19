import { apiGet } from "../../../../api/core/adminApiClient";
import type {
  ExecutionWorkerHeartbeatRecord,
  ExecutionWorkerTelemetrySummary,
  OperationalWorkerTelemetryItem,
  OperationalWorkerTelemetryResponse,
  WorkerHealthDiagnostics,
  WorkerHealthRow,
  WorkerHealthStatus,
} from "../types/executionWorkerTelemetry";

function clampStaleAfterSeconds(value: number): number {
  if (!Number.isFinite(value)) {
    return 120;
  }

  if (value < 15) {
    return 15;
  }

  if (value > 3600) {
    return 3600;
  }

  return Math.trunc(value);
}

function parseDate(value?: string | null): Date | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function ageSeconds(generatedUtc: string, lastSeenUtc?: string | null): number | null {
  const generated = parseDate(generatedUtc);
  const lastSeen = parseDate(lastSeenUtc);

  if (!generated || !lastSeen) {
    return null;
  }

  return Math.max(0, Math.trunc((generated.getTime() - lastSeen.getTime()) / 1000));
}

function normalizeStatus(value?: string | null): string {
  return String(value ?? "").trim().toLowerCase();
}

function statusFromHeartbeat(worker: ExecutionWorkerHeartbeatRecord, age: number | null, staleAfterSeconds: number): WorkerHealthStatus {
  const status = normalizeStatus(worker.status);

  if (age !== null && age > staleAfterSeconds) {
    return "Stale";
  }

  if (status.includes("stale") || status.includes("offline")) {
    return "Stale";
  }

  if (worker.activeLeaseCount > 0 || status.includes("active") || status.includes("running") || status.includes("busy")) {
    return "Busy";
  }

  if (status.includes("idle")) {
    return "Idle";
  }

  return age === null ? "Unknown" : "Online";
}

function statusFromOperational(worker: OperationalWorkerTelemetryItem, age: number | null, staleAfterSeconds: number): WorkerHealthStatus {
  const status = normalizeStatus(worker.status);

  if (age !== null && age > staleAfterSeconds) {
    return "Stale";
  }

  if (status.includes("stale") || status.includes("offline")) {
    return "Stale";
  }

  if (worker.activeLeases > 0 || worker.inFlightWorkItems > 0 || status.includes("active") || status.includes("running")) {
    return "Busy";
  }

  if (status.includes("idle")) {
    return "Idle";
  }

  return age === null ? "Unknown" : "Online";
}

function buildDiagnostics(
  staleAfterSeconds: number,
  executionSummary: ExecutionWorkerTelemetrySummary,
  operationalTelemetry: OperationalWorkerTelemetryResponse,
): WorkerHealthDiagnostics {
  const generatedUtc = operationalTelemetry.generatedUtc || executionSummary.generatedUtc;
  const rows: WorkerHealthRow[] = [];

  for (const worker of executionSummary.workers ?? []) {
    const age = ageSeconds(generatedUtc, worker.lastHeartbeatUtc);
    rows.push({
      workerId: worker.workerId,
      source: "Executor heartbeat",
      status: statusFromHeartbeat(worker, age, staleAfterSeconds),
      lastSeenUtc: worker.lastHeartbeatUtc,
      heartbeatAgeSeconds: age,
      activeLeases: worker.activeLeaseCount ?? 0,
      inFlightWorkItems: worker.activeLeaseCount ?? 0,
      executionSessionId: worker.executionSessionId,
      role: "ServiceBus executor",
      message: worker.message,
    });
  }

  for (const worker of operationalTelemetry.workers ?? []) {
    const age = ageSeconds(generatedUtc, worker.lastHeartbeatUtc);
    rows.push({
      workerId: worker.workerId,
      source: "SQL work-item projection",
      status: statusFromOperational(worker, age, staleAfterSeconds),
      lastSeenUtc: worker.lastHeartbeatUtc,
      heartbeatAgeSeconds: age,
      activeLeases: worker.activeLeases ?? 0,
      inFlightWorkItems: worker.inFlightWorkItems ?? 0,
      role: worker.role,
      message: null,
    });
  }

  const queue = operationalTelemetry.queue;
  const warnings = [...(operationalTelemetry.warnings ?? [])];

  if ((queue?.ready ?? 0) > 0 && (queue?.leased ?? 0) === 0 && (queue?.inFlight ?? 0) === 0) {
    warnings.push("Queued work exists, but no leased or running work items are visible. Confirm dispatcher polling and queue dispatch state.");
  }

  if ((queue?.leased ?? 0) > 0 && executionSummary.totalWorkers === 0) {
    warnings.push("Leased work exists, but no executor heartbeat rows are visible. Confirm ServiceBus executor heartbeat persistence.");
  }

  return {
    generatedUtc,
    staleAfterSeconds,
    executionSummary,
    operationalTelemetry,
    workers: rows.sort((left, right) => String(left.workerId).localeCompare(String(right.workerId))),
    warnings,
  };
}

export const executionWorkerTelemetryApi = {
  summary(staleAfterSeconds = 120): Promise<ExecutionWorkerTelemetrySummary> {
    const seconds = clampStaleAfterSeconds(staleAfterSeconds);
    return apiGet<ExecutionWorkerTelemetrySummary>(
      `/api/operational/execution-workers/summary?staleAfterSeconds=${seconds}`,
    );
  },

  operational(): Promise<OperationalWorkerTelemetryResponse> {
    return apiGet<OperationalWorkerTelemetryResponse>("/api/operational/workers/telemetry");
  },

  async diagnostics(staleAfterSeconds = 120): Promise<WorkerHealthDiagnostics> {
    const seconds = clampStaleAfterSeconds(staleAfterSeconds);
    const [executionSummary, operationalTelemetry] = await Promise.all([
      this.summary(seconds),
      this.operational(),
    ]);

    return buildDiagnostics(seconds, executionSummary, operationalTelemetry);
  },
};
