export type OverrideExecutionReplayPolicyRequest = {
  sourceExecutionSessionId: string;
  scope: string;
  overriddenBy: string;
  overrideReason: string;
  expiresInMinutes: number;
};

export type ExecutionReplayPolicyOverrideRecord = {
  replayPolicyOverrideId: string;
  sourceExecutionSessionId: string;
  scope: string;
  policyDecision: string;
  policyScore: number;
  overriddenBy: string;
  overrideReason: string;
  status: string;
  expiresUtc: string;
  createdUtc: string;
  consumedUtc?: string | null;
  replayExecutionSessionId?: string | null;
};

export type ExecutionReplayPolicyOverrideResult = {
  override: ExecutionReplayPolicyOverrideRecord;
  violations: {
    severity: string;
    code: string;
    message: string;
  }[];
};

export type ExecutionReplayPolicyOverrideHistoryResponse = {
  sourceExecutionSessionId: string;
  overrides: ExecutionReplayPolicyOverrideRecord[];
};
