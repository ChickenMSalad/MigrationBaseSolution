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

function resolveProjectId(projectOrProjectId: ProjectRecord | string | null | undefined): string {
  const projectId = typeof projectOrProjectId === "string"
    ? projectOrProjectId
    : projectOrProjectId?.projectId;

  if (!projectId || projectId === "undefined") {
    throw new Error("Project is not loaded yet; cannot save project credentials.");
  }

  return projectId;
}

export type ManifestBuilderSource = {
  sourceType?: string;
  type?: string;
  displayName?: string;
  description?: string;
  options?: unknown[];
};

export type BuildManifestRequest = {
  sourceType: string;
  credentialSetId: string;
  projectId?: string | null;
  fileName?: string | null;
  options?: Record<string, unknown>;
};

export type BuildManifestResponse = {
  artifact?: ArtifactRecord;
  artifactId?: string;
  fileName?: string;
  rowCount?: number;
  message?: string;
};

export const api = {
  health: () => request<{ status: string; service: string; utc: string }>("/health"),

  connectors: () => request<ConnectorsResponse>("/api/connectors"),

  projects: () => request<ProjectRecord[]>("/api/projects"),
  project: (projectId: string) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`),
  createProject: (payload: CreateProjectRequest) => request<ProjectRecord>("/api/projects", {
    method: "POST",
    body: JSON.stringify(payload)
  }),
  updateProject: (projectId: string, payload: Partial<ProjectRecord>) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`, {
    method: "PUT",
    body: JSON.stringify(payload)
  }),
  deleteProject: (projectId: string) => request<void>(`/api/projects/${encodeURIComponent(projectId)}`, {
    method: "DELETE"
  }),
  bindProjectArtifacts: (projectId: string, payload: BindProjectArtifactsRequest) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/artifacts`, {
    method: "PUT",
    body: JSON.stringify(payload)
  }),
  bindProjectCredentials: (projectOrProjectId: ProjectRecord | string, payload: BindProjectCredentialsRequest) => {
    const projectId = resolveProjectId(projectOrProjectId);
    return request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/credentials`, {
      method: "PUT",
      body: JSON.stringify(payload)
    });
  },

  artifacts: (kind?: string) => request<ArtifactRecord[]>(`/api/artifacts${queryString({ kind })}`),
  artifactDownloadUrl: (artifactId: string) => `/api/artifacts/${encodeURIComponent(artifactId)}`,
  deleteArtifact: (artifactId: string) => request<void>(`/api/artifacts/${encodeURIComponent(artifactId)}`, {
    method: "DELETE"
  }),
  uploadArtifact: (kind: string, file: File) => {
    const form = new FormData();
    form.append("kind", kind);
    form.append("artifactType", kind);
    form.append("file", file);

    return request<ArtifactRecord>("/api/artifacts", {
      method: "POST",
      body: form
    });
  },

  manifestBuilderSources: () => request<ManifestBuilderSource[]>("/api/manifest-builder/sources"),
  buildManifest: (payload: BuildManifestRequest) => request<BuildManifestResponse>("/api/manifest-builder/build", {
    method: "POST",
    body: JSON.stringify(payload)
  }),
  createManifestArtifact: (payload: BuildManifestRequest) => request<BuildManifestResponse>("/api/manifest-builder/build", {
    method: "POST",
    body: JSON.stringify(payload)
  }),

  runs: () => request<RunRecord[]>("/api/runs"),
  run: (runId: string) => request<RunRecord>(`/api/runs/${encodeURIComponent(runId)}`),
  createRun: (projectId: string, payload: CreateRunRequest) => request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/runs`, {
    method: "POST",
    body: JSON.stringify(payload)
  }),
  queuePreflight: (projectId: string) => request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/preflight`, {
    method: "POST"
  }),
  runSummary: (runId: string) => request<RunSummary>(`/api/runs/${encodeURIComponent(runId)}/summary`),
  runEvents: (runId: string, take = 250) => request<RunEventsResponse>(`/api/runs/${encodeURIComponent(runId)}/events${queryString({ take })}`),
  runFailures: (runId: string) => request<RunFailuresResponse>(`/api/runs/${encodeURIComponent(runId)}/failures`),
  runWorkItems: (runId: string) => request<RunWorkItemsResponse>(`/api/runs/${encodeURIComponent(runId)}/work-items`),

  credentials: () => request<CredentialSetSummary[]>("/api/credentials"),
  createCredential: (payload: CreateCredentialSetRequest) => request<CredentialSetSummary>("/api/credentials", {
    method: "POST",
    body: JSON.stringify(payload)
  }),
  testCredential: (credentialSetId: string) => request<CredentialTestResult>(`/api/credentials/${encodeURIComponent(credentialSetId)}/test`, {
    method: "POST"
  }),
  deleteCredential: (credentialSetId: string) => request<void>(`/api/credentials/${encodeURIComponent(credentialSetId)}`, {
    method: "DELETE"
  })
};
