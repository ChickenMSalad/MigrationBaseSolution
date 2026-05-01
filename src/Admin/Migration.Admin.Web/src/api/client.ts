import type {
  ArtifactRecord,
  BuildSourceManifestRequest,
  BuildSourceManifestResponse,
  ConnectorDescriptor,
  ConnectorsResponse,
  CreateCredentialSetRequest,
  CreateProjectRequest,
  CreateRunRequest,
  CredentialSetSummary,
  CredentialTestResult,
  ManifestBuilderSourceDescriptor,
  PreflightResult,
  ProjectArtifactBindingRequest,
  ProjectArtifactBindingResponse,
  ProjectPreflightRequest,
  ProjectRecord,
  RunEventsResponse,
  RunFailuresResponse,
  RunRecord,
  RunSummary,
  RunWorkItemsResponse
} from "../types/api";

const configuredBaseUrl = import.meta.env.VITE_ADMIN_API_BASE_URL?.trim() ?? "";
const baseUrl = configuredBaseUrl.replace(/\/$/, "");

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
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
      message = body?.error ?? JSON.stringify(body);
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

async function requestForm<T>(path: string, form: FormData, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    method: init?.method ?? "POST",
    body: form
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;

    try {
      const body = await response.json();
      message = body?.error ?? JSON.stringify(body);
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

export const api = {
  health: () =>
    request<{ status: string; service: string; utc: string }>("/health"),

  connectors: () =>
    request<ConnectorsResponse>("/api/connectors"),

  connectorSchema: (connectorType: string, role?: string) => {
    const suffix = role ? `?role=${encodeURIComponent(role)}` : "";
    return request<ConnectorDescriptor>(`/api/connectors/${encodeURIComponent(connectorType)}/schema${suffix}`);
  },

  credentials: () =>
    request<CredentialSetSummary[]>("/api/credentials"),

  credential: (credentialSetId: string) =>
    request<CredentialSetSummary>(`/api/credentials/${encodeURIComponent(credentialSetId)}`),

  createCredential: (body: CreateCredentialSetRequest) =>
    request<CredentialSetSummary>("/api/credentials", {
      method: "POST",
      body: JSON.stringify(body)
    }),

  testCredential: (credentialSetId: string) =>
    request<CredentialTestResult>(`/api/credentials/${encodeURIComponent(credentialSetId)}/test`, {
      method: "POST"
    }),

  deleteCredential: (credentialSetId: string) =>
    request<void>(`/api/credentials/${encodeURIComponent(credentialSetId)}`, {
      method: "DELETE"
    }),

  projects: () =>
    request<ProjectRecord[]>("/api/projects"),

  project: (projectId: string) =>
    request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`),

  createProject: (body: CreateProjectRequest) =>
    request<ProjectRecord>("/api/projects", {
      method: "POST",
      body: JSON.stringify(body)
    }),

  deleteProject: (projectId: string) =>
    request<void>(`/api/projects/${encodeURIComponent(projectId)}`, {
      method: "DELETE"
    }),

  runs: () =>
    request<RunRecord[]>("/api/runs"),

  run: (runId: string) =>
    request<RunRecord>(`/api/runs/${encodeURIComponent(runId)}`),

  deleteRun: (runId: string) =>
    request<void>(`/api/runs/${encodeURIComponent(runId)}`, {
      method: "DELETE"
    }),

  cancelRun: (runId: string) =>
    request<RunRecord>(`/api/runs/${encodeURIComponent(runId)}/cancel`, {
      method: "POST"
    }),

  artifacts: (kind?: string) => {
    const suffix = kind ? `?kind=${encodeURIComponent(kind)}` : "";
    return request<ArtifactRecord[]>(`/api/artifacts${suffix}`);
  },

  artifactDownloadUrl: (artifactId: string) =>
    `${baseUrl}/api/artifacts/${encodeURIComponent(artifactId)}/download`,

  uploadArtifact: (file: File, options: { kind: string; projectId?: string | null; description?: string | null }) => {
    const form = new FormData();
    form.append("file", file);
    form.append("kind", options.kind);

    if (options.projectId) {
      form.append("projectId", options.projectId);
    }

    if (options.description) {
      form.append("description", options.description);
    }

    return requestForm<ArtifactRecord>("/api/artifacts", form);
  },

  deleteArtifact: (artifactId: string) =>
    request<void>(`/api/artifacts/${encodeURIComponent(artifactId)}`, {
      method: "DELETE"
    }),

projectArtifactBinding: (projectId: string) =>
  request<ProjectArtifactBindingResponse>(`/api/projects/${encodeURIComponent(projectId)}/artifacts/`),

bindProjectArtifacts: (projectId: string, body: ProjectArtifactBindingRequest) =>
  request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/artifacts/`, {
    method: "PUT",
    body: JSON.stringify(body)
  }),

  createRun: (projectId: string, body: CreateRunRequest) =>
    request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/runs`, {
      method: "POST",
      body: JSON.stringify(body)
    }),

  runPreflight: (projectId: string, body: ProjectPreflightRequest) =>
    request<PreflightResult>(`/api/projects/${encodeURIComponent(projectId)}/preflight`, {
      method: "POST",
      body: JSON.stringify(body)
    }),

  runSummary: (runId: string) =>
    request<RunSummary>(`/api/runs/${encodeURIComponent(runId)}/summary`),

  runEvents: (runId: string, take = 250) =>
    request<RunEventsResponse>(`/api/runs/${encodeURIComponent(runId)}/events?take=${take}`),

  runFailures: (runId: string) =>
    request<RunFailuresResponse>(`/api/runs/${encodeURIComponent(runId)}/failures`),

  runWorkItems: (runId: string) =>
    request<RunWorkItemsResponse>(`/api/runs/${encodeURIComponent(runId)}/work-items`),

  manifestBuilderSources: () =>
    request<ManifestBuilderSourceDescriptor[]>("/api/manifest-builder/sources"),

  buildManifest: (body: BuildSourceManifestRequest) =>
    request<BuildSourceManifestResponse>("/api/manifest-builder/build", {
      method: "POST",
      body: JSON.stringify(body)
    })
};

export function displayConnectorName(connector: { displayName?: string; type?: string; name?: string } | null | undefined) {
  return connector?.displayName || connector?.type || connector?.name || "Unknown";
}

export function connectorValue(connector: { type?: string; name?: string; displayName?: string } | null | undefined) {
  return connector?.type || connector?.name || connector?.displayName || "";
}