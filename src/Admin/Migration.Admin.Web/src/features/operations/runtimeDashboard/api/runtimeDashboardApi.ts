import type {
  RuntimeDashboardRun,
  RuntimeDashboardRunDetail,
  RuntimeDashboardSummary
} from "../types/runtimeDashboard";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: init?.body instanceof FormData
      ? init.headers
      : {
          "Content-Type": "application/json",
          ...(init?.headers ?? {})
        }
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;

    try {
      const body = await response.json();
      message = body?.error ?? body?.message ?? JSON.stringify(body);
    } catch {
      try {
        message = await response.text();
      } catch {
        // Keep the default status message.
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

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
  summary: () => request<RuntimeDashboardSummary>("/api/runtime/dashboard/summary"),
  runs: (take = 50) => request<RuntimeDashboardRun[]>(`/api/runtime/dashboard/runs${queryString({ take })}`),
  runDetail: (runId: string) => request<RuntimeDashboardRunDetail>(`/api/runtime/dashboard/runs/${encodeURIComponent(runId)}`)
};
