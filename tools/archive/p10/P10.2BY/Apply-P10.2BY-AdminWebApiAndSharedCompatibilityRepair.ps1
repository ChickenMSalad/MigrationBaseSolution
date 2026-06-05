Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BY-AdminWebApiAndSharedCompatibilityRepair.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BY - Admin Web API and Shared Compatibility Repair')
[void]$report.Add('')
[void]$report.Add(('Admin Web source root: `{0}`' -f $sourceRoot))
[void]$report.Add('')

$coreDir = Join-Path $sourceRoot 'api\core'
if (-not (Test-Path -Path $coreDir -PathType Container)) {
    New-Item -Path $coreDir -ItemType Directory -Force | Out-Null
}

$adminApiClientPath = Join-Path $coreDir 'adminApiClient.ts'
$adminApiClientContent = @'
import { AdminApiError } from './adminApiError';

const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? '').replace(/\/$/, '');

export type ApiRequestOptions = RequestInit & {
  parseJson?: boolean;
};

async function tryReadBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? '';

  if (contentType.includes('application/json')) {
    try {
      return await response.json();
    } catch {
      return undefined;
    }
  }

  try {
    return await response.text();
  } catch {
    return undefined;
  }
}

function hasBody(body: unknown): body is BodyInit {
  return body !== undefined && body !== null;
}

function toRequestBody(body: unknown): BodyInit | undefined {
  if (!hasBody(body)) {
    return undefined;
  }

  if (
    typeof body === 'string' ||
    body instanceof Blob ||
    body instanceof FormData ||
    body instanceof URLSearchParams ||
    body instanceof ArrayBuffer
  ) {
    return body;
  }

  return JSON.stringify(body);
}

function withJsonHeaders(options?: ApiRequestOptions, body?: unknown): ApiRequestOptions {
  const headers = new Headers(options?.headers ?? undefined);

  if (hasBody(body) && !(body instanceof Blob) && !(body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }

  return {
    ...(options ?? {}),
    headers,
  };
}

export async function apiRequest<TResponse = unknown>(
  path: string,
  options?: ApiRequestOptions,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...(options ?? {}),
    headers: {
      Accept: 'application/json',
      ...(options?.headers ?? {}),
    },
  });

  const responseBody = await tryReadBody(response);

  if (!response.ok) {
    const message =
      typeof responseBody === 'object' &&
      responseBody !== null &&
      'error' in responseBody &&
      typeof (responseBody as { error?: unknown }).error === 'string'
        ? String((responseBody as { error: string }).error)
        : `Request failed with status ${response.status}`;

    throw new AdminApiError(message, response.status, response.statusText, responseBody);
  }

  if (options?.parseJson === false) {
    return responseBody as TResponse;
  }

  return responseBody as TResponse;
}

export async function apiGet<TResponse = unknown>(path: string, options?: ApiRequestOptions): Promise<TResponse> {
  return apiRequest<TResponse>(path, { ...(options ?? {}), method: 'GET' });
}

