export type FrontendAuthConfig = {
  enabled: boolean;
  authority?: string;
  clientId?: string;
  audience?: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
  scopes: string[];
};

function readBoolean(value: string | undefined, fallback: boolean): boolean {
  if (value === undefined || value.trim() === '') {
    return fallback;
  }

  return value.toLocaleLowerCase() === 'true';
}

function readString(value: string | undefined): string | undefined {
  if (value === undefined || value.trim() === '') {
    return undefined;
  }

  return value.trim();
}

function readScopes(value: string | undefined): string[] {
  if (value === undefined || value.trim() === '') {
    return [];
  }

  return value
    .split(/[,\\s]+/)
    .map((scope) => scope.trim())
    .filter((scope) => scope.length > 0);
}

export function getFrontendAuthConfig(): FrontendAuthConfig {
  const enabled = readBoolean(import.meta.env.VITE_AUTH_ENABLED, false);
  const authority = readString(import.meta.env.VITE_AUTH_AUTHORITY);
  const clientId = readString(import.meta.env.VITE_AUTH_CLIENT_ID);
  const audience = readString(import.meta.env.VITE_AUTH_AUDIENCE);
  const redirectUri = readString(import.meta.env.VITE_AUTH_REDIRECT_URI) ?? window.location.origin;
  const postLogoutRedirectUri =
    readString(import.meta.env.VITE_AUTH_POST_LOGOUT_REDIRECT_URI) ?? window.location.origin;

  const scopes = readScopes(import.meta.env.VITE_AUTH_SCOPES);

  return {
    enabled,
    authority,
    clientId,
    audience,
    redirectUri,
    postLogoutRedirectUri,
    scopes
  };
}

export function getFrontendAuthWarnings(config: FrontendAuthConfig = getFrontendAuthConfig()): string[] {
  if (!config.enabled) {
    return [];
  }

  const warnings: string[] = [];

  if (!config.authority) warnings.push('VITE_AUTH_AUTHORITY is required when frontend auth is enabled.');
  if (!config.clientId) warnings.push('VITE_AUTH_CLIENT_ID is required when frontend auth is enabled.');
  if (!config.audience) warnings.push('VITE_AUTH_AUDIENCE is recommended when frontend auth is enabled.');
  if (config.scopes.length === 0) warnings.push('VITE_AUTH_SCOPES is recommended when frontend auth is enabled.');

  return warnings;
}
