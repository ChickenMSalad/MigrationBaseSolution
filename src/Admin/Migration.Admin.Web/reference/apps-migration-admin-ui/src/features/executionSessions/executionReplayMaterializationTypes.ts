export type MaterializeExecutionReplayRequest = {
  sourceExecutionSessionId: string;
  scope: string;
  approvalNote: string;
};

export type ExecutionReplayMaterializationResult = {
  sourceExecutionSessionId: string;
  replayExecutionSessionId: string;
  scope: string;
  replayDepth: number;
  workItemCount: number;
  createdUtc: string;
  findings: {
    severity: string;
    code: string;
    message: string;
  }[];
};
