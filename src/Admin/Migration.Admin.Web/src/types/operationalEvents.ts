export type OperationalEventSeverity = "Information" | "Warning" | "Error" | "Critical" | string;

export interface OperationalEventTimelineItem {
  eventId: string;
  runId?: string | null;
  workItemId?: number | null;
  eventType: string;
  severity: OperationalEventSeverity;
  message: string;
  source?: string | null;
  createdAtUtc: string;
}

export interface OperationalEventTimelineResponse {
  generatedAtUtc: string;
  events: OperationalEventTimelineItem[];
}
