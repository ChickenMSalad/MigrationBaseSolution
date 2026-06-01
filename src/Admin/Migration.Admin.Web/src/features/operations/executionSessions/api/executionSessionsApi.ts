import type {
  CreateExecutionSessionRequest,
  ExecutionSessionRecord,
  RecentExecutionSessionsResponse,
} from "../types/executionSessions";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: init?.body instanceof FormData
      ? init.headers
      : {
          "Content-Type": "application/json",
          ...(init?.headers ?? {}),
        },
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
        // Keep the status text fallback.
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const executionSessionsApi = {
  recent: (take = 25) =>
    request<RecentExecutionSessionsResponse>(
      `/api/operational/execution-sessions/recent?take=${encodeURIComponent(String(take))}`,
    ),

  create: (body: CreateExecutionSessionRequest) =>
    request<ExecutionSessionRecord>("/api/operational/execution-sessions", {
      method: "POST",
      body: JSON.stringify(body),
    }),

  recordSnapshot: (executionSessionId: string, migrationRunId?: string | null) =>
    request<void>("/api/operational/events/snapshot", {
      method: "POST",
      body: JSON.stringify({
        executionSessionId,
        migrationRunId: migrationRunId ?? null,
      }),
    }),
};
