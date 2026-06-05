Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminWebRoot 'src'

if (-not (Test-Path -Path $srcRoot -PathType Container)) {
    throw ('Admin Web src root was not found: {0}' -f $srcRoot)
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -Path $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
    Write-Host ('Updated: {0}' -f $Path)
}

$artifactManifestIndexPath = Join-Path $srcRoot 'api\artifactManifestIndex.ts'
$artifactStoragePath = Join-Path $srcRoot 'api\artifactStorage.ts'
$artifactStorageBridgePath = Join-Path $srcRoot 'api\artifactStorageBridge.ts'
$cloudBinaryStoragePath = Join-Path $srcRoot 'api\cloudBinaryStorage.ts'
$runtimeRunDetailPath = Join-Path $srcRoot 'features\operations\runtimeDashboard\pages\RuntimeRunDetail.tsx'

$requiredFiles = @(
    $artifactManifestIndexPath,
    $artifactStoragePath,
    $artifactStorageBridgePath,
    $cloudBinaryStoragePath,
    $runtimeRunDetailPath
)

foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -Path $requiredFile -PathType Leaf)) {
        throw ('Required target file was not found: {0}' -f $requiredFile)
    }
}

$artifactManifestIndexContent = @'
import { apiGet, apiPost } from './core/adminApiClient';
import type { ArtifactStorageDescriptor } from './artifactStorage';

export type ArtifactManifestEntry = {
  artifactId: string;
  artifactKind: string;
  fileName: string;
  objectKey: string;
  uri: string;
  contentType: string;
  createdUtc: string;
};

export type ArtifactManifestIndex = {
  workspaceId: string;
  schemaVersion: string;
  updatedUtc: string;
  artifacts: ArtifactManifestEntry[];
};

export type ArtifactManifestIndexProbeResponse = {
  artifact: ArtifactStorageDescriptor;
  index: ArtifactManifestIndex;
};

export async function getArtifactManifestIndex(): Promise<ArtifactManifestIndex> {
  return apiGet<ArtifactManifestIndex>('/api/cloud/artifacts/index');
}

export async function probeArtifactManifestIndex(): Promise<ArtifactManifestIndexProbeResponse> {
  return apiPost<Record<string, never>, ArtifactManifestIndexProbeResponse>(
    '/api/cloud/artifacts/index/probe',
    {}
  );
}
'@

$artifactStorageContent = @'
import { apiGet, apiPost } from './core/adminApiClient';
import type { CloudStorageLocation } from './cloudStorageLocations';

export type ArtifactStorageDescriptor = {
  workspaceId: string;
  artifactKind: string;
  artifactId: string;
  fileName: string;
  objectKey: string;
  location: CloudStorageLocation;
  contentType?: string | null;
  length?: number | null;
  eTag?: string | null;
};

export type ArtifactStorageProbeResponse = {
  exists: boolean;
  artifact: ArtifactStorageDescriptor;
};

export async function resolveArtifactStorage(
  kind = 'manifest',
  artifactId = 'sample-artifact',
  fileName = 'sample.json'
): Promise<ArtifactStorageDescriptor> {
  const params = new URLSearchParams({ kind, artifactId, fileName });
  return apiGet<ArtifactStorageDescriptor>(`/api/cloud/artifacts/resolve?${params}`);
}

export async function probeArtifactStorage(): Promise<ArtifactStorageProbeResponse> {
  return apiPost<Record<string, never>, ArtifactStorageProbeResponse>(
    '/api/cloud/artifacts/probe',
    {}
  );
}
'@

$artifactStorageBridgeContent = @'
import { apiDelete, apiGet, apiPost } from './core/adminApiClient';
import type { ArtifactManifestIndex } from './artifactManifestIndex';
import type { ArtifactStorageDescriptor } from './artifactStorage';

export type ArtifactStorageBridgeUploadResponse = {
  artifact: ArtifactStorageDescriptor;
  index: ArtifactManifestIndex;
};

export type ArtifactStorageBridgeDeleteResponse = {
  deleted: boolean;
  artifactKind: string;
  artifactId: string;
  fileName: string;
  workspaceId: string;
};

export async function uploadArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string,
  content: string | Blob,
  contentType = 'application/octet-stream'
): Promise<ArtifactStorageBridgeUploadResponse> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;

  return apiPost<string | Blob, ArtifactStorageBridgeUploadResponse>(path, content, {
    headers: {
      'Content-Type': contentType
    }
  });
}

export async function downloadArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string
): Promise<Blob> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;
  const response = await fetch(path, { method: 'GET' });

  if (!response.ok) {
    throw new Error(`Artifact download failed: ${response.status} ${response.statusText}`);
  }

  return response.blob();
}

export async function deleteArtifactBridge(
  artifactKind: string,
  artifactId: string,
  fileName: string
): Promise<ArtifactStorageBridgeDeleteResponse> {
  const path = `/api/cloud/artifacts/${encodeURIComponent(artifactKind)}/${encodeURIComponent(artifactId)}/files/${encodeURIComponent(fileName)}`;
  return apiDelete<ArtifactStorageBridgeDeleteResponse>(path);
}
'@

$cloudBinaryStorageContent = @'
import { apiGet, apiPost } from './core/adminApiClient';
import type { CloudStorageLocation } from './cloudStorageLocations';

