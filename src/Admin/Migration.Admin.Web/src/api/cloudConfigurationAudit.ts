import { apiGet } from './core/adminApiClient';

export type CloudConfigurationKeyAuditDescriptor = {
  key: string;
  category: string;
  isConfigured: boolean;
  isRequiredForCloud: boolean;
  recommendation?: string | null;
};

export type CloudConfigurationAuditDescriptor = {
  environmentName: string;
  maturityLevel: string;
  configuredCount: number;
  missingCount: number;
  warningCount: number;
  keys: CloudConfigurationKeyAuditDescriptor[];
  warnings: string[];
};

export async function getCloudConfigurationAudit(): Promise<CloudConfigurationAuditDescriptor> {
  return apiGet<CloudConfigurationAuditDescriptor>('/api/cloud/configuration-audit');
}
