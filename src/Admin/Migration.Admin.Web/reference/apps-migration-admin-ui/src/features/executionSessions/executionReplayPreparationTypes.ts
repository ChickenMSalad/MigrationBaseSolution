export type PrepareExecutionReplayRequest = {
  executionSessionId: string;
  scope: string;
  reason?: string | null;
};

export type ExecutionReplayPreparationItem = {
  sourceExecutionWorkItemId?: string | null;
  sourceExecutionPlanStepId?: string | null;
  replayOrder: number;
  replayType: string;
  replayName: string;
  sourceStatus: string;
  payloadJson?: string | null;
};

export type ExecutionReplayPreparationResult = {
  sourceExecutionSessionId: string;
  generatedUtc: string;
  scope: string;
  requiresApproval: boolean;
  canPrepareReplay: boolean;
  recommendation: string;
  items: ExecutionReplayPreparationItem[];
  findings: {
    severity: string;
    code: string;
    message: string;
  }[];
};
