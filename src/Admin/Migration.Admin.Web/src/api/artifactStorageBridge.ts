import { apiDelete, apiGet, apiPost } from './core/adminApiClient';
import type { ArtifactManifestIndex } from './artifactManifestIndex';
import type { ArtifactStorageDescriptor } from './artifactStorage';

export type ArtifactStorageBridgeUploadResponse = {
  artifact: ArtifactStorageDescriptor;
  index: ArtifactManifestIndex;
};

export type ArtifactStorageBridgeDeleteResponse = {
  deleted: boolean;
  artifactKind: string;
  artifactId: string;
  fileName: string;
  workspaceId: string;
};

export async function uploadArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string,
  content: string | Blob,
  contentType = 'application/octet-stream'
): Promise<ArtifactStorageBridgeUploadResponse> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;

  return apiPost<string | Blob, ArtifactStorageBridgeUploadResponse>(path, content, {
    headers: {
      'Content-Type': contentType
    }
  });
}

export async function downloadArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string
): Promise<Blob> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;
  const response = await fetch(path, { method: 'GET' });

  if (!response.ok) {
    throw new Error(`Artifact download failed: ${response.status} ${response.statusText}`);
  }

  return response.blob();
}

export async function deleteArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string
): Promise<ArtifactStorageBridgeDeleteResponse> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;
  return apiDelete<ArtifactStorageBridgeDeleteResponse>(path);
}