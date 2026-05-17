import { apiGet } from './core/adminApiClient';

export type CloudCredentialProviderDescriptor = {
  providerKind: string;
  isConfigured: boolean;
  usesManagedIdentity: boolean;
  keyVaultUriConfigured?: string | null;
  secretNamePrefix: string;
  supportedSecretKinds: string[];
  warnings: string[];
};

export type CloudCredentialReference = {
  workspaceId: string;
  connectorRole: string;
  connectorKey: string;
  credentialSetId: string;
  secretKind: string;
  secretName: string;
};

export async function getCloudCredentialProvider(): Promise<CloudCredentialProviderDescriptor> {
  return apiGet<CloudCredentialProviderDescriptor>('/api/cloud/credentials/provider');
}

export async function resolveCloudCredentialSecretName(
  role = 'source',
  connector = 'aem',
  credentialSet = 'default',
  secretKind = 'password'
): Promise<CloudCredentialReference> {
  const params = new URLSearchParams({
    role,
    connector,
    credentialSet,
    secretKind
  });

  return apiGet<CloudCredentialReference>(`/api/cloud/credentials/secret-name?${params}`);
}
