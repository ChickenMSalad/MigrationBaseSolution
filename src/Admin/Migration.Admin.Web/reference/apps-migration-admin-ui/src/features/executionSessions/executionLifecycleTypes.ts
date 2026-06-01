export type ExecutionPhaseHistoryRecord = {
  executionPhaseHistoryId: string;
  executionSessionId: string;
  migrationRunId?: string | null;
  previousPhase?: string | null;
  newPhase: string;
  reason?: string | null;
  createdUtc: string;
};

export type ExecutionPhaseCatalogResponse = {
  phases: string[];
};

export type ExecutionPhaseHistoryResponse = {
  executionSessionId: string;
  take: number;
  history: ExecutionPhaseHistoryRecord[];
};

export type TransitionExecutionPhaseRequest = {
  executionSessionId: string;
  newPhase: string;
  reason?: string | null;
};
