import { apiGet } from './core/adminApiClient';

export type DeploymentProfileDescriptor = {
  environmentName: string;
  profileName: string;
  hostingModel: string;
  region: string;
  sku: string;
  usesManagedIdentity: boolean;
  requiresHttps: boolean;
  requiresAuth: boolean;
  requiresPrivateNetworking: boolean;
  enablesDiagnostics: boolean;
  enablesHealthProbes: boolean;
  requiredConfigurationKeys: string[];
  optionalConfigurationKeys: string[];
  warnings: string[];
};

export async function getDeploymentProfile(): Promise<DeploymentProfileDescriptor> {
  return apiGet<DeploymentProfileDescriptor>('/api/cloud/deployment-profile');
}
