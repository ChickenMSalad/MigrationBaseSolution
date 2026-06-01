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

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
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
        // keep default message
      }
    }

    throw new Error(message);
  }

  return (await response.json()) as T;
}

export function runProjectPreflight(
  projectId: string,
  payload: RunProjectPreflightRequest
): Promise<PreflightResult> {
  return request<PreflightResult>(`/api/projects/${encodeURIComponent(projectId)}/preflight/run`, {
    method: "POST",
    body: JSON.stringify(payload)
  });
}
