import { apiGet } from './core/adminApiClient';

export type TelemetryCorrelationDescriptor = {
  environmentName: string;
  correlationId: string;
  requestId: string;
  traceIdentifier: string;
  workspaceId?: string | null;
  tenantId?: string | null;
  telemetryMode: string;
  applicationInsightsConnectionConfigured?: string | null;
  recommendedHeaders: string[];
  recommendedLogProperties: string[];
  warnings: string[];
};

export async function getTelemetryCorrelation(): Promise<TelemetryCorrelationDescriptor> {
  return apiGet<TelemetryCorrelationDescriptor>('/api/cloud/telemetry/correlation');
}
