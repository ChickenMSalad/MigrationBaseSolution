export type ApproveExecutionReplayRequest = {
  sourceExecutionSessionId: string;
  scope: string;
  approvedBy: string;
  approvalNote: string;
  expiresInMinutes: number;
};

export type ExecutionReplayApprovalRecord = {
  replayApprovalId: string;
  sourceExecutionSessionId: string;
  scope: string;
  approvedBy: string;
  approvalNote: string;
  status: string;
  expiresUtc: string;
  createdUtc: string;
  consumedUtc?: string | null;
  replayExecutionSessionId?: string | null;
};

export type ExecutionReplayApprovalResult = {
  approval: ExecutionReplayApprovalRecord;
  findings: {
    severity: string;
    code: string;
    message: string;
  }[];
};
