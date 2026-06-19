export type ExecutionWorkerHeartbeatRecord = {
  workerId: string;
  executionSessionId?: string | null;
  status: string;
  lastHeartbeatUtc: string;
  activeLeaseCount: number;
  message?: string | null;
  createdUtc: string;
};

export type ExecutionWorkerTelemetrySummary = {
  totalWorkers: number;
  activeWorkers: number;
  idleWorkers: number;
  staleWorkers: number;
  generatedUtc: string;
  workers: ExecutionWorkerHeartbeatRecord[];
};

export type OperationalWorkerQueueTelemetry = {
  ready: number;
  leased: number;
  inFlight: number;
  failed: number;
  completed: number;
  retryable: number;
};

export type OperationalWorkerTelemetryItem = {
  workerId: string;
  status: string;
  lastHeartbeatUtc: string;
  activeLeases: number;
  inFlightWorkItems: number;
  role: string;
};

export type OperationalWorkerTelemetryResponse = {
  generatedUtc: string;
  runId?: string | null;
  workers: OperationalWorkerTelemetryItem[];
  queue: OperationalWorkerQueueTelemetry;
  warnings: string[];
};

export type WorkerHealthStatus = "Online" | "Busy" | "Idle" | "Stale" | "Offline" | "Unknown";

export type WorkerHealthRow = {
  workerId: string;
  source: string;
  status: WorkerHealthStatus;
  lastSeenUtc?: string | null;
  heartbeatAgeSeconds?: number | null;
  activeLeases: number;
  inFlightWorkItems: number;
  executionSessionId?: string | null;
  role?: string | null;
  message?: string | null;
};

export type WorkerHealthDiagnostics = {
  generatedUtc: string;
  staleAfterSeconds: number;
  executionSummary: ExecutionWorkerTelemetrySummary;
  operationalTelemetry: OperationalWorkerTelemetryResponse;
  workers: WorkerHealthRow[];
  warnings: string[];
};
