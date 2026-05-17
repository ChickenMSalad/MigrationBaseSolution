import { apiGet } from './core/adminApiClient';

export type CloudStorageLocation = {
  provider: string;
  root: string;
  workspaceId: string;
  relativePath: string;
  uri: string;
};

export type CloudStorageLocationsResponse = {
  workspaceId: string;
  workspaceRoot: CloudStorageLocation;
  projectRoot: CloudStorageLocation;
  runRoot: CloudStorageLocation;
  manifestArtifactsRoot: CloudStorageLocation;
  mappingArtifactsRoot: CloudStorageLocation;
  taxonomyArtifactsRoot: CloudStorageLocation;
  auditRoot: CloudStorageLocation;
};

export async function getCloudStorageLocations(
  projectId = 'sample-project',
  runId = 'sample-run'
): Promise<CloudStorageLocationsResponse> {
  const params = new URLSearchParams({
    projectId,
    runId
  });

  return apiGet<CloudStorageLocationsResponse>(`/api/cloud/storage/locations?${params}`);
}
