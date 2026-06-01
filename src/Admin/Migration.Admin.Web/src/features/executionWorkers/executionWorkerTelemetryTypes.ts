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
