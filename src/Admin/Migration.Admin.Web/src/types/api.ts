export type JsonObject = Record<string, unknown>;

export type ConnectorDescriptor = {
  type?: string;
  name?: string;
  displayName?: string;
  description?: string | null;
  direction?: string;
  capabilities?: unknown;
  credentials?: unknown[];
  options?: unknown[];
};

export type ConnectorsResponse = {
  sources: ConnectorDescriptor[];
  targets: ConnectorDescriptor[];
  manifestProviders: ConnectorDescriptor[];
};

export type CredentialSetSummary = {
  credentialSetId: string;
  displayName: string;
  connectorType: string;
  connectorRole: string;
  createdUtc: string;
  updatedUtc: string;
  values: JsonObject;
  secretKeys: string[];
};

export type CreateCredentialSetRequest = {
  displayName: string;
  connectorType: string;
  connectorRole: string;
  values: JsonObject;
  secretKeys?: string[];
};

export type CredentialTestResult = {
  credentialSetId: string;
  connectorType: string;
  connectorRole: string;
  success: boolean;
  message: string;
  testedUtc: string;
};

export type ArtifactRecord = {
  artifactId: string;
  kind?: string;
  artifactType?: string;
  fileName: string;
  contentType?: string;
  createdUtc?: string;
  uploadedUtc?: string;
  projectId?: string | null;
  description?: string | null;
  metadata?: JsonObject;
};

export type ProjectRecord = {
  projectId: string;
  displayName: string;
  sourceType: string;
  targetType: string;
  manifestType: string;
  createdUtc: string;
  updatedUtc: string;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  taxonomyArtifactId?: string | null;
  sourceCredentialSetId?: string | null;
  targetCredentialSetId?: string | null;
  settings?: Record<string, string | null | undefined>;
};

export type CreateProjectRequest = {
  displayName: string;
  sourceType: string;
  targetType: string;
  manifestType: string;
  settings?: Record<string, string | null | undefined>;
};

export type BindProjectArtifactsRequest = {
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  taxonomyArtifactId?: string | null;
};

export type BindProjectCredentialsRequest = {
  sourceCredentialSetId?: string | null;
  targetCredentialSetId?: string | null;
};

export type RunRecord = {
  runId: string;
  projectId: string;
  jobName: string;
  status: string;
  dryRun: boolean;
  createdUtc: string;
  updatedUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  message?: string | null;
  job?: unknown;
};

export type CreateRunRequest = {
  jobName?: string | null;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  dryRun: boolean;
  parallelism: number;
  settings?: Record<string, string | null | undefined>;
};

export type RunSummary = {
  runId?: string;
  jobName?: string;
  status?: string;
  total?: number;
  completed?: number;
  failed?: number;
  skipped?: number;
  running?: number;
  percentComplete?: number;
  recentFailures?: unknown[];
  [key: string]: unknown;
};

export type RunEventsResponse = {
  runId: string;
  count: number;
  events: unknown[];
};

export type RunFailuresResponse = {
  runId: string;
  count: number;
  failures: unknown[];
};

export type RunWorkItemsResponse = {
  runId: string;
  jobName: string;
  count: number;
  workItems: unknown[];
};

export type ManifestPreview = {
  artifactId: string;
  fileName: string;
  columns: string[];
  sampleRows: JsonObject[];
};

export type MappingBuilderFieldMap = {
  source: string;
  target: string;
  transform?: string | null;
};

export type SaveMappingArtifactRequest = {
  profileName: string;
  sourceType: string;
  targetType: string;
  fieldMappings: MappingBuilderFieldMap[];
  requiredTargetFields: string[];
  manifestArtifactId?: string | null;
  projectId?: string | null;
  fileName?: string | null;
  description?: string | null;
};

export type SaveMappingArtifactResponse = {
  artifact: ArtifactRecord;
  mappingProfile: unknown;
};

export type ManifestBuilderOptionDescriptor = {
  name: string;
  label?: string | null;
  description?: string | null;
  placeholder?: string | null;
  required?: boolean;
  defaultValue?: unknown;
  type?: string;
};

export type ManifestBuilderServiceDescriptor = {
  sourceType?: string;
  serviceName: string;
  displayName: string;
  description?: string | null;
  options: ManifestBuilderOptionDescriptor[];
};

export type ManifestBuilderSourceDescriptor = {
  sourceType: string;
  displayName: string;
  description?: string | null;
  services: ManifestBuilderServiceDescriptor[];
};

export type BuildSourceManifestRequest = {
  sourceType: string;
  serviceName?: string | null;
  credentialSetId?: string | null;
  projectId?: string | null;
  fileName?: string | null;
  options?: Record<string, string | null | undefined>;
};

export type BuildSourceManifestResponse = {
  artifact?: ArtifactRecord;
  artifactId?: string;
  manifestId?: string;
  fileName?: string;
  rowCount?: number;
  sourceType?: string;
  serviceName?: string;
  message?: string;
  downloadUrl?: string;
};

export type PreflightIssue = {
    severity?: string | null;
    message?: string | null;
    code?: string | null;
    field?: string | null;
    path?: string | null;
    rowId?: string | number | null;
    [key: string]: unknown;
};

export type PreflightSummary = {
    totalRows?: number;
    checkedRows?: number;
    errorCount?: number;
    warningCount?: number;
    [key: string]: unknown;
};

export type PreflightResult = {
    preflightId?: string;
    projectId?: string;
    runId?: string;
    status?: string;
    success?: boolean;
    passed?: boolean;
    issueCount?: number;
    summary?: PreflightSummary;
    issues?: PreflightIssue[];
    warnings?: PreflightIssue[];
    errors?: PreflightIssue[];
    [key: string]: unknown;
};
