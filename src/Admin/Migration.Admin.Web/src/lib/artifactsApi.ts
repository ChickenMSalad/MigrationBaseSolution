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
  const response = await fetch(`/api/artifacts?${params.toString()}`);
  if (!response.ok) throw new Error(`Failed to list artifacts (${response.status})`);
  return response.json();
}

export async function uploadArtifact(file: File, kind: ArtifactKind, projectId?: string, description?: string): Promise<ControlPlaneArtifactRecord> {
  const form = new FormData();
  form.append('file', file);
  form.append('kind', kind);
  if (projectId) form.append('projectId', projectId);
  if (description) form.append('description', description);

  const response = await fetch('/api/artifacts', {
    method: 'POST',
    body: form,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Failed to upload artifact (${response.status})`);
  }

  return response.json();
}

export async function getManifestPreview(artifactId: string, take = 10): Promise<ManifestPreview> {
  const response = await fetch(`/api/artifacts/${artifactId}/manifest-preview?take=${take}`);
  if (!response.ok) throw new Error(`Failed to load manifest preview (${response.status})`);
  return response.json();
}

export async function deleteArtifact(artifactId: string): Promise<void> {
  const response = await fetch(`/api/artifacts/${artifactId}`, { method: 'DELETE' });
  if (!response.ok && response.status !== 404) throw new Error(`Failed to delete artifact (${response.status})`);
}
