import { apiGet } from './core/adminApiClient';
import type { CloudCredentialReference } from './cloudCredentials';

export type CloudCredentialSecretExistsResponse = {
  reference: CloudCredentialReference;
  exists: boolean;
  valueReturned: boolean;
};

export async function checkCloudCredentialSecretExists(
  role = 'source',
  connector = 'aem',
  credentialSet = 'default',
  secretKind = 'password'
): Promise<CloudCredentialSecretExistsResponse> {
  const params = new URLSearchParams({
    role,
    connector,
    credentialSet,
    secretKind
  });

  return apiGet<CloudCredentialSecretExistsResponse>(`/api/cloud/credentials/secret-exists?${params}`);
}
