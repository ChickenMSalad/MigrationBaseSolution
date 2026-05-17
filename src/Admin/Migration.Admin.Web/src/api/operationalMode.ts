import { apiGet } from './core/adminApiClient';

export type OperationalModeSnapshot = {
  generatedUtc: string;
  environmentName: string;
  mode: string;
  isLocalDevelopment: boolean;
  isDiagnosticsOnly: boolean;
  isProductionReady: boolean;
  isLiveQueueExecutionAllowed: boolean;
  capabilities: string[];
  disabledCapabilities: string[];
  warnings: string[];
};

export async function getOperationalMode(): Promise<OperationalModeSnapshot> {
  return apiGet<OperationalModeSnapshot>('/api/cloud/operations/mode');
}
