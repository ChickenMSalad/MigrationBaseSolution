import { apiPost } from "../../../../api/core/adminApiClient";

export type RunProjectPreflightRequest = {
  jobName?: string | null;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  settings?: Record<string, string | null | undefined>;
};

export type PreflightIssue = {
  severity?: string;
  code?: string;
  rowId?: string | number | null;
  field?: string | null;
  message?: string;
};

export type PreflightResult = {
  status: string;
  summary: {
    totalRows: number;
    checkedRows: number;
    errorCount: number;
    warningCount: number;
    [key: string]: unknown;
  };
  issues?: PreflightIssue[];
  [key: string]: unknown;
};

export function runProjectPreflight(
  projectId: string,
  payload: RunProjectPreflightRequest
): Promise<PreflightResult> {
  return apiPost<PreflightResult>(`/api/projects/${encodeURIComponent(projectId)}/preflight/run`, payload);
}
