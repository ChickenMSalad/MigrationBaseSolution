export type RuntimeDashboardSummary = {
  runCount: number;
  workItemCount: number;
  queuedWorkItemCount: number;
  dispatchedWorkItemCount: number;
  completedWorkItemCount: number;
  failedWorkItemCount: number;
};

export type RuntimeDashboardRun = {
  runId: string;
  runKey?: string | null;
  runName?: string | null;
  sourceSystem?: string | null;
  targetSystem?: string | null;
  status?: string | null;
  environmentName?: string | null;
  isDryRun?: boolean;
  requestedAtUtc?: string | null;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
  workItemCount: number;
  queuedWorkItemCount: number;
  dispatchedWorkItemCount: number;
  completedWorkItemCount: number;
  failedWorkItemCount: number;
};

export type RuntimeDashboardWorkItem = {
  workItemId: number;
  runId: string;
  workType?: string | null;
  status?: string | null;
  attemptCount?: number;
  claimedBy?: string | null;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  lastErrorMessage?: string | null;
};

export type RuntimeDashboardFailure = {
  failureId?: string | null;
  runId?: string | null;
  workItemId?: number | null;
  manifestRowId?: number | null;
  failureType?: string | null;
  message?: string | null;
  createdAtUtc?: string | null;
};

export type RuntimeDashboardRunDetail = {
  run: RuntimeDashboardRun | null;
  workItems: RuntimeDashboardWorkItem[];
  failures: RuntimeDashboardFailure[];
};
