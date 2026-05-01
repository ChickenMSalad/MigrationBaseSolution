import type {
  ArtifactRecord,
  BindProjectArtifactsRequest,
  BindProjectCredentialsRequest,
  ConnectorsResponse,
  ConnectorDescriptor,
  CreateCredentialSetRequest,
  CreateProjectRequest,
  CreateRunRequest,
  CredentialSetSummary,
  CredentialTestResult,
  ProjectRecord,
  RunEventsResponse,
  RunFailuresResponse,
  RunRecord,
  RunSummary,
  RunWorkItemsResponse
} from "../types/api";

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
        // keep default message
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function connectorValue(connector: ConnectorDescriptor | null | undefined) {
  return String(connector?.type ?? connector?.name ?? connector?.displayName ?? "").trim();
}

export function displayConnectorName(connector: ConnectorDescriptor | null | undefined) {
  return String(connector?.displayName ?? connector?.name ?? connector?.type ?? "Unnamed connector").trim();
}

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

export const api = {
  health: () => request<{ status: string; service: string; utc: string }>("/health"),

  connectors: () => request<ConnectorsResponse>("/api/connectors"),

  projects: () => request<ProjectRecord[]>("/api/projects"),

  project: (projectId: string) =>
    request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`),

  createProject: (payload: CreateProjectRequest) =>
    request<ProjectRecord>("/api/projects", {
      method: "POST",
      body: JSON.stringify(payload)
    }),

  updateProject: (project: ProjectRecord) =>
    request<ProjectRecord>("/api/projects", {
      method: "POST",
      body: JSON.stringify(project)
    }),

  bindProjectArtifacts: (projectId: string, payload: BindProjectArtifactsRequest) =>
    request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/artifacts`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }),

  bindProjectCredentials: async (project: ProjectRecord, payload: BindProjectCredentialsRequest) => {
    // No new backend store/pattern required: the existing project Settings bag already persists
    // project context. The top-level values are included for forward compatibility; current
    // backends that do not have those properties will ignore them and keep the settings values.
    const settings = {
      ...(project.settings ?? {}),
      sourceCredentialSetId: payload.sourceCredentialSetId ?? null,
      targetCredentialSetId: payload.targetCredentialSetId ?? null
    } satisfies Record<string, string | null>;

    return request<ProjectRecord>("/api/projects", {
      method: "POST",
      body: JSON.stringify({
        ...project,
        sourceCredentialSetId: payload.sourceCredentialSetId ?? null,
        targetCredentialSetId: payload.targetCredentialSetId ?? null,
        settings
      })
    });
  },

  artifacts: (kind?: string) =>
    request<ArtifactRecord[]>(`/api/artifacts${queryString({ kind })}`),

  runs: () => request<RunRecord[]>("/api/runs"),

  run: (runId: string) =>
    request<RunRecord>(`/api/runs/${encodeURIComponent(runId)}`),

  createRun: (projectId: string, payload: CreateRunRequest) =>
    request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/runs`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),

  queuePreflight: (projectId: string) =>
    request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/preflight`, {
      method: "POST"
    }),

  runSummary: (runId: string) =>
    request<RunSummary>(`/api/runs/${encodeURIComponent(runId)}/summary`),

  runEvents: (runId: string, take = 250) =>
    request<RunEventsResponse>(`/api/runs/${encodeURIComponent(runId)}/events${queryString({ take })}`),

  runFailures: (runId: string) =>
    request<RunFailuresResponse>(`/api/runs/${encodeURIComponent(runId)}/failures`),

  runWorkItems: (runId: string) =>
    request<RunWorkItemsResponse>(`/api/runs/${encodeURIComponent(runId)}/work-items`),

  credentials: () => request<CredentialSetSummary[]>("/api/credentials"),

  createCredential: (payload: CreateCredentialSetRequest) =>
    request<CredentialSetSummary>("/api/credentials", {
      method: "POST",
      body: JSON.stringify(payload)
    }),

  testCredential: (credentialSetId: string) =>
    request<CredentialTestResult>(`/api/credentials/${encodeURIComponent(credentialSetId)}/test`, {
      method: "POST"
    }),

  deleteCredential: (credentialSetId: string) =>
    request<void>(`/api/credentials/${encodeURIComponent(credentialSetId)}`, {
      method: "DELETE"
    })
};
