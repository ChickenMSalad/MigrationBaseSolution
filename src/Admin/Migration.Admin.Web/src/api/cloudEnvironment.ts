import { apiGet } from './core/adminApiClient';

export type CloudEnvironmentDescriptor = {
  environmentName: string;
  hostKind: string;
  storageMode: string;
  queueProvider: string;
  queueName?: string | null;
  credentialMode: string;
  artifactMode: string;
  controlPlaneStorageRoot?: string | null;
  isLocal: boolean;
  isCloudReady: boolean;
  warnings: string[];
};

export async function getCloudEnvironment(): Promise<CloudEnvironmentDescriptor> {
  return apiGet<CloudEnvironmentDescriptor>('/api/cloud/environment');
}
