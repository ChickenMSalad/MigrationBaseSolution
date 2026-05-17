import { apiGet } from './core/adminApiClient';
import type { AuthPolicyReadinessSnapshot } from './authPolicyReadiness';
import type { CredentialAccessPolicyReadinessSnapshot } from './credentialAccessPolicyReadiness';
import type { OperationalReadinessSnapshot } from './operationalReadiness';

export type ProductionSafetyGate = {
  name: string;
  passed: boolean;
  requiredForProduction: boolean;
  description: string;
  issues: string[];
};

export type ProductionSafetyGateSnapshot = {
  generatedUtc: string;
  isProductionReady: boolean;
  isLiveQueueExecutionAllowed: boolean;
  gates: ProductionSafetyGate[];
  blockingIssues: string[];
  warnings: string[];
  authPolicy: AuthPolicyReadinessSnapshot;
  credentialAccess: CredentialAccessPolicyReadinessSnapshot;
  operationalReadiness: OperationalReadinessSnapshot;
};

export async function getProductionSafetyGates(): Promise<ProductionSafetyGateSnapshot> {
  return apiGet<ProductionSafetyGateSnapshot>('/api/cloud/operations/production-safety-gates');
}
