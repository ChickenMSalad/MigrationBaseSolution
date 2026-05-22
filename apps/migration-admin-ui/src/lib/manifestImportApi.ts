import { adminApiBaseUrl } from './adminApi';

export interface ManifestImportPreview {
  fileName: string;
  contentType: string;
  sizeBytes: number;
  projectId: string;
  mappingProfileId?: string;
  importMode: 'validate-only' | 'import';
}

export interface ManifestImportResult {
  importId?: string;
  projectId?: string;
  status: string;
  acceptedRows?: number;
  rejectedRows?: number;
  message?: string;
  diagnostics?: unknown;
}

export async function importManifestFile(
  request: ManifestImportPreview,
  file: File,
  signal?: AbortSignal
): Promise<ManifestImportResult> {
  const formData = new FormData();
  formData.append('file', file, file.name);
  formData.append('projectId', request.projectId);
  formData.append('importMode', request.importMode);

  if (request.mappingProfileId) {
    formData.append('mappingProfileId', request.mappingProfileId);
  }

  const response = await fetch(`${adminApiBaseUrl}/api/operational/manifests/import`, {
    method: 'POST',
    body: formData,
    signal
  });

  const text = await response.text();
  const parsed = text ? (JSON.parse(text) as ManifestImportResult) : { status: 'accepted' };

  if (!response.ok) {
    throw new Error(parsed.message || `${response.status} ${response.statusText}`);
  }

  return parsed;
}
