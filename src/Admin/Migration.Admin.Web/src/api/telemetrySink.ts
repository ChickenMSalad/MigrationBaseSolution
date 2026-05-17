import { apiGet, apiPost } from './core/adminApiClient';

export type TelemetryEvent = {
  eventId: string;
  workspaceId: string;
  tenantId?: string | null;
  eventName: string;
  category: string;
  severity: string;
  correlationId: string;
  projectId?: string | null;
  runId?: string | null;
  timestampUtc: string;
  dimensions: Record<string, string>;
  metrics: Record<string, number>;
};

export type TelemetryProviderDescriptor = {
  providerKind: string;
  isConfigured: boolean;
  isDurable: boolean;
  supportsMetrics: boolean;
  supportsTraces: boolean;
  supportsCorrelation: boolean;
  warnings: string[];
};

export type TelemetryWriteResult = {
  accepted: boolean;
  providerKind: string;
  eventId: string;
  writtenUtc: string;
};

export type TelemetryProbe = {
  telemetryEvent: TelemetryEvent;
  result: TelemetryWriteResult;
};

export type RecentTelemetryResponse = {
  workspaceId: string;
  count: number;
  events: TelemetryEvent[];
};

export async function getTelemetryProvider(): Promise<TelemetryProviderDescriptor> {
  return apiGet<TelemetryProviderDescriptor>('/api/cloud/telemetry/provider');
}

export async function probeTelemetry(): Promise<TelemetryProbe> {
  return apiPost<TelemetryProbe>('/api/cloud/telemetry/probe', {});
}

export async function getRecentTelemetry(take = 25): Promise<RecentTelemetryResponse> {
  return apiGet<RecentTelemetryResponse>(`/api/cloud/telemetry/recent?take=${take}`);
}
