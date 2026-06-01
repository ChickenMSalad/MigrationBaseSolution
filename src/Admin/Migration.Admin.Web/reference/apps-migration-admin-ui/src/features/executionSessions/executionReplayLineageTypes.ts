export type ExecutionReplayLineageNode = {
  executionSessionId: string;
  replaySourceExecutionSessionId?: string | null;
  name: string;
  status: string;
  replayScope?: string | null;
  replayDepth: number;
  createdUtc: string;
};

export type ExecutionReplayLineageResult = {
  executionSessionId: string;
  rootExecutionSessionId: string;
  sourceExecutionSessionId?: string | null;
  replayDepth: number;
  replayScope?: string | null;
  ancestors: ExecutionReplayLineageNode[];
  children: ExecutionReplayLineageNode[];
};
