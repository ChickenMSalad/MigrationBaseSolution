export type EvaluateExecutionReplayAdmissionRequest = {
  take?: number | null;
};

export type ExecutionReplayAdmissionDecision = {
  executionSessionId: string;
  name: string;
  decision: string;
  reason: string;
  createdUtc: string;
};

export type ExecutionReplayAdmissionEvaluationResult = {
  generatedUtc: string;
  activeReplayCount: number;
  maxConcurrentReplays: number;
  withinAllowedWindow: boolean;
  decisions: ExecutionReplayAdmissionDecision[];
};

export type ExecutionReplayAdmissionDecisionRecord = {
  replayAdmissionDecisionId: string;
  executionSessionId: string;
  decision: string;
  reason: string;
  activeReplayCount: number;
  maxConcurrentReplays: number;
  withinAllowedWindow: boolean;
  createdUtc: string;
};

export type ExecutionReplayAdmissionDecisionHistoryResponse = {
  executionSessionId: string;
  decisions: ExecutionReplayAdmissionDecisionRecord[];
};
