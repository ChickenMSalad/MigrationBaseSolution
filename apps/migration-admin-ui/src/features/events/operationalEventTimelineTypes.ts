export type OperationalEventRecord = {
  operationalEventId: string;
  eventType: string;
  severity: string;
  category: string;
  source: string;
  message: string;
  payloadJson?: string | null;
  createdUtc: string;
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
  skip?: number;
  take?: number;
};
