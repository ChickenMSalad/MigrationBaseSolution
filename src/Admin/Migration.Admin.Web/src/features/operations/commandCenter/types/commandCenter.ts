export type CommandCenterRuntimeStatus = 'Healthy' | 'Warning' | 'Degraded' | 'Unknown' | string;

export interface CommandCenterSummary {
  generatedUtc?: string | null;
  runtimeStatus?: CommandCenterRuntimeStatus | null;
  activeRuns?: number;
  completedRunsToday?: number;
  failedRunsToday?: number;
  queuedWorkItems?: number;
  dispatchedWorkItems?: number;
  failedWorkItems?: number;
  retryPendingWorkItems?: number;
  activeWorkers?: number;
  staleWorkers?: number;
  pendingNotifications?: number;
  criticalAlerts?: number;
  recentEvents?: CommandCenterEvent[];
}

export interface CommandCenterEvent {
  eventId?: string | number | null;
  createdUtc?: string | null;
  severity?: string | null;
  category?: string | null;
  title?: string | null;
  message?: string | null;
  source?: string | null;
}

export interface CommandCenterHealthCheck {
  name: string;
  status: string;
  message?: string | null;
}

export interface CommandCenterHealthResponse {
  generatedUtc?: string | null;
  status?: CommandCenterRuntimeStatus | null;
  checks?: CommandCenterHealthCheck[];
}
