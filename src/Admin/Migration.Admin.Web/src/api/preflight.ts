import { apiPost } from './core/adminApiClient';
import type { PreflightResult } from '../types/api';

export type RunProjectPreflightRequest = {
  jobName: string;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  settings?: Record<string, string>;
};

export async function runProjectPreflight(
  projectId: string,
  request: RunProjectPreflightRequest,
): Promise<PreflightResult> {
  return apiPost<RunProjectPreflightRequest, PreflightResult>(
    `/api/projects/${encodeURIComponent(projectId)}/preflight/run`,
    request,
  );
}
