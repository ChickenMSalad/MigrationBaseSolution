import { apiGet, apiPost } from './core/adminApiClient';
import type { TelemetryWriteResult } from './telemetrySink';

export type CloudOperationTelemetryEventNames = {
  category: string;
  eventNames: string[];
};

export type CloudOperationTelemetryProbe = {
  workspaceId: string;
  eventCount: number;
  results: TelemetryWriteResult[];
};

export async function getCloudOperationTelemetryEventNames(): Promise<CloudOperationTelemetryEventNames> {
  return apiGet<CloudOperationTelemetryEventNames>('/api/cloud/telemetry/operation/event-names');
}

export async function probeCloudOperationTelemetry(): Promise<CloudOperationTelemetryProbe> {
  return apiPost<CloudOperationTelemetryProbe>('/api/cloud/telemetry/operation/probe', {});
}
