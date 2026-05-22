export type ExecutionWorkItemRecord = {
  executionWorkItemId: string;
  executionSessionId: string;
  migrationRunId?: string | null;
  executionPlanStepId?: string | null;
  workItemType: string;
  workItemName: string;
  status: string;
  priority: number;
  retryCount: number;
  maxRetries: number;
  workerId?: string | null;
  leaseId?: string | null;
  leaseExpiresUtc?: string | null;
  payloadJson?: string | null;
  createdUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  errorMessage?: string | null;
};

export type ExecutionWorkItemQueueSummary = {
  executionSessionId: string;
  total: number;
  pending: number;
  leased: number;
  running: number;
  completed: number;
  failed: number;
  deadLettered: number;
};

export type ExecutionWorkItemListResponse = {
  executionSessionId: string;
  items: ExecutionWorkItemRecord[];
};

export type ExpandExecutionPlanToWorkItemsRequest = {
  executionSessionId: string;
};

export type LeaseExecutionWorkItemsRequest = {
  executionSessionId: string;
  workerId: string;
  take: number;
  leaseSeconds: number;
};
