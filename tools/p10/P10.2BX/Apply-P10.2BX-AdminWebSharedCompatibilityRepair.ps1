Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$coreRoot = Join-Path $srcRoot 'api\core'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BX-AdminWebSharedCompatibilityRepair.Report.md'

if (-not (Test-Path -Path $adminRoot -PathType Container)) { throw ('Admin Web root not found: {0}' -f $adminRoot) }
if (-not (Test-Path -Path $srcRoot -PathType Container)) { throw ('Admin Web src root not found: {0}' -f $srcRoot) }
if (-not (Test-Path -Path $coreRoot -PathType Container)) { New-Item -ItemType Directory -Path $coreRoot -Force | Out-Null }

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BX - Admin Web Shared Compatibility Repair')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add('')

$clientPath = Join-Path $coreRoot 'adminApiClient.ts'
$clientLines = New-Object 'System.Collections.Generic.List[string]'
[void]$clientLines.Add("import { AdminApiError } from './adminApiError';")
[void]$clientLines.Add('')
[void]$clientLines.Add("const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? '').replace(/\/$/, '');")
[void]$clientLines.Add('')
[void]$clientLines.Add('export type ApiRequestOptions = RequestInit & {')
[void]$clientLines.Add('  parseJson?: boolean;')
[void]$clientLines.Add('};')
[void]$clientLines.Add('')
[void]$clientLines.Add('async function tryReadBody(response: Response): Promise<unknown> {')
[void]$clientLines.Add("  const contentType = response.headers.get('content-type') ?? '';")
[void]$clientLines.Add("  if (contentType.includes('application/json')) {")
[void]$clientLines.Add('    try {')
[void]$clientLines.Add('      return await response.json();')
[void]$clientLines.Add('    } catch {')
[void]$clientLines.Add('      return undefined;')
[void]$clientLines.Add('    }')
[void]$clientLines.Add('  }')
[void]$clientLines.Add('')
[void]$clientLines.Add('  try {')
[void]$clientLines.Add('    return await response.text();')
[void]$clientLines.Add('  } catch {')
[void]$clientLines.Add('    return undefined;')
[void]$clientLines.Add('  }')
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('function mergeHeaders(options?: ApiRequestOptions, contentType?: string): HeadersInit {')
[void]$clientLines.Add('  return {')
[void]$clientLines.Add("    Accept: 'application/json',")
[void]$clientLines.Add('    ...(contentType ? { ''Content-Type'': contentType } : {}),')
[void]$clientLines.Add('    ...(options?.headers ?? {})')
[void]$clientLines.Add('  };')
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export async function apiRequest<TResponse = unknown>(')
[void]$clientLines.Add('  path: string,')
[void]$clientLines.Add('  options?: ApiRequestOptions')
[void]$clientLines.Add('): Promise<TResponse> {')
[void]$clientLines.Add('  const response = await fetch(`${API_BASE_URL}${path}`, {')
[void]$clientLines.Add('    ...options,')
[void]$clientLines.Add('    headers: mergeHeaders(options)')
[void]$clientLines.Add('  });')
[void]$clientLines.Add('')
[void]$clientLines.Add('  const responseBody = await tryReadBody(response);')
[void]$clientLines.Add('')
[void]$clientLines.Add('  if (!response.ok) {')
[void]$clientLines.Add('    const message =')
[void]$clientLines.Add("      typeof responseBody === 'object' &&")
[void]$clientLines.Add('      responseBody !== null &&')
[void]$clientLines.Add("      'error' in responseBody &&")
[void]$clientLines.Add("      typeof (responseBody as { error?: unknown }).error === 'string'")
[void]$clientLines.Add('        ? String((responseBody as { error: string }).error)')
[void]$clientLines.Add('        : `Request failed with status ${response.status}`;')
[void]$clientLines.Add('')
[void]$clientLines.Add('    throw new AdminApiError(message, response.status, response.statusText, responseBody);')
[void]$clientLines.Add('  }')
[void]$clientLines.Add('')
[void]$clientLines.Add('  if (options?.parseJson === false) {')
[void]$clientLines.Add('    return responseBody as TResponse;')
[void]$clientLines.Add('  }')
[void]$clientLines.Add('')
[void]$clientLines.Add('  return responseBody as TResponse;')
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export async function apiGet<TResponse = unknown>(path: string): Promise<TResponse> {')
[void]$clientLines.Add("  return apiRequest<TResponse>(path, { method: 'GET' });")
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export async function apiPost<TResponse = unknown, TRequest = unknown>(')
[void]$clientLines.Add('  path: string,')
[void]$clientLines.Add('  body?: TRequest,')
[void]$clientLines.Add('  options?: ApiRequestOptions')
[void]$clientLines.Add('): Promise<TResponse> {')
[void]$clientLines.Add('  const isFormOrBlob = body instanceof FormData || body instanceof Blob;')
[void]$clientLines.Add('  const requestOptions: ApiRequestOptions = {')
[void]$clientLines.Add('    ...options,')
[void]$clientLines.Add("    method: 'POST',")
[void]$clientLines.Add('    headers: mergeHeaders(options, isFormOrBlob ? undefined : ''application/json''),')
[void]$clientLines.Add('    body: isFormOrBlob ? (body as BodyInit) : JSON.stringify(body ?? {})')
[void]$clientLines.Add('  };')
[void]$clientLines.Add('  return apiRequest<TResponse>(path, requestOptions);')
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export async function apiPut<TResponse = unknown, TRequest = unknown>(')
[void]$clientLines.Add('  path: string,')
[void]$clientLines.Add('  body?: TRequest,')
[void]$clientLines.Add('  options?: ApiRequestOptions')
[void]$clientLines.Add('): Promise<TResponse> {')
[void]$clientLines.Add('  const requestOptions: ApiRequestOptions = {')
[void]$clientLines.Add('    ...options,')
[void]$clientLines.Add("    method: 'PUT',")
[void]$clientLines.Add("    headers: mergeHeaders(options, 'application/json'),")
[void]$clientLines.Add('    body: JSON.stringify(body ?? {})')
[void]$clientLines.Add('  };')
[void]$clientLines.Add('  return apiRequest<TResponse>(path, requestOptions);')
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export async function apiDelete<TResponse = void>(path: string): Promise<TResponse> {')
[void]$clientLines.Add("  return apiRequest<TResponse>(path, { method: 'DELETE', parseJson: false });")
[void]$clientLines.Add('}')
[void]$clientLines.Add('')
[void]$clientLines.Add('export const adminApiClient = {')
[void]$clientLines.Add('  get: apiGet,')
[void]$clientLines.Add('  post: apiPost,')
[void]$clientLines.Add('  put: apiPut,')
[void]$clientLines.Add('  delete: apiDelete')
[void]$clientLines.Add('};')
Set-Content -Path $clientPath -Value $clientLines -Encoding UTF8
[void]$report.Add(('- Rewrote compatibility API client: `{0}`' -f $clientPath))

$coreClientPath = Join-Path $coreRoot 'client.ts'
$coreClientLines = New-Object 'System.Collections.Generic.List[string]'
[void]$coreClientLines.Add("export { apiRequest, apiGet, apiPost, apiPut, apiDelete, adminApiClient } from './adminApiClient';")
Set-Content -Path $coreClientPath -Value $coreClientLines -Encoding UTF8
[void]$report.Add(('- Added legacy compatibility re-export: `{0}`' -f $coreClientPath))

$cardPath = Join-Path $srcRoot 'components\Card.tsx'
if (Test-Path -Path $cardPath -PathType Leaf) {
    $cardText = Get-Content -Path $cardPath -Raw
    if ($cardText -notmatch 'description\?:\s*string') {
        $cardText = $cardText -replace '\{\s*title,\s*subtitle,\s*action,\s*children\s*\}', '{ title, subtitle, description, action, children }'
        $cardText = $cardText -replace 'subtitle\?:\s*string;\s*action\?:', 'subtitle?: string; description?: string; action?:'
        Set-Content -Path $cardPath -Value $cardText -Encoding UTF8
        [void]$report.Add('- Added Card description prop compatibility.')
    } else {
        [void]$report.Add('- Card description prop compatibility already present.')
    }
} else {
    throw ('Card component not found: {0}' -f $cardPath)
}

$loadingPath = Join-Path $srcRoot 'components\LoadingError.tsx'
if (Test-Path -Path $loadingPath -PathType Leaf) {
    $loadingText = Get-Content -Path $loadingPath -Raw
    if ($loadingText -notmatch 'title\?:\s*string') {
        $loadingText = $loadingText -replace 'type\s+LoadingErrorProps\s*=\s*\{', 'type LoadingErrorProps = { title?: string;'
        $loadingText = $loadingText -replace '\{\s*loading,\s*error,\s*message,\s*onRetry\s*\}', '{ title, loading, error, message, onRetry }'
        Set-Content -Path $loadingPath -Value $loadingText -Encoding UTF8
        [void]$report.Add('- Added LoadingError title prop compatibility.')
    } else {
        [void]$report.Add('- LoadingError title prop compatibility already present.')
    }
} else {
    throw ('LoadingError component not found: {0}' -f $loadingPath)
}

$operationalEventsPage = Join-Path $srcRoot 'features\operations\operationalEvents\pages\OperationalEvents.tsx'
if (Test-Path -Path $operationalEventsPage -PathType Leaf) {
    $pageText = Get-Content -Path $operationalEventsPage -Raw
    $updatedPageText = $pageText.Replace("from '../components/Card'", "from '../../../../components/Card'")
    $updatedPageText = $updatedPageText.Replace('from "../components/Card"', 'from "../../../../components/Card"')
    $updatedPageText = $updatedPageText.Replace("from '../components/LoadingError'", "from '../../../../components/LoadingError'")
    $updatedPageText = $updatedPageText.Replace('from "../components/LoadingError"', 'from "../../../../components/LoadingError"')
    if ($updatedPageText -ne $pageText) {
        Set-Content -Path $operationalEventsPage -Value $updatedPageText -Encoding UTF8
        [void]$report.Add('- Normalized OperationalEvents component imports.')
    } else {
        [void]$report.Add('- OperationalEvents component imports already normalized or not present.')
    }
} else {
    [void]$report.Add('- OperationalEvents page not present; skipped component import normalization.')
}

[void]$report.Add('')
[void]$report.Add('P10.2BX applied.')
$reportDir = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDir -PathType Container)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BX Admin Web shared compatibility repair applied.'
