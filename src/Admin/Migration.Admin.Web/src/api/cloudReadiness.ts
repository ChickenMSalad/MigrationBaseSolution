import { apiGet } from './core/adminApiClient';

export type CloudReadinessCheckDescriptor = {
  name: string;
  status: string;
  warnings: string[];
};

export type CloudReadinessSummaryDescriptor = {
  environmentName: string;
  isDevelopment: boolean;
  isCloudReady: boolean;
  warningCount: number;
  checks: CloudReadinessCheckDescriptor[];
  warnings: string[];
};

export async function getCloudReadiness(): Promise<CloudReadinessSummaryDescriptor> {
  return apiGet<CloudReadinessSummaryDescriptor>('/api/cloud/readiness');
}
