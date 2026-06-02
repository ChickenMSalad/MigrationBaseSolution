import { apiGet } from "../../../../api/core/adminApiClient";
import type { FailureRetryResponse } from "../types/failureRetry";

function queryString(params: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const text = search.toString();
  return text ? `?${text}` : "";
}

export const failureRetryApi = {
  recent: (take = 50) =>
    apiGet<FailureRetryResponse>(
      `/api/runtime/dashboard/failures${queryString({ take })}`,
    ),
};
