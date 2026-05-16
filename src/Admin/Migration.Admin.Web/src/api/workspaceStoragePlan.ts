import { apiGet } from './core/adminApiClient';

export type WorkspaceStoragePlanDescriptor = {
  workspaceId: string;
  storageMode: string;
  controlPlaneRoot: string;
  workspaceRoot: string;
  projectsRoot: string;
  runsRoot: string;
  artifactsRoot: string;
  credentialsRoot: string;
  isLocalFileSystem: boolean;
  isCloudBlob: boolean;
  warnings: string[];
};

export async function getWorkspaceStoragePlan(): Promise<WorkspaceStoragePlanDescriptor> {
  return apiGet<WorkspaceStoragePlanDescriptor>('/api/workspace/storage-plan');
}
