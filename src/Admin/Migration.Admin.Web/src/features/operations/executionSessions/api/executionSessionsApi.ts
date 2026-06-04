import { apiGet, apiPost } from "../../../../api/core/adminApiClient";
import type {
  CreateExecutionSessionRequest,
  ExecutionSessionRecord,
  RecentExecutionSessionsResponse,
} from "../types/executionSessions";

export const executionSessionsApi = {
  recent: (take = 25) =>
    apiGet<RecentExecutionSessionsResponse>(
      `/api/operational/execution-sessions/recent?take=${encodeURIComponent(String(take))}`,
    ),

  create: (body: CreateExecutionSessionRequest) =>
    apiPost<ExecutionSessionRecord>("/api/operational/execution-sessions", body),

  recordSnapshot: (executionSessionId: string, migrationRunId?: string | null) =>
    apiPost<void>("/api/operational/events/snapshot", {
      executionSessionId,
      migrationRunId: migrationRunId ?? null,
    }),
};
