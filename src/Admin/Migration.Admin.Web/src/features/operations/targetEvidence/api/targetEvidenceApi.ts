import { apiGet } from "../../../../api/core/adminApiClient";
import type { TargetExecutionEvidenceResponse } from "../types/targetEvidence";

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
  getRunEvidence: (runId: string, status = "all", take = 500) =>
    apiGet<TargetExecutionEvidenceResponse>(`/api/runs/${encodeURIComponent(runId)}/target-evidence${queryString({ status, take })}`),
};
