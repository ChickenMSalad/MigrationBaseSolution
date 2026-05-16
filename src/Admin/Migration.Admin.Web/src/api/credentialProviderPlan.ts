import { apiGet } from './core/adminApiClient';

export type CredentialProviderPlanDescriptor = {
  environmentName: string;
  credentialMode: string;
  workspaceId: string;
  tenantId?: string | null;
  providerKind: string;
  usesLocalSecrets: boolean;
  usesUserSecrets: boolean;
  usesKeyVault: boolean;
  usesManagedIdentity: boolean;
  keyVaultName?: string | null;
  keyVaultUri?: string | null;
  secretNamePrefix: string;
  supportedSecretKinds: string[];
  warnings: string[];
};

export async function getCredentialProviderPlan(): Promise<CredentialProviderPlanDescriptor> {
  return apiGet<CredentialProviderPlanDescriptor>('/api/cloud/credential-provider-plan');
}
