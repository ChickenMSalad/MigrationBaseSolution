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
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$knownBad = New-Object System.Collections.Generic.List[string]
[void]$knownBad.Add('from "./core/adminApiClient"')
[void]$knownBad.Add("from './core/adminApiClient'")
[void]$knownBad.Add('from "../../../../api/core/client"')
[void]$knownBad.Add('adminApiClient.get(')
[void]$knownBad.Add('adminApiClient.post(')

$targetFiles = New-Object System.Collections.Generic.List[string]
$paths = @(
    'features\governance\notificationRouting\api\notificationRoutingApi.ts',
    'features\operations\failureRetry\api\failureRetryApi.ts',
    'features\operations\operationalEvents\api\operationalEventsApi.ts',
    'features\operations\executionProfiles\api\executionProfilesApi.ts',
    'features\platform\capacityForecast\api\capacityForecastApi.ts',
    'features\platform\costAnalytics\api\costAnalyticsApi.ts'
)
foreach ($relativePath in $paths) {
    $path = Join-Path $sourceRoot $relativePath
    if (Test-Path -Path $path -PathType Leaf) {
        [void]$targetFiles.Add($path)
    }
}

foreach ($path in $targetFiles) {
    $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    foreach ($bad in $knownBad) {
        if ($content.Contains($bad)) {
            throw ('Known bad API client pattern remains in {0}: {1}' -f $path, $bad)
        }
    }
    if ($content.Contains(".tsx'") -or $content.Contains('.tsx"')) {
        throw ('Extension-bearing TSX import remains in {0}' -f $path)
    }
}

$flatApiRoot = Join-Path $sourceRoot 'api'
if (Test-Path -Path $flatApiRoot -PathType Container) {
    $flatApiFiles = @(Get-ChildItem -Path $flatApiRoot -Filter '*.ts' -File -Recurse | Where-Object { $_.FullName -notlike '*\core\*' })
    foreach ($file in $flatApiFiles) {
        $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        $singleGenericMatches = @([System.Text.RegularExpressions.Regex]::Matches($content, 'apiPost<[A-Za-z0-9_]+>\('))
        if ($singleGenericMatches.Length -gt 0) {
            throw ('Single-generic apiPost call remains in {0}' -f $file.FullName)
        }
    }
}

Write-Host 'P10.2BW Admin Web API client compatibility repair validation passed.'
