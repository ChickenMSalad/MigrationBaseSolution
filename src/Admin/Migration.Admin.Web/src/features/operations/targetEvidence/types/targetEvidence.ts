export type TargetExecutionEvidenceRow = {
  workItemId: string;
  status: string;
  originId?: string | null;
  id?: string | null;
  targetAssetId?: string | null;
  fileName?: string | null;
  message?: string | null;
  error?: string | null;
  startedUtc?: string | null;
  completedUtc?: string | null;
  updatedUtc?: string | null;
  stampedFields: Record<string, string | null | undefined>;
  targetPayloadFields: Record<string, string | null | undefined>;
  properties: Record<string, string | null | undefined>;
  warnings: string[];
};

export type TargetExecutionEvidenceResponse = {
  runId: string;
  jobName: string;
  totalCount: number;
  successCount: number;
  failedCount: number;
  retryCount: number;
  rows: TargetExecutionEvidenceRow[];
};
