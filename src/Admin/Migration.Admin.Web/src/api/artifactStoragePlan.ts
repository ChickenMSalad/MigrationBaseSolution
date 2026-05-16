import { apiGet } from './core/adminApiClient';

export type ArtifactStoragePlanDescriptor = {
  environmentName: string;
  workspaceId: string;
  artifactMode: string;
  providerKind: string;
  artifactRoot: string;
  manifestRoot: string;
  mappingRoot: string;
  taxonomyRoot: string;
  otherRoot: string;
  blobContainerName?: string | null;
  blobAccountName?: string | null;
  usesLocalFileSystem: boolean;
  usesAzureBlob: boolean;
  requiresManagedIdentity: boolean;
  supportedArtifactKinds: string[];
  warnings: string[];
};

export async function getArtifactStoragePlan(): Promise<ArtifactStoragePlanDescriptor> {
  return apiGet<ArtifactStoragePlanDescriptor>('/api/cloud/artifact-storage-plan');
}
