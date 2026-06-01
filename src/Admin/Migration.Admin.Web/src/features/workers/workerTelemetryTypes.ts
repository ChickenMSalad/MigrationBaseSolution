export interface OperationalWorkerTelemetryResponse {
  generatedUtc: string;
  runId?: string | null;
  workers: OperationalWorkerTelemetryItem[];
  queue: OperationalWorkerQueueTelemetry;
  warnings: string[];
}

export interface OperationalWorkerTelemetryItem {
  workerId: string;
  status: string;
  lastHeartbeatUtc: string;
  activeLeases: number;
  inFlightWorkItems: number;
  role: string;
}

export interface OperationalWorkerQueueTelemetry {
  ready: number;
  leased: number;
  inFlight: number;
  failed: number;
  completed: number;
}

export interface OperationalWorkerLeaseResponse {
  generatedUtc: string;
  runId?: string | null;
  leases: OperationalWorkerLeaseItem[];
}

export interface OperationalWorkerLeaseItem {
  leaseId: string;
  workerId: string;
  status: string;
  expiresUtc: string;
  secondsRemaining: number;
}
