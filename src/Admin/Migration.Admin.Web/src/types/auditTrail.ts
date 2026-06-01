export type AuditTrailSummary = {
  totalEvents: number;
  securityEvents: number;
  runtimeEvents: number;
  configurationEvents: number;
  lastEventUtc: string | null;
  status: string;
};

export type AuditTrailEvent = {
  eventId: string;
  occurredUtc: string;
  category: string;
  action: string;
  actor: string;
  resourceType: string;
  resourceId: string;
  outcome: string;
  message?: string | null;
};

export type AuditTrailRecentResponse = {
  take: number;
  events: AuditTrailEvent[];
};
