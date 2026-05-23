export type EvaluateExecutionReplayAdmissionHealthRequest = {
  emitEvents: boolean;
  take?: number | null;
};

export type ExecutionReplayAdmissionStalePendingSession = {
  executionSessionId: string;
  name: string;
  status: string;
  replayScope?: string | null;
  replayDepth: number;
  createdUtc: string;
  ageMinutes: number;
};

export type ExecutionReplayAdmissionHealthResult = {
  generatedUtc: string;
  stalePendingMinutes: number;
  pendingCount: number;
  stalePendingCount: number;
  stalePendingSessions: ExecutionReplayAdmissionStalePendingSession[];
};
