import type {
  ArtifactRecord,
  BindProjectArtifactsRequest,
  BindProjectCredentialsRequest,
  BuildSourceManifestRequest,
  BuildSourceManifestResponse,
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
        /* keep default */
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

// UI descriptor augmentation only. Keeps existing pages/components/styles unchanged.
function normalizeConnectorKey(value: unknown) {
  return String(value ?? "").trim().toLowerCase();
}

function hasConnector(items: ConnectorDescriptor[] | undefined, connectorType: string) {
  const expected = normalizeConnectorKey(connectorType);

  return (items ?? []).some(item =>
    normalizeConnectorKey(item.type) === expected ||
    normalizeConnectorKey(item.name) === expected ||
    normalizeConnectorKey(item.displayName) === expected
  );
}

const sharePointCredentialFields = [
  { name: "Mode", label: "Mode", description: "Use Rclone or Graph.", required: true, defaultValue: "Rclone" },
  { name: "RcloneExecutablePath", label: "rclone executable path", description: "Path to rclone.exe or rclone when using Rclone mode.", required: false, defaultValue: "rclone" },
  { name: "RcloneConfigPath", label: "rclone config path", description: "Path to rclone.conf when using Rclone mode.", required: false, secret: true, defaultValue: "C:\\Migration\\rclone.conf" },
  { name: "RcloneRemoteName", label: "rclone remote name", description: "Configured rclone remote for the SharePoint document library.", required: false, defaultValue: "sharepoint" },
  { name: "TenantId", label: "Tenant ID", description: "Entra tenant id for Graph mode.", required: false, defaultValue: "your-tenant-id" },
  { name: "ClientId", label: "Client ID", description: "Entra app registration client id for Graph mode.", required: false, secret: true, defaultValue: "your-client-id" },
  { name: "ClientSecret", label: "Client Secret", description: "Entra app registration client secret for Graph mode.", required: false, secret: true, defaultValue: "your-client-secret" }
];

const sharePointOptionFields = [
  { name: "SiteUrl", label: "Site URL", description: "SharePoint site URL, for example https://tenant.sharepoint.com/sites/assets.", required: false },
  { name: "LibraryName", label: "Library name", description: "Document library name, for example Documents.", required: false },
  { name: "SourcePath", label: "Source path", description: "Folder/path to migrate, for example Documents/Images or Shared Documents/Images.", required: false },
  { name: "IncludeFolderMetadata", label: "Include folder metadata", description: "Derive metadata from folder names and folder depth.", required: false },
  { name: "IncludeFileNameMetadata", label: "Include file-name metadata", description: "Derive metadata from file naming conventions.", required: false },
  { name: "IncludeGraphMetadata", label: "Include Graph metadata", description: "Include Graph/SharePoint item metadata when Graph mode is used.", required: false }
];

const s3CredentialFields = [
  { name: "AccessKey", label: "Access key", description: "AWS access key id for the S3 target bucket.", required: true, secret: true },
  { name: "SecretKey", label: "Secret key", description: "AWS secret access key for the S3 target bucket.", required: true, secret: true },
  { name: "Region", label: "Region", description: "AWS region, for example us-east-1.", required: true, defaultValue: "us-east-1" },
  { name: "BucketName", label: "Bucket name", description: "Destination S3 bucket name.", required: true },
  { name: "Prefix", label: "Prefix", description: "Optional default destination key prefix.", required: false },
  { name: "ServiceUrl", label: "Service URL", description: "Optional S3-compatible endpoint for MinIO, R2, Wasabi, etc.", required: false },
  { name: "ForcePathStyle", label: "Force path style", description: "true/false. Enable for many S3-compatible endpoints.", required: false, defaultValue: "false" }
];

const s3TargetOptionFields = [
  { name: "BucketName", label: "Bucket name", description: "Destination S3 bucket name.", required: false },
  { name: "Prefix", label: "Prefix", description: "Optional default destination key prefix.", required: false },
  { name: "ObjectKeyTemplate", label: "Object key template", description: "Optional object key template used by mappings, for example {sourceRelativePath}.", required: false }
];

const s3TargetDescriptor: ConnectorDescriptor = {
  type: "S3",
  name: "S3",
  displayName: "S3",
  description: "Amazon S3 target connector for writing migrated binaries to an S3 bucket.",
  direction: "Target",
  capabilities: { canWriteAssets: true, supportsPrefixes: true },
  credentials: s3CredentialFields,
  options: s3TargetOptionFields
};

const bynderCredentialFields = [
  { name: "BaseUrl", label: "Base URL", description: "Bynder portal base URL, for example https://example.getbynder.com.", required: true },
  { name: "ClientId", label: "Client ID", description: "Bynder OAuth client id.", required: true, secret: true },
  { name: "ClientSecret", label: "Client Secret", description: "Bynder OAuth client secret.", required: true, secret: true },
  { name: "Scopes", label: "Scopes", description: "OAuth scopes used by the Bynder SDK.", required: true, defaultValue: "asset:read asset:write asset:delete meta.assetbank:read" },
  { name: "BrandStoreId", label: "Brand store ID", description: "Required when Bynder is used as a target. Optional for source-only workflows.", required: false }
];

const bynderSourceOptionFields = [
  { name: "SourceUriField", label: "Source URI field", description: "Manifest field containing the Bynder download/source URL. Defaults to sourceUri/downloadUrl/url.", required: false },
  { name: "FileNameField", label: "File name field", description: "Manifest field containing the filename. Defaults to fileName/name.", required: false }
];

const bynderSourceDescriptor: ConnectorDescriptor = {
  type: "Bynder",
  name: "Bynder",
  displayName: "Bynder",
  description: "Bynder source connector for manifest-driven migrations using Bynder asset/download URLs.",
  direction: "Source",
  capabilities: { canReadAssets: true },
  credentials: bynderCredentialFields,
  options: bynderSourceOptionFields
};


const sharePointSourceDescriptor: ConnectorDescriptor = {
  type: "SharePoint",
  name: "SharePoint",
  displayName: "SharePoint",
  description: "SharePoint Online source connector. Supports rclone and Microsoft Graph modes.",
  direction: "Source",
  capabilities: { canReadAssets: true, canBuildManifest: true, modes: ["Rclone", "Graph"] },
  credentials: sharePointCredentialFields,
  options: sharePointOptionFields
};

const sharePointManifestProviderDescriptor: ConnectorDescriptor = {
  type: "SharePoint",
  name: "SharePoint",
  displayName: "SharePoint",
  description: "Build a manifest from SharePoint using rclone path data or Graph metadata.",
  direction: "Manifest",
  capabilities: { canBuildManifest: true, modes: ["Rclone", "Graph"] },
  credentials: sharePointCredentialFields,
  options: sharePointOptionFields
};

function withSharePointConnectors(data: ConnectorsResponse): ConnectorsResponse {
  const sources = [...(data?.sources ?? [])];
  const targets = [...(data?.targets ?? [])];
  const manifestProviders = [...(data?.manifestProviders ?? [])];

  if (!hasConnector(sources, "SharePoint")) sources.push(sharePointSourceDescriptor);
  if (!hasConnector(sources, "Bynder")) sources.push(bynderSourceDescriptor);
  if (!hasConnector(targets, "S3")) targets.push(s3TargetDescriptor);
  if (!hasConnector(manifestProviders, "SharePoint")) manifestProviders.push(sharePointManifestProviderDescriptor);

  return { sources, targets, manifestProviders };
}

type ManifestBuilderSourceDescriptorLike = {
  sourceType: string;
  displayName: string;
  services: Array<{
    sourceType?: string;
    serviceName: string;
    displayName: string;
    description?: string | null;
    options: Array<{
      name: string;
      label?: string | null;
      description?: string | null;
      required?: boolean;
      placeholder?: string | null;
    }>;
  }>;
};

function withFallbackManifestSources<T extends ManifestBuilderSourceDescriptorLike[]>(sources: T): T {
  const result = [...(sources ?? [])] as ManifestBuilderSourceDescriptorLike[];

  if (!result.some(source => normalizeConnectorKey(source.sourceType) === "sharepoint")) {
    result.push({
      sourceType: "SharePoint",
      displayName: "SharePoint",
      services: [
        {
          sourceType: "SharePoint",
          serviceName: "Rclone",
          displayName: "rclone folder/file manifest",
          description: "Builds a manifest from SharePoint paths using rclone. Metadata is derived from folders, folder depth, and file naming.",
          options: [
            { name: "SourcePath", label: "Source path", description: "SharePoint path under the rclone remote, for example Documents/Images.", required: true, placeholder: "Documents/Images" },
            { name: "IncludeFolderMetadata", label: "Include folder metadata", description: "true/false", required: false, placeholder: "true" },
            { name: "IncludeFileNameMetadata", label: "Include file-name metadata", description: "true/false", required: false, placeholder: "true" },
            { name: "MaxDepth", label: "Max folder depth", description: "Optional maximum folder depth to include.", required: false, placeholder: "" }
          ]
        },
        {
          sourceType: "SharePoint",
          serviceName: "Graph",
          displayName: "Graph metadata manifest",
          description: "Builds a manifest from SharePoint through Microsoft Graph and can include richer file metadata.",
          options: [
            { name: "SiteUrl", label: "Site URL", description: "SharePoint site URL.", required: true, placeholder: "https://tenant.sharepoint.com/sites/assets" },
            { name: "LibraryName", label: "Library name", description: "Document library name.", required: false, placeholder: "Documents" },
            { name: "SourcePath", label: "Source path", description: "Optional folder path under the library.", required: false, placeholder: "Images" },
            { name: "IncludeGraphMetadata", label: "Include Graph metadata", description: "true/false", required: false, placeholder: "true" },
            { name: "IncludeFolderMetadata", label: "Include folder metadata", description: "true/false", required: false, placeholder: "true" },
            { name: "IncludeFileNameMetadata", label: "Include file-name metadata", description: "true/false", required: false, placeholder: "true" }
          ]
        }
      ]
    });
  }

  if (!result.some(source => normalizeConnectorKey(source.sourceType) === "aem")) {
    result.push({
      sourceType: "AEM",
      displayName: "AEM",
      services: [
        {
          sourceType: "AEM",
          serviceName: "ExportFolders",
          displayName: "AEM folder export manifest",
          description: "Builds an AEM manifest from one or more DAM folders.",
          options: [
            {
              name: "Folders",
              label: "Export folders",
              description: "One AEM DAM folder path per line. Example: /content/dam/site/folder",
              required: true,
              placeholder: "/content/dam/site/folder-one\n/content/dam/site/folder-two"
            },
            {
              name: "Recursive",
              label: "Recursive",
              description: "true/false. Include assets below each selected folder.",
              required: false,
              placeholder: "true"
            }
          ]
        }
      ]
    });
  }

  return result as T;
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

export type BuildManifestRequest = BuildSourceManifestRequest;
export type BuildManifestResponse = BuildSourceManifestResponse;

export const api = {
  health: () => request<{ status: string; service: string; utc: string }>("/health"),
  connectors: () => request<ConnectorsResponse>("/api/connectors").then(withSharePointConnectors),
  projects: () => request<ProjectRecord[]>("/api/projects"),
  project: (projectId: string) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`),
  createProject: (payload: CreateProjectRequest) => request<ProjectRecord>("/api/projects", { method: "POST", body: JSON.stringify(payload) }),
  updateProject: (projectId: string, payload: Partial<ProjectRecord>) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}`, { method: "PUT", body: JSON.stringify(payload) }),
  deleteProject: (projectId: string) => request<void>(`/api/projects/${encodeURIComponent(projectId)}`, { method: "DELETE" }),
  bindProjectArtifacts: (projectId: string, payload: BindProjectArtifactsRequest) => request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/artifacts`, { method: "PUT", body: JSON.stringify(payload) }),
  bindProjectCredentials: (projectOrProjectId: ProjectRecord | string, payload: BindProjectCredentialsRequest) => {
    const projectId = resolveProjectId(projectOrProjectId);
    return request<ProjectRecord>(`/api/projects/${encodeURIComponent(projectId)}/credentials`, { method: "PUT", body: JSON.stringify(payload) });
  },
  artifacts: (kind?: string) => request<ArtifactRecord[]>(`/api/artifacts${queryString({ kind })}`),
  artifactDownloadUrl: (artifactId: string) => `/api/artifacts/${encodeURIComponent(artifactId)}/download`,
  deleteArtifact: (artifactId: string) => request<void>(`/api/artifacts/${encodeURIComponent(artifactId)}`, { method: "DELETE" }),
  uploadArtifact: (kind: string, file: File) => {
    const form = new FormData();
    form.append("kind", kind);
    form.append("artifactType", kind);
    form.append("file", file);
    return request<ArtifactRecord>("/api/artifacts", { method: "POST", body: form });
  },
  manifestBuilderSources: () => request<ManifestBuilderSourceDescriptorLike[]>("/api/manifest-builder/sources").then(withFallbackManifestSources),
  buildManifest: (payload: BuildSourceManifestRequest) => request<BuildSourceManifestResponse>("/api/manifest-builder/build", { method: "POST", body: JSON.stringify(payload) }),
  createManifestArtifact: (payload: BuildSourceManifestRequest) => request<BuildSourceManifestResponse>("/api/manifest-builder/build", { method: "POST", body: JSON.stringify(payload) }),
  runs: () => request<RunRecord[]>("/api/runs"),
  run: (runId: string) => request<RunRecord>(`/api/runs/${encodeURIComponent(runId)}`),
  createRun: (projectId: string, payload: CreateRunRequest) => request<RunRecord>(`/api/projects/${encodeURIComponent(projectId)}/runs`, { method: "POST", body: JSON.stringify(payload) }),
  deleteRun: (runId: string) => request<void>(`/api/runs/${encodeURIComponent(runId)}`, { method: "DELETE" }),
  queuePreflight: (projectId: string) => request<unknown>(`/api/projects/${encodeURIComponent(projectId)}/preflight`, { method: "POST" }),
  runSummary: (runId: string) => request<RunSummary>(`/api/runs/${encodeURIComponent(runId)}/summary`),
  runEvents: (runId: string, take = 250) => request<RunEventsResponse>(`/api/runs/${encodeURIComponent(runId)}/events${queryString({ take })}`),
  runFailures: (runId: string) => request<RunFailuresResponse>(`/api/runs/${encodeURIComponent(runId)}/failures`),
  runWorkItems: (runId: string) => request<RunWorkItemsResponse>(`/api/runs/${encodeURIComponent(runId)}/work-items`),
  credentials: () => request<CredentialSetSummary[]>("/api/credentials"),
  createCredential: (payload: CreateCredentialSetRequest) => request<CredentialSetSummary>("/api/credentials", { method: "POST", body: JSON.stringify(payload) }),
  testCredential: (credentialSetId: string) => request<CredentialTestResult>(`/api/credentials/${encodeURIComponent(credentialSetId)}/test`, { method: "POST" }),
  deleteCredential: (credentialSetId: string) => request<void>(`/api/credentials/${encodeURIComponent(credentialSetId)}`, { method: "DELETE" })
};