export async function apiPost<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPost<TRequest, TResponse>(path: string, body: TRequest, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPost<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse> {
  const requestOptions = withJsonHeaders(options, body);

  return apiRequest<TResponse>(path, {
    ...requestOptions,
    method: 'POST',
    body: toRequestBody(body),
  });
}

export async function apiPut<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPut<TRequest, TResponse>(path: string, body: TRequest, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPut<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse> {
  const requestOptions = withJsonHeaders(options, body);

  return apiRequest<TResponse>(path, {
    ...requestOptions,
    method: 'PUT',
    body: toRequestBody(body),
  });
}

export async function apiDelete<TResponse = unknown>(path: string, options?: ApiRequestOptions): Promise<TResponse> {
  return apiRequest<TResponse>(path, { ...(options ?? {}), method: 'DELETE' });
}

export const adminApiClient = {
  request: apiRequest,
  get: apiGet,
  post: apiPost,
  put: apiPut,
  delete: apiDelete,
};
'@
Set-Content -Path $adminApiClientPath -Value $adminApiClientContent -Encoding UTF8
[void]$report.Add(('Rewrote compatibility API client: `{0}`' -f $adminApiClientPath))

$coreClientPath = Join-Path $coreDir 'client.ts'
Set-Content -Path $coreClientPath -Value "export * from './adminApiClient';`r`n" -Encoding UTF8
[void]$report.Add(('Wrote compatibility client re-export: `{0}`' -f $coreClientPath))

$cardPath = Join-Path $sourceRoot 'components\Card.tsx'
if (Test-Path -Path $cardPath -PathType Leaf) {
$cardContent = @'
import type { ReactNode } from "react";

export function Card({
  title,
  subtitle,
  description,
  action,
  children,
}: {
  title?: string;
  subtitle?: string;
  description?: string;
  action?: ReactNode;
  children?: ReactNode;
}) {
  const supportingText = subtitle ?? description;

  return (
    <section className="card">
      {(title || supportingText || action) && (
        <div className="card-header">
          <div>
            {title && <h2>{title}</h2>}
            {supportingText && <p>{supportingText}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </section>
  );
}

export function StatusPill({ status, value }: { status?: string; value?: string }) {
  const displayValue = status ?? value ?? "Unknown";
  const normalized = displayValue.toLowerCase();
  const kind = normalized.includes("fail")
    ? "bad"
    : normalized.includes("complete")
      ? "good"
      : normalized.includes("run") || normalized.includes("queue")
        ? "warn"
        : "neutral";

  return <span className={`pill ${kind}`}>{displayValue}</span>;
}

export function EmptyState({
  title,
  message,
  description,
}: {
  title: string;
  message?: string;
  description?: string;
}) {
  const text = message ?? description;

  return (
    <div className="empty-state">
      <h3>{title}</h3>
      {text && <p>{text}</p>}
    </div>
  );
}

export function JsonBlock({ value }: { value: unknown }) {
  return <pre className="json-block">{JSON.stringify(value, null, 2)}</pre>;
}
'@
    Set-Content -Path $cardPath -Value $cardContent -Encoding UTF8
    [void]$report.Add(('Rewrote shared component compatibility surface: `{0}`' -f $cardPath))
}

$loadingErrorPath = Join-Path $sourceRoot 'components\LoadingError.tsx'
if (Test-Path -Path $loadingErrorPath -PathType Leaf) {
$loadingErrorContent = @'
type LoadingErrorProps = {
  loading?: boolean;
  title?: string;
  error?: string | null;
  message?: string | null;
  onRetry?: () => void;
};

export function LoadingError({ loading, title, error, message, onRetry }: LoadingErrorProps) {
  if (loading) {
    return <p className="muted">Loading…</p>;
  }

  const text = error ?? message;

  if (!text) {
    return null;
  }

  return (
    <div className="error-state">
      {title && <h3>{title}</h3>}
      <p>{text}</p>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}
'@
    Set-Content -Path $loadingErrorPath -Value $loadingErrorContent -Encoding UTF8
    [void]$report.Add(('Rewrote loading/error compatibility surface: `{0}`' -f $loadingErrorPath))
}

$preflightPath = Join-Path $sourceRoot 'api\preflight.ts'
if (-not (Test-Path -Path $preflightPath -PathType Leaf)) {
$preflightContent = @'
import { apiPost } from './core/adminApiClient';
import type { PreflightResult } from '../types/api';

export type RunProjectPreflightRequest = {
  jobName: string;
  manifestPath?: string | null;
  mappingProfilePath?: string | null;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
  settings?: Record<string, string>;
};

export async function runProjectPreflight(
  projectId: string,
  request: RunProjectPreflightRequest,
): Promise<PreflightResult> {
  return apiPost<RunProjectPreflightRequest, PreflightResult>(
    `/api/projects/${encodeURIComponent(projectId)}/preflight`,
    request,
  );
}
'@
    Set-Content -Path $preflightPath -Value $preflightContent -Encoding UTF8
    [void]$report.Add(('Added missing shared Preflight API bridge: `{0}`' -f $preflightPath))
} else {
    [void]$report.Add(('Preflight API bridge already exists: `{0}`' -f $preflightPath))
}

$artifactPath = Join-Path $sourceRoot 'api\artifactStorageBridge.ts'
if (Test-Path -Path $artifactPath -PathType Leaf) {
    $artifactContent = Get-Content -Path $artifactPath -Raw
    $artifactContent = [regex]::Replace($artifactContent, 'import\s+\{[^}]*\}\s+from\s+[''"]\.\/core\/adminApiClient[''"];\s*', "import { apiDelete, apiGet, apiPost } from './core/adminApiClient';`r`n")
    if ($artifactContent -notmatch 'from\s+[''"]\.\/core\/adminApiClient[''"]') {
        $artifactContent = "import { apiDelete, apiGet, apiPost } from './core/adminApiClient';`r`n" + $artifactContent
    }
    Set-Content -Path $artifactPath -Value $artifactContent -Encoding UTF8
    [void]$report.Add(('Normalized artifact storage bridge API helper import: `{0}`' -f $artifactPath))
}

$featureApiFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$featureApiFiles.Add('features\operations\executionProfiles\api\executionProfilesApi.ts')
[void]$featureApiFiles.Add('features\operations\operationalEvents\api\operationalEventsApi.ts')
[void]$featureApiFiles.Add('features\platform\capacityForecast\api\capacityForecastApi.ts')
[void]$featureApiFiles.Add('features\platform\costAnalytics\api\costAnalyticsApi.ts')

foreach ($relativePath in $featureApiFiles) {
    $filePath = Join-Path $sourceRoot $relativePath
    if (-not (Test-Path -Path $filePath -PathType Leaf)) {
        [void]$report.Add(('Skipped missing feature API file: `{0}`' -f $filePath))
        continue
    }

    $content = Get-Content -Path $filePath -Raw
    $content = $content -replace 'from\s+[''"]\.\/core\/adminApiClient[''"]', "from '../../../../api/core/adminApiClient'"
    $content = $content -replace 'from\s+[''"]\.\.\/\.\.\/\.\.\/\.\.\/api\/core\/client[''"]', "from '../../../../api/core/adminApiClient'"

    if ($content -match 'adminApiClient' -and $content -notmatch 'import\s+\{\s*adminApiClient\s*\}\s+from\s+[''"]\.\.\/\.\.\/\.\.\/\.\.\/api\/core\/adminApiClient[''"]') {
        $content = "import { adminApiClient } from '../../../../api/core/adminApiClient';`r`n" + $content
    }

    Set-Content -Path $filePath -Value $content -Encoding UTF8
    [void]$report.Add(('Normalized feature API client import: `{0}`' -f $filePath))
}

$docsDir = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BY Admin Web API and shared compatibility repair applied.'
