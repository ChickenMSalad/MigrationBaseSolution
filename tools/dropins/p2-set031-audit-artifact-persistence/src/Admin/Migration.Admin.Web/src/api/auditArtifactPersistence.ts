import { apiGet } from './core/adminApiClient';

export type AuditArtifactPersistenceConfiguration = {
  provider: string;
  artifactKind: string;
  artifactId: string;
  fileNamePrefix: string;
  recentQueryLimit: string;
};

export async function getAuditArtifactPersistenceConfiguration():
  Promise<AuditArtifactPersistenceConfiguration> {
  return apiGet<AuditArtifactPersistenceConfiguration>(
    '/api/cloud/audit/artifact-persistence/configuration'
  );
}
