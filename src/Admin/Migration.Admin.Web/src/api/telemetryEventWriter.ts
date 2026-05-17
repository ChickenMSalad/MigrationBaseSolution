import { apiPost } from './core/adminApiClient';
import type { TelemetryWriteResult } from './telemetrySink';

export type TelemetryEventWriteRequest = {
  workspaceId: string;
  eventName: string;
  category: string;
  severity: string;
  tenantId?: string | null;
  projectId?: string | null;
  runId?: string | null;
  correlationId?: string | null;
  dimensions?: Record<string, string> | null;
  metrics?: Record<string, number> | null;
};

export type TelemetryEventWriterProbe = {
  request: TelemetryEventWriteRequest;
  result: TelemetryWriteResult;
};

export async function probeTelemetryEventWriter(): Promise<TelemetryEventWriterProbe> {
  return apiPost<TelemetryEventWriterProbe>('/api/cloud/telemetry/writer/probe', {});
}
