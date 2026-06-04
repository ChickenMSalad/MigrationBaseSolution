import { apiDelete, apiGet, apiPost } from '../api/core/adminApiClient';

export type ArtifactKind = 'Unknown' | 'Manifest' | 'Mapping' | 'Taxonomy' | 'Binary' | 'Report' | 'Other';

export interface ControlPlaneArtifactRecord {
  artifactId: string;
  kind: ArtifactKind;
  fileName: string;
  contentType: string;
  length: number;
  relativePath: string;
  absolutePath: string;
  createdUtc: string;
  projectId?: string | null;
  description?: string | null;
  metadata: Record<string, string>;
}

export interface ManifestPreview {
  artifactId: string;
  fileName: string;
  columns: string[];
  sampleRows: Array<Record<string, string>>;
}

export async function listArtifacts(kind?: ArtifactKind, projectId?: string): Promise<ControlPlaneArtifactRecord[]> {
  const params = new URLSearchParams();
  if (kind) params.set('kind', kind);
  if (projectId) params.set('projectId', projectId);
  const query = params.toString();
  return apiGet<ControlPlaneArtifactRecord[]>(`/api/artifacts${query ? `?${query}` : ''}`);
}

export async function uploadArtifact(file: File, kind: ArtifactKind, projectId?: string, description?: string): Promise<ControlPlaneArtifactRecord> {
  const form = new FormData();
  form.append('file', file);
  form.append('kind', kind);
  if (projectId) form.append('projectId', projectId);
  if (description) form.append('description', description);

  return apiPost<ControlPlaneArtifactRecord>('/api/artifacts', form);
}

export async function getManifestPreview(artifactId: string, take = 10): Promise<ManifestPreview> {
  return apiGet<ManifestPreview>(`/api/artifacts/${encodeURIComponent(artifactId)}/manifest-preview?take=${encodeURIComponent(String(take))}`);
}

export async function deleteArtifact(artifactId: string): Promise<void> {
  await apiDelete(`/api/artifacts/${encodeURIComponent(artifactId)}`);
}
