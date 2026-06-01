export type ReplayAdmissionManualDecisionRequest = {
  executionSessionId: string;
  operator: string;
  reason: string;
};

export type ReplayAdmissionManualDecisionResult = {
  executionSessionId: string;
  decision: string;
  reason: string;
  createdUtc: string;
};
