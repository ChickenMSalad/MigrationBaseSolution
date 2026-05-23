export type ExecutionReplayPolicyViolation = {
  severity: string;
  code: string;
  message: string;
};

export type ExecutionReplayPolicyMetrics = {
  replayDepth: number;
  preparedItemCount: number;
  totalWorkItemCount: number;
  failedWorkItemCount: number;
  deadLetteredWorkItemCount: number;
  activeReplayCount: number;
  deadLetteredPercent: number;
};

export type ExecutionReplayPolicyEvaluationResult = {
  sourceExecutionSessionId: string;
  scope: string;
  generatedUtc: string;
  decision: string;
  policyScore: number;
  violations: ExecutionReplayPolicyViolation[];
  metrics: ExecutionReplayPolicyMetrics;
};

export type ExecutionReplayPolicyEvaluationRecord = {
  replayPolicyEvaluationId: string;
  sourceExecutionSessionId: string;
  scope: string;
  decision: string;
  policyScore: number;
  metricsJson: string;
  violationsJson: string;
  createdUtc: string;
};

export type ExecutionReplayPolicyEvaluationHistoryResponse = {
  sourceExecutionSessionId: string;
  evaluations: ExecutionReplayPolicyEvaluationRecord[];
};
