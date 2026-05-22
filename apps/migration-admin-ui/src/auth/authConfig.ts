export type OperatorAuthMode = 'disabled' | 'entra-id';

export interface OperatorAuthConfig {
  mode: OperatorAuthMode;
  tenantId?: string;
  clientId?: string;
  authority?: string;
  apiScope?: string;
  redirectUri: string;
}

function readEnv(name: string): string | undefined {
  const value = import.meta.env[name] as string | undefined;
  return value && value.trim().length > 0 ? value.trim() : undefined;
}

const tenantId = readEnv('VITE_ENTRA_TENANT_ID');
const clientId = readEnv('VITE_ENTRA_CLIENT_ID');
const authority = readEnv('VITE_ENTRA_AUTHORITY') ?? (tenantId ? `https://login.microsoftonline.com/${tenantId}` : undefined);
const apiScope = readEnv('VITE_ADMIN_API_SCOPE');

export const operatorAuthConfig: OperatorAuthConfig = {
  mode: tenantId && clientId ? 'entra-id' : 'disabled',
  tenantId,
  clientId,
  authority,
  apiScope,
  redirectUri: readEnv('VITE_AUTH_REDIRECT_URI') ?? window.location.origin,
};

export function isOperatorAuthConfigured(): boolean {
  return operatorAuthConfig.mode === 'entra-id' && Boolean(operatorAuthConfig.authority && operatorAuthConfig.clientId);
}
