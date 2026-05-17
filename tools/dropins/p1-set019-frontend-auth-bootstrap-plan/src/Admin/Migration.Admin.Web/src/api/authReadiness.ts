import { apiGet } from './core/adminApiClient';
import { getFrontendAuthConfig, getFrontendAuthWarnings } from '../auth';

export type FrontendAuthReadinessDescriptor = {
  enabled: boolean;
  authorityConfigured: boolean;
  clientIdConfigured: boolean;
  audienceConfigured: boolean;
  scopesConfigured: boolean;
  redirectUri: string;
  postLogoutRedirectUri: string;
  warnings: string[];
};

export type CombinedAuthReadinessDescriptor = {
  frontend: FrontendAuthReadinessDescriptor;
  backend: {
    environmentName: string;
    authMode: string;
    authRequired: boolean;
    isConfigured: boolean;
    authorityConfigured?: string | null;
    audienceConfigured?: string | null;
    clientIdConfigured?: string | null;
    tenantIdConfigured?: string | null;
    warnings: string[];
  };
  warnings: string[];
};

export function getFrontendAuthReadiness(): FrontendAuthReadinessDescriptor {
  const config = getFrontendAuthConfig();

  return {
    enabled: config.enabled,
    authorityConfigured: Boolean(config.authority),
    clientIdConfigured: Boolean(config.clientId),
    audienceConfigured: Boolean(config.audience),
    scopesConfigured: config.scopes.length > 0,
    redirectUri: config.redirectUri,
    postLogoutRedirectUri: config.postLogoutRedirectUri,
    warnings: getFrontendAuthWarnings(config)
  };
}

export async function getCombinedAuthReadiness(): Promise<CombinedAuthReadinessDescriptor> {
  const frontend = getFrontendAuthReadiness();
  const backend = await apiGet<CombinedAuthReadinessDescriptor['backend']>(
    '/api/cloud/auth/configuration'
  );

  return {
    frontend,
    backend,
    warnings: [
      ...frontend.warnings,
      ...backend.warnings
    ]
  };
}
