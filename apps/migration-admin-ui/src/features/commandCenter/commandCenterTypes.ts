export type CommandCenterSummary = {
  runtimeStatus: string;
  activeRuns: number;
  queueDepth: number;
  activeWorkers: number;
  criticalAlerts: number;
  slaSloBreaches: number;
  estimatedHoursRemaining: number;
  estimatedMonthlyCost: number;
  lastUpdatedUtc: string;
};
