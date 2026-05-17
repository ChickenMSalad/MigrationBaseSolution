import { apiGet } from './core/adminApiClient';

export type AuthEnforcementDiagnostic = {
  area: string;
  recommendedPolicy: string;
  enforcementEnabled: boolean;
  productionBlocking: boolean;
  notes: string;
};

export type AuthEnforcementDiagnosticsSnapshot = {
  generatedUtc: string;
  globalAuthRequired: boolean;
  productionModeEnabled: boolean;
  diagnostics: AuthEnforcementDiagnostic[];
  warnings: string[];
};

export async function getAuthEnforcementDiagnostics(): Promise<AuthEnforcementDiagnosticsSnapshot> {
  return apiGet<AuthEnforcementDiagnosticsSnapshot>('/api/cloud/auth/enforcement-diagnostics');
}
