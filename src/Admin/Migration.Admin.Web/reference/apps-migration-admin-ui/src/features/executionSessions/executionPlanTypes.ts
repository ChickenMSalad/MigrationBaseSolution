export type ExecutionPlanStepRecord = {
  executionPlanStepId: string;
  executionSessionId: string;
  migrationRunId?: string | null;
  stepOrder: number;
  stepType: string;
  stepName: string;
  status: string;
  sourceConnector?: string | null;
  targetConnector?: string | null;
  payloadJson?: string | null;
  createdUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  errorMessage?: string | null;
};

export type SeedExecutionPlanRequest = {
  executionSessionId: string;
  sourceConnector?: string | null;
  targetConnector?: string | null;
};

export type ExecutionPlanResponse = {
  executionSessionId: string;
  steps: ExecutionPlanStepRecord[];
};
