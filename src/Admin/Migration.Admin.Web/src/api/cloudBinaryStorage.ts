import { apiGet, apiPost } from './core/adminApiClient';
import type { CloudStorageLocation } from './cloudStorageLocations';

export type CloudBinaryStorageProviderCapabilities = {
  provider: string;
  supportsStreamingWrites: boolean;
  supportsMultipartUploads: boolean;
  supportsObjectTags: boolean;
  supportsLeases: boolean;
  supportsVersioning: boolean;
  supportsConditionalWrites: boolean;
  supportsSignedUrls: boolean;
};

export type CloudBinaryStorageProbeResponse = {
  workspaceId: string;
  exists: boolean;
  location: CloudStorageLocation;
};

export async function getCloudBinaryStorageProvider(): Promise<CloudBinaryStorageProviderCapabilities> {
  return apiGet<CloudBinaryStorageProviderCapabilities>('/api/cloud/storage/provider');
}

export async function probeCloudBinaryStorage(): Promise<CloudBinaryStorageProbeResponse> {
  return apiPost<Record<string, never>, CloudBinaryStorageProbeResponse>(
    '/api/cloud/storage/probe',
    {}
  );
}