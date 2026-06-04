import { apiDelete } from './core/adminApiClient';

export function deleteProject(projectId: string, includeRuns = false) {
  return apiDelete(`/api/projects/${encodeURIComponent(projectId)}?includeRuns=${includeRuns}`);
}

export function deleteRun(runId: string) {
  return apiDelete(`/api/runs/${encodeURIComponent(runId)}`);
}

export function deleteCredential(credentialId: string) {
  return apiDelete(`/api/credentials/${encodeURIComponent(credentialId)}`);
}

export function deleteArtifact(artifactId: string) {
  return apiDelete(`/api/artifacts/${encodeURIComponent(artifactId)}`);
}

export function deleteConnector(connectorType: string) {
  return apiDelete(`/api/connectors/${encodeURIComponent(connectorType)}`);
}
