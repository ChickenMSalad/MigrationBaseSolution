import { apiGet, apiPost } from './core/adminApiClient';
import type { ArtifactStorageDescriptor } from './artifactStorage';

export type ArtifactManifestEntry = {
  artifactId: string;
  artifactKind: string;
  fileName: string;
  objectKey: string;
  uri: string;
  contentType: string;
  createdUtc: string;
};

export type ArtifactManifestIndex = {
  workspaceId: string;
  schemaVersion: string;
  updatedUtc: string;
  artifacts: ArtifactManifestEntry[];
};

export type ArtifactManifestIndexProbeResponse = {
  artifact: ArtifactStorageDescriptor;
  index: ArtifactManifestIndex;
};

export async function getArtifactManifestIndex(): Promise<ArtifactManifestIndex> {
  return apiGet<ArtifactManifestIndex>('/api/cloud/artifacts/index');
}

export async function probeArtifactManifestIndex(): Promise<ArtifactManifestIndexProbeResponse> {
  return apiPost<Record<string, never>, ArtifactManifestIndexProbeResponse>(
    '/api/cloud/artifacts/index/probe',
    {}
  );
}