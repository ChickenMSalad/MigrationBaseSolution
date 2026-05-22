export type OperationalEventRecord = {
  operationalEventId: string;
  eventType: string;
  severity: string;
  category: string;
  source: string;
  message: string;
  payloadJson?: string | null;
  createdUtc: string;
  executionSessionId?: string | null;
  migrationRunId?: string | null;
};

export type OperationalEventQueryResponse = {
  skip: number;
  take: number;
  returned: number;
  events: OperationalEventRecord[];
};

export type OperationalEventQuery = {
  severity?: string;
  category?: string;
  eventType?: string;
  fromUtc?: string;
  toUtc?: string;
  executionSessionId?: string;
  migrationRunId?: string;
  skip?: number;
  take?: number;
};

export type OperationalEventAggregateBucket = {
  name: string;
  count: number;
};

export type OperationalEventAggregateSummary = {
  fromUtc: string | null;
  toUtc: string | null;
  totalEvents: number;
  bySeverity: OperationalEventAggregateBucket[];
  byCategory: OperationalEventAggregateBucket[];
  byEventType: OperationalEventAggregateBucket[];
};
