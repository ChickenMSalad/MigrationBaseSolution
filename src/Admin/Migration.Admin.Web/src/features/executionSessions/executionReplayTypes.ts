export type ExecutionReplayFinding = {
  severity: string;
  code: string;
  message: string;
};

export type ExecutionReplayStateSummary = {
  sessionStatus?: string | null;
  planStepCount: number;
  workItemCount: number;
  pendingWorkItems: number;
  leasedWorkItems: number;
  completedWorkItems: number;
  failedWorkItems: number;
  deadLetteredWorkItems: number;
  cancelledWorkItems: number;
  operationalEventCount: number;
  phaseTransitionCount: number;
};

export type ExecutionReplayAnalysisResult = {
  executionSessionId: string;
  generatedUtc: string;
  replayRecommended: boolean;
  recommendation: string;
  riskScore: number;
  findings: ExecutionReplayFinding[];
  stateSummary: ExecutionReplayStateSummary;
};
