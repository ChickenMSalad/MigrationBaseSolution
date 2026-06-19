import { apiDelete, apiGet } from "../../../../api/core/adminApiClient";
import type {
  RuntimeDashboardRun,
  RuntimeDashboardRunDetail,
  RuntimeDashboardSummary
} from "../types/runtimeDashboard";

function queryString(params: Record<string, unknown>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const text = search.toString();
  return text ? `?${text}` : "";
}

export const runtimeDashboardApi = {
  summary: () => apiGet<RuntimeDashboardSummary>("/api/runtime/dashboard/summary"),
  runs: (take = 50) => apiGet<RuntimeDashboardRun[]>(`/api/runtime/dashboard/runs${queryString({ take })}`),
  runDetail: (runId: string) => apiGet<RuntimeDashboardRunDetail>(`/api/runtime/dashboard/runs/${encodeURIComponent(runId)}`),
  deleteRun: (runId: string) => apiDelete<void>(`/api/runs/${encodeURIComponent(runId)}`)
};
