import { apiGet } from './core/adminApiClient';
import type { CloudBinaryStorageProviderCapabilities } from './cloudBinaryStorage';

export type AzureBlobStorageDiagnostics = {
  storageRoot: string;
  selectedProvider: string;
  activeProvider: string;
  azureBlobSelected: boolean;
  azureBlobConfigured: boolean;
  accountNameConfigured: boolean;
  serviceUriConfigured: boolean;
  connectionStringConfigured: boolean;
  containerName?: string | null;
  capabilities: CloudBinaryStorageProviderCapabilities;
  warnings: string[];
};

export async function getAzureBlobStorageDiagnostics(): Promise<AzureBlobStorageDiagnostics> {
  return apiGet<AzureBlobStorageDiagnostics>('/api/cloud/storage/azure-blob/diagnostics');
}
