import { apiGet } from "../../../../api/core/adminApiClient";
import type { TargetExecutionEvidenceResponse } from "../types/targetEvidence";

const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? "").replace(/\/$/, "");

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

export const targetEvidenceApi = {
  getRunEvidence: (runId: string, status = "all", take = 500, skip = 0, search?: string) =>
    apiGet<TargetExecutionEvidenceResponse>(
      `/api/runs/${encodeURIComponent(runId)}/target-evidence${queryString({ status, take, skip, search })}`,
    ),

  exportUrl: (runId: string, status = "all", search?: string, take = 50000) =>
    `${API_BASE_URL}/api/runs/${encodeURIComponent(runId)}/target-evidence/export${queryString({ status, search, take })}`,
};
