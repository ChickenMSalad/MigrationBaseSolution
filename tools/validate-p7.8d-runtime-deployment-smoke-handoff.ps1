[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = (Get-Location).Path
    }

    $current = Get-Item -LiteralPath $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path $current.FullName 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $candidate) {
            return $current.FullName
        }

        $parent = Split-Path -Parent $current.FullName
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.FullName) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate repo root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required file is missing: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern,
        [Parameter(Mandatory = $true)][string] $Description
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        throw "Validation failed for $Path. Missing: $Description"
    }
}

$repoRoot = Get-RepoRoot

$requiredFiles = @(
    'docs\p7\P7.8D-Runtime-Deployment-Smoke-Handoff.md',
    'config-samples\runtime-smoke-job-definition.sample.json',
    'database\sql\p7\010_runtime_smoke_job_seed.sql',
    'tools\runtime\Publish-RuntimeWorker.ps1',
    'tools\runtime\Invoke-RuntimeSmokeEnqueue.ps1',
    'tools\runtime\Test-RuntimeSmokeState.ps1',
    'tools\runtime\Invoke-RuntimeDeploymentSmoke.ps1',
    'tools\validate-p7.8d-runtime-deployment-smoke-handoff.ps1'
)

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$payloadPath = Join-Path $repoRoot 'config-samples\runtime-smoke-job-definition.sample.json'
try {
    $payload = (Get-Content -LiteralPath $payloadPath -Raw) | ConvertFrom-Json
}
catch {
    throw "Smoke payload sample is not valid JSON: $payloadPath"
}

foreach ($propertyName in @('jobName', 'sourceType', 'targetType', 'manifestType', 'mappingProfilePath')) {
    if (-not $payload.PSObject.Properties[$propertyName]) {
        throw "Smoke payload sample is missing required property: $propertyName"
    }
}

$sqlPath = Join-Path $repoRoot 'database\sql\p7\010_runtime_smoke_job_seed.sql'
Assert-FileContains -Path $sqlPath -Pattern 'migration\.WorkItems' -Description 'canonical migration.WorkItems usage'
Assert-FileContains -Path $sqlPath -Pattern 'MigrationJobDefinition' -Description 'MigrationJobDefinition work type'
Assert-FileContains -Path $sqlPath -Pattern 'ISJSON' -Description 'JSON validation'

$runtimeScriptRelativePaths = @(
    'tools\runtime\Publish-RuntimeWorker.ps1',
    'tools\runtime\Invoke-RuntimeSmokeEnqueue.ps1',
    'tools\runtime\Test-RuntimeSmokeState.ps1',
    'tools\runtime\Invoke-RuntimeDeploymentSmoke.ps1'
)

foreach ($psRelativePath in $requiredFiles | Where-Object { $_ -like '*.ps1' }) {
    $psPath = Join-Path $repoRoot $psRelativePath
    $text = Get-Content -LiteralPath $psPath -Raw
    $unsafeColonPattern = '\$' + '[A-Za-z_][A-Za-z0-9_]*:'
    if ($text -match $unsafeColonPattern) {
        throw "PowerShell contains unsafe colon interpolation: $psPath"
    }
}

$fragileBinObjPattern = [regex]::Escape('\bin\') + '|' + [regex]::Escape('\obj\')
foreach ($psRelativePath in $runtimeScriptRelativePaths) {
    $psPath = Join-Path $repoRoot $psRelativePath
    $text = Get-Content -LiteralPath $psPath -Raw
    if ($text -match $fragileBinObjPattern) {
        throw "Runtime script contains fragile bin/obj path matching text: $psPath"
    }
}

Write-Host 'P7.8D runtime deployment smoke handoff validation passed.'
