Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')).Path
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BY-AdminWebApiAndSharedCompatibilityRepair.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

$requiredFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$requiredFiles.Add((Join-Path $sourceRoot 'api\core\adminApiClient.ts'))
[void]$requiredFiles.Add((Join-Path $sourceRoot 'api\core\client.ts'))
[void]$requiredFiles.Add((Join-Path $sourceRoot 'components\Card.tsx'))
[void]$requiredFiles.Add((Join-Path $sourceRoot 'components\LoadingError.tsx'))
[void]$requiredFiles.Add((Join-Path $sourceRoot 'api\preflight.ts'))
[void]$requiredFiles.Add($reportPath)

foreach ($filePath in $requiredFiles) {
    if (-not (Test-Path -Path $filePath -PathType Leaf)) {
        throw ('Expected file missing: {0}' -f $filePath)
    }
}

$adminApiClient = Get-Content -Path (Join-Path $sourceRoot 'api\core\adminApiClient.ts') -Raw
if ($adminApiClient -notmatch 'export\s+async\s+function\s+apiPost') {
    throw 'apiPost export missing from adminApiClient.ts.'
}
if ($adminApiClient -notmatch 'export\s+async\s+function\s+apiDelete') {
    throw 'apiDelete export missing from adminApiClient.ts.'
}
if ($adminApiClient -notmatch 'export\s+const\s+adminApiClient') {
    throw 'adminApiClient object export missing from adminApiClient.ts.'
}

$coreClient = Get-Content -Path (Join-Path $sourceRoot 'api\core\client.ts') -Raw
if ($coreClient -notmatch 'export\s+\*\s+from\s+[''"]\.\/adminApiClient[''"]') {
    throw 'Compatibility core/client.ts re-export is missing.'
}

$card = Get-Content -Path (Join-Path $sourceRoot 'components\Card.tsx') -Raw
if ($card -notmatch 'description\?:\s*string') {
    throw 'Card/EmptyState compatibility description prop missing.'
}
if ($card -notmatch 'value\?:\s*string') {
    throw 'StatusPill compatibility value prop missing.'
}

$loadingError = Get-Content -Path (Join-Path $sourceRoot 'components\LoadingError.tsx') -Raw
if ($loadingError -notmatch 'title\?:\s*string') {
    throw 'LoadingError compatibility title prop missing.'
}

$featureApiFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$featureApiFiles.Add((Join-Path $sourceRoot 'features\operations\executionProfiles\api\executionProfilesApi.ts'))
[void]$featureApiFiles.Add((Join-Path $sourceRoot 'features\operations\operationalEvents\api\operationalEventsApi.ts'))
[void]$featureApiFiles.Add((Join-Path $sourceRoot 'features\platform\capacityForecast\api\capacityForecastApi.ts'))
[void]$featureApiFiles.Add((Join-Path $sourceRoot 'features\platform\costAnalytics\api\costAnalyticsApi.ts'))

foreach ($filePath in $featureApiFiles) {
    if (-not (Test-Path -Path $filePath -PathType Leaf)) {
        continue
    }

    $content = Get-Content -Path $filePath -Raw
    if ($content -match 'adminApiClient' -and $content -notmatch 'from\s+[''"]\.\.\/\.\.\/\.\.\/\.\.\/api\/core\/adminApiClient[''"]') {
        throw ('Feature API file does not import canonical adminApiClient: {0}' -f $filePath)
    }
    if ($content -match 'from\s+[''"]\.\/core\/adminApiClient[''"]') {
        throw ('Feature API file still imports local missing core client: {0}' -f $filePath)
    }
}

$compiledSourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include *.ts,*.tsx | Where-Object {
    $fullName = $_.FullName
    $fullName -notmatch '\\reference\\' -and
    $fullName -notmatch '\\node_modules\\' -and
    $fullName -notmatch '\\dist\\'
})

foreach ($sourceFile in $compiledSourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    if ($content -match 'from\s+[''"][^''"]+\.tsx[''"]') {
        throw ('Extension-bearing .tsx import found in compiled source: {0}' -f $sourceFile.FullName)
    }
    if ($content -match 'from\s+[''"][^''"]*reference[^''"]*[''"]') {
        throw ('Compiled source imports reference material: {0}' -f $sourceFile.FullName)
    }
}

Write-Host 'P10.2BY Admin Web API and shared compatibility repair validation passed.'
