import { apiGet } from './core/adminApiClient';

export type AuthenticationConfigurationDescriptor = {
  environmentName: string;
  authMode: string;
  authRequired: boolean;
  isConfigured: boolean;
  authorityConfigured?: string | null;
  audienceConfigured?: string | null;
  clientIdConfigured?: string | null;
  tenantIdConfigured?: string | null;
  requiredFrontendSettings: string[];
  requiredApiSettings: string[];
  recommendedTokenClaims: string[];
  warnings: string[];
};

export async function getAuthenticationConfiguration(): Promise<AuthenticationConfigurationDescriptor> {
  return apiGet<AuthenticationConfigurationDescriptor>('/api/cloud/auth/configuration');
}
