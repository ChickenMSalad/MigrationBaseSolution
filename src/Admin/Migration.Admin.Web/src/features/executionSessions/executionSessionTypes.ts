export type ExecutionSessionRecord = {
  executionSessionId: string;
  migrationRunId?: string | null;
  name: string;
  sourceConnector?: string | null;
  targetConnector?: string | null;
  status: string;
  createdUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  notes?: string | null;
};

export type CreateExecutionSessionRequest = {
  migrationRunId?: string | null;
  name: string;
  sourceConnector?: string | null;
  targetConnector?: string | null;
  notes?: string | null;
};

export type RecentExecutionSessionsResponse = {
  take: number;
  sessions: ExecutionSessionRecord[];
};
