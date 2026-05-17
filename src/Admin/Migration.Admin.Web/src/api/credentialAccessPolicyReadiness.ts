import { apiGet } from './core/adminApiClient';

export type CredentialAccessPolicyRequirement = {
  operation: string;
  requiredPolicy: string;
  requiredScope: string;
  requiresAudit: boolean;
  requiresTelemetry: boolean;
  allowedInDevelopmentWithoutAuth: boolean;
  description: string;
};

export type CredentialAccessPolicyReadinessSnapshot = {
  generatedUtc: string;
  requiresAuth: boolean;
  isDevelopment: boolean;
  allowsLocalDevelopmentBypass: boolean;
  requiresDedicatedCredentialScope: boolean;
  requiresAuditForCredentialAccess: boolean;
  requiresTelemetryForCredentialAccess: boolean;
  isReadyForProduction: boolean;
  requirements: CredentialAccessPolicyRequirement[];
  blockingIssues: string[];
  warnings: string[];
};

export async function getCredentialAccessPolicyReadiness(): Promise<CredentialAccessPolicyReadinessSnapshot> {
  return apiGet<CredentialAccessPolicyReadinessSnapshot>('/api/cloud/auth/credential-access-policy');
}
