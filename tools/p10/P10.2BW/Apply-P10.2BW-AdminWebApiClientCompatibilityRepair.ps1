Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BW-AdminWebApiClientCompatibilityRepair.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BW - Admin Web API Client Compatibility Repair')
[void]$report.Add('')
[void]$report.Add(('Admin Web source root: `{0}`' -f $sourceRoot))
[void]$report.Add('')
[void]$report.Add('## Changes')

function Get-TextFileContent($path) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        return $null
    }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function Set-TextFileContent($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, (New-Object System.Text.UTF8Encoding($false)))
}

function Update-TextFile($relativePath, $searchText, $replacementText, $label) {
    $path = Join-Path $sourceRoot $relativePath
    $content = Get-TextFileContent $path
    if ($null -eq $content) {
        [void]$report.Add(('- Skipped missing {0}: `{1}`' -f $label, $relativePath))
        return
    }

    if ($content.Contains($replacementText)) {
        [void]$report.Add(('- Already normalized {0}: `{1}`' -f $label, $relativePath))
        return
    }

    if (-not $content.Contains($searchText)) {
        [void]$report.Add(('- No legacy text found for {0}: `{1}`' -f $label, $relativePath))
        return
    }

    $updated = $content.Replace($searchText, $replacementText)
    if ($updated -ne $content) {
        Set-TextFileContent $path $updated
        [void]$report.Add(('- Updated {0}: `{1}`' -f $label, $relativePath))
    }
}

Update-TextFile 'features\governance\notificationRouting\api\notificationRoutingApi.ts' "from './core/adminApiClient'" "from '../../../../api/core/adminApiClient'" 'Notification Routing API client import'
Update-TextFile 'features\operations\failureRetry\api\failureRetryApi.ts' 'from "./core/adminApiClient"' 'from "../../../../api/core/adminApiClient"' 'Failure Retry API client import'
Update-TextFile 'features\operations\operationalEvents\api\operationalEventsApi.ts' 'import { adminApiClient } from "./core/adminApiClient";' 'import { apiGet } from "../../../../api/core/adminApiClient";' 'Operational Events API client import'
Update-TextFile 'features\operations\executionProfiles\api\executionProfilesApi.ts' 'import { adminApiClient } from "../../../../api/core/client";' 'import { apiGet, apiPost } from "../../../../api/core/adminApiClient";' 'Execution Profiles API client import'
Update-TextFile 'features\platform\capacityForecast\api\capacityForecastApi.ts' 'import { adminApiClient } from "../../../../api/core/adminApiClient";' 'import { apiGet } from "../../../../api/core/adminApiClient";' 'Capacity Forecast API client import'
Update-TextFile 'features\platform\costAnalytics\api\costAnalyticsApi.ts' 'import { adminApiClient } from "../../../../api/core/adminApiClient";' 'import { apiGet } from "../../../../api/core/adminApiClient";' 'Cost Analytics API client import'

$apiFiles = @(
    'features\operations\operationalEvents\api\operationalEventsApi.ts',
    'features\operations\executionProfiles\api\executionProfilesApi.ts',
    'features\platform\capacityForecast\api\capacityForecastApi.ts',
    'features\platform\costAnalytics\api\costAnalyticsApi.ts'
)
foreach ($relativePath in $apiFiles) {
    $path = Join-Path $sourceRoot $relativePath
    $content = Get-TextFileContent $path
    if ($null -eq $content) { continue }
    $updated = $content
    $updated = $updated.Replace('adminApiClient.get(', 'apiGet(')
    $updated = $updated.Replace('adminApiClient.post(', 'apiPost(')
    if ($updated -ne $content) {
        Set-TextFileContent $path $updated
        [void]$report.Add(('- Replaced adminApiClient method calls in `{0}`' -f $relativePath))
    }
}

$flatApiRoot = Join-Path $sourceRoot 'api'
if (Test-Path -Path $flatApiRoot -PathType Container) {
    $flatApiFiles = @(Get-ChildItem -Path $flatApiRoot -Filter '*.ts' -File -Recurse | Where-Object { $_.FullName -notlike '*\core\*' })
    foreach ($file in $flatApiFiles) {
        $content = Get-TextFileContent $file.FullName
        if ($null -eq $content) { continue }
        $updated = $content
        $updated = [System.Text.RegularExpressions.Regex]::Replace(
            $updated,
            'apiPost<([A-Za-z0-9_]+)>\(',
            'apiPost<unknown, $1>('
        )
        if ($updated -ne $content) {
            Set-TextFileContent $file.FullName $updated
            $relative = $file.FullName.Substring($sourceRoot.Length + 1)
            [void]$report.Add(('- Added request generic to apiPost call(s) in `{0}`' -f $relative))
        }
    }
}

$artifactBridge = Join-Path $sourceRoot 'api\artifactStorageBridge.ts'
$artifactContent = Get-TextFileContent $artifactBridge
if ($null -ne $artifactContent) {
    $updated = $artifactContent
    $updated = $updated.Replace("import { apiDelete, apiGet, apiPost } from './core/adminApiClient';", "import { apiDelete, apiGet, apiRequest } from './core/adminApiClient';")
    $oldUpload = @'
return apiPost(
    path,
    content,
    {
      headers: { 'Content-Type': contentType }
    }
  );
'@
    $newUpload = @'
return apiRequest<ArtifactStorageBridgeUploadResponse>(path, {
    method: 'POST',
    headers: { 'Content-Type': contentType },
    body: content
  });
'@
    if ($updated.Contains($oldUpload)) {
        $updated = $updated.Replace($oldUpload, $newUpload)
    }
    $oldDelete = @'
return apiDelete(path);
'@
    $newDelete = @'
await apiDelete(path);
  return {
    deleted: true,
    artifactKind,
    artifactId,
    fileName,
    workspaceId: ''
  };
'@
    if ($updated.Contains($oldDelete)) {
        $updated = $updated.Replace($oldDelete, $newDelete)
    }
    if ($updated -ne $artifactContent) {
        Set-TextFileContent $artifactBridge $updated
        [void]$report.Add('- Updated artifact storage bridge for current adminApiClient helpers.')
    }
}

$docsDir = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}
[System.IO.File]::WriteAllLines($reportPath, $report, (New-Object System.Text.UTF8Encoding($false)))
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BW Admin Web API client compatibility repair applied.'
