import { apiGet, apiPost } from './core/adminApiClient';
import type { TelemetryWriteResult } from './telemetrySink';
import type { QueueMessageEnvelope } from './queueContracts';
import type { QueueExecutionPlan } from './queueExecutionPlanner';

export type QueueTelemetryEventNames = {
  category: string;
  eventNames: string[];
};

export type QueueTelemetryProbe = {
  envelope: QueueMessageEnvelope;
  plan: QueueExecutionPlan;
  telemetryResults: TelemetryWriteResult[];
};

export async function getQueueTelemetryEventNames(): Promise<QueueTelemetryEventNames> {
  return apiGet<QueueTelemetryEventNames>('/api/cloud/queue/telemetry/event-names');
}

export async function probeQueueTelemetryEvents(): Promise<QueueTelemetryProbe> {
  return apiPost<unknown, QueueTelemetryProbe>('/api/cloud/queue/telemetry/probe', {});
}
