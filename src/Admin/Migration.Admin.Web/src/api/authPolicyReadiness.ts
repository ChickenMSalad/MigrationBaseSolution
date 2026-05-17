import { apiGet } from './core/adminApiClient';

export type AuthPolicyRequirement = {
  policyName: string;
  scope: string;
  requiredInProduction: boolean;
  requiredInDevelopment: boolean;
  description: string;
};

export type AuthPolicyReadinessSnapshot = {
  generatedUtc: string;
  environmentName: string;
  requiresAuth: boolean;
  isDevelopment: boolean;
  isProductionLike: boolean;
  isReadyForProduction: boolean;
  requiredPolicies: AuthPolicyRequirement[];
  blockingIssues: string[];
  warnings: string[];
};

export async function getAuthPolicyReadiness(): Promise<AuthPolicyReadinessSnapshot> {
  return apiGet<AuthPolicyReadinessSnapshot>('/api/cloud/auth/policy-readiness');
}
