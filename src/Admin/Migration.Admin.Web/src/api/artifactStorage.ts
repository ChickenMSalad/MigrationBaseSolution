import { apiGet, apiPost } from './core/adminApiClient';
import type { CloudStorageLocation } from './cloudStorageLocations';

export type ArtifactStorageDescriptor = {
  workspaceId: string;
  artifactKind: string;
  artifactId: string;
  fileName: string;
  objectKey: string;
  location: CloudStorageLocation;
  contentType?: string | null;
  length?: number | null;
  eTag?: string | null;
};

export type ArtifactStorageProbeResponse = {
  exists: boolean;
  artifact: ArtifactStorageDescriptor;
};

export async function resolveArtifactStorage(
  kind = 'manifest',
  artifactId = 'sample-artifact',
  fileName = 'sample.json'
): Promise<ArtifactStorageDescriptor> {
  const params = new URLSearchParams({
    kind,
    artifactId,
    fileName
  });

  return apiGet<ArtifactStorageDescriptor>(`/api/cloud/artifacts/resolve?${params}`);
}

export async function probeArtifactStorage(): Promise<ArtifactStorageProbeResponse> {
  return apiPost<ArtifactStorageProbeResponse, Record<string, never>>(
    '/api/cloud/artifacts/probe',
    {}
  );
}
