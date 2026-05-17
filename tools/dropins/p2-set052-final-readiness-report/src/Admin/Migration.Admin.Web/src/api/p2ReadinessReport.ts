import { apiGet } from './core/adminApiClient';

export type P2ReadinessReportSnapshot = {
  generatedUtc: string;
  overallStatus: string;
  isDiagnosticsReady: boolean;
  isProductionReady: boolean;
  isLiveQueueExecutionReady: boolean;
  operationalMode: string;
  completedCapabilityAreas: string[];
  remainingRecommendedAreas: string[];
  warnings: string[];
};

export async function getP2ReadinessReport(): Promise<P2ReadinessReportSnapshot> {
  return apiGet<P2ReadinessReportSnapshot>('/api/cloud/operations/p2-readiness-report');
}