export type CloudBinaryStorageProviderCapabilities = {
  provider: string;
  supportsStreamingWrites: boolean;
  supportsMultipartUploads: boolean;
  supportsObjectTags: boolean;
  supportsLeases: boolean;
  supportsVersioning: boolean;
  supportsConditionalWrites: boolean;
  supportsSignedUrls: boolean;
};

export type CloudBinaryStorageProbeResponse = {
  workspaceId: string;
  exists: boolean;
  location: CloudStorageLocation;
};

export async function getCloudBinaryStorageProvider(): Promise<CloudBinaryStorageProviderCapabilities> {
  return apiGet<CloudBinaryStorageProviderCapabilities>('/api/cloud/storage/provider');
}

export async function probeCloudBinaryStorage(): Promise<CloudBinaryStorageProbeResponse> {
  return apiPost<Record<string, never>, CloudBinaryStorageProbeResponse>(
    '/api/cloud/storage/probe',
    {}
  );
}
'@

$runtimeRunDetailContent = @'
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { runtimeDashboardApi } from "../api/runtimeDashboardApi";
import { Card, EmptyState, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import type { RuntimeDashboardRunDetail } from "../types/runtimeDashboard";

function formatDate(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatNumber(value?: number | null) {
  return value === undefined || value === null ? "—" : value.toLocaleString();
}

export function RuntimeRunDetail() {
  const { runId } = useParams();
  const [detail, setDetail] = useState<RuntimeDashboardRunDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    if (!runId) {
      setError("Missing run id.");
      setLoading(false);
      return;
    }

    setError(null);

    try {
      const result = await runtimeDashboardApi.runDetail(runId);
      setDetail(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [runId]);

  if (loading) {
    return <LoadingError loading />;
  }

  if (error) {
    return <LoadingError message={error} onRetry={() => void load()} />;
  }

  if (!detail || !detail.run) {
    return <EmptyState title="Run not found" message="The requested runtime run was not returned by the Admin API." />;
  }

  const run = detail.run;
  const displayName = run.runName ?? run.runKey ?? run.runId;

  return (
    <>
      <Link to="/operations/runtime-dashboard">← Runtime dashboard</Link>

      <Card
        title={displayName}
        subtitle="Operational runtime run detail from Azure SQL."
        action={<button type="button" onClick={() => void load()}>Refresh</button>}
      >
        <div className="metric-grid">
          <div>
            <span>Total work items</span>
            <strong>{formatNumber(run.workItemCount)}</strong>
          </div>
          <div>
            <span>Completed</span>
            <strong>{formatNumber(run.completedWorkItemCount)}</strong>
          </div>
          <div>
            <span>Failed</span>
            <strong>{formatNumber(run.failedWorkItemCount)}</strong>
          </div>
          <div>
            <span>Status</span>
            <StatusPill status={run.status ?? undefined} />
          </div>
        </div>
      </Card>

      <Card title="Run metadata">
        <dl className="detail-grid">
          <dt>Run ID</dt>
          <dd>{run.runId}</dd>
          <dt>Run key</dt>
          <dd>{run.runKey ?? "—"}</dd>
          <dt>Environment</dt>
          <dd>{run.environmentName ?? "—"}</dd>
          <dt>Requested</dt>
          <dd>{formatDate(run.requestedAtUtc)}</dd>
          <dt>Created</dt>
          <dd>{formatDate(run.createdAtUtc)}</dd>
          <dt>Updated</dt>
          <dd>{formatDate(run.updatedAtUtc)}</dd>
        </dl>
      </Card>

      <Card title="Work items">
        {detail.workItems.length === 0 ? (
          <EmptyState title="No work items" message="No work items were returned for this run." />
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Status</th>
                <th>Type</th>
                <th>Attempts</th>
                <th>Updated</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {detail.workItems.map((item) => (
                <tr key={item.workItemId}>
                  <td>{item.workItemId}</td>
                  <td><StatusPill status={item.status ?? undefined} /></td>
                  <td>{item.workType ?? "—"}</td>
                  <td>{formatNumber(item.attemptCount)}</td>
                  <td>{formatDate(item.updatedAtUtc)}</td>
                  <td>{item.lastErrorMessage ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </>
  );
}
'@

Write-Utf8NoBomFile -Path $artifactManifestIndexPath -Content $artifactManifestIndexContent
Write-Utf8NoBomFile -Path $artifactStoragePath -Content $artifactStorageContent
Write-Utf8NoBomFile -Path $artifactStorageBridgePath -Content $artifactStorageBridgeContent
Write-Utf8NoBomFile -Path $cloudBinaryStoragePath -Content $cloudBinaryStorageContent
Write-Utf8NoBomFile -Path $runtimeRunDetailPath -Content $runtimeRunDetailContent

$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BZ-AdminWebFinalCompileBucketRepair.Report.md'
$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BZ - Admin Web Final Compile Bucket Repair')
[void]$report.Add('')
[void]$report.Add('Updated final artifact/cloud API probe compatibility files and aligned RuntimeRunDetail to the nested runtime-dashboard detail model.')
[void]$report.Add('')
[void]$report.Add('Updated files:')
foreach ($requiredFile in $requiredFiles) {
    [void]$report.Add(('- `{0}`' -f $requiredFile.Replace($repoRoot.Path + '\', '').Replace('\', '/')))
}
[System.IO.File]::WriteAllLines($reportPath, $report, [System.Text.Encoding]::UTF8)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BZ Admin Web final compile bucket repair applied.'
