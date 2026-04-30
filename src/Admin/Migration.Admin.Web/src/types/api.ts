export type ConnectorDescriptor = {
  type?: string;
  name?: string;
  displayName?: string;
  description?: string;
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
  values: Record<string, string | null>;
  secretKeys: string[];
};

export type CreateCredentialSetRequest = {
  displayName: string;
  connectorType: string;
  connectorRole: string;
  values: Record<string, string | null>;
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
  artifactType?: string;
  kind?: string;
  fileName: string;
  createdUtc?: string;
  uploadedUtc?: string;
  projectId?: string | null;
};

export type ProjectRecord = {
  projectId: string;
  displayName: string;
  sourceType: string;
  targetType: string;
  manifestType: string;
  createdUtc: string;
  updatedUtc: string;
  settings?: Record<string, string | null>;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
};

export type ProjectArtifactBindingRequest = {
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
};

export type ProjectArtifactBindingResponse = {
  projectId: string;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  manifestArtifact?: ArtifactRecord | null;
  mappingArtifact?: ArtifactRecord | null;
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

export type CreateProjectRequest = {
  displayName: string;
  sourceType: string;
  targetType: string;
  manifestType: string;
  settings?: Record<string, string | null>;
};

export type CreateRunRequest = {
  jobName?: string | null;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  dryRun: boolean;
  parallelism: number;
  settings?: Record<string, string | null>;
};

export type ProjectPreflightRequest = {
  jobName?: string | null;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  settings?: Record<string, string | null>;
};

export type PreflightIssue = {
  severity: "Error" | "Warning" | "Info" | string;
  code: string;
  message: string;
  rowId?: string | null;
  field?: string | null;
  sourceAssetId?: string | null;
};

export type PreflightSummary = {
  totalRows: number;
  checkedRows: number;
  errorCount: number;
  warningCount: number;
  infoCount: number;
  passed: boolean;
};

export type PreflightResult = {
  preflightId: string;
  projectId?: string | null;
  jobName: string;
  status: "Passed" | "Warning" | "Failed" | string;
  startedUtc: string;
  completedUtc: string;
  summary: PreflightSummary;
  issues: PreflightIssue[];
  details?: Record<string, unknown>;
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
  sampleRows: Record<string, string>[];
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
    label: string;
    description?: string | null;
    required: boolean;
    placeholder?: string | null;
};

export type ManifestBuilderServiceDescriptor = {
    sourceType: string;
    serviceName: string;
    displayName: string;
    description?: string | null;
    options: ManifestBuilderOptionDescriptor[];
};

export type ManifestBuilderSourceDescriptor = {
    sourceType: string;
    displayName: string;
    services: ManifestBuilderServiceDescriptor[];
};

export type BuildSourceManifestRequest = {
    sourceType: string;
    serviceName: string;
    credentialSetId?: string | null;
    options?: Record<string, string>;
};

export type BuildSourceManifestResponse = {
    manifestId: string;
    sourceType: string;
    serviceName: string;
    fileName: string;
    contentType: string;
    rowCount: number;
    downloadUrl: string;
    createdUtc: string;
};
