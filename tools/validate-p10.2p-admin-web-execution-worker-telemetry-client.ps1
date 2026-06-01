[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($repoRoot, $normalized)
}

$checks = @(
    [pscustomobject]@{
        Path = 'docs/p10/P10.2P-Admin-Web-Execution-Worker-Telemetry-Client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'execution-workers/summary')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-execution-worker-telemetry-client.md'
        Terms = @('Execution Worker Telemetry', 'feature-source', 'execution-workers/summary')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-execution-worker-telemetry-client.sample.json'
        Terms = @('canonicalAdminUiPath', 'featureSourcePath', '/api/operational/execution-workers/summary')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/executionWorkerTelemetry.ts'
        Terms = @('ExecutionWorkerHeartbeatRecord', 'ExecutionWorkerTelemetrySummary', 'activeLeaseCount')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/executionWorkerTelemetryApi.ts'
        Terms = @('executionWorkerTelemetryApi', '/api/operational/execution-workers/summary', 'apiGet')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/ExecutionWorkerTelemetry.tsx'
        Terms = @('Execution worker telemetry', 'executionWorkerTelemetryApi', 'staleAfterSeconds')
    }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']
    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Validator check entry has an empty Path.'
    }

    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $relativePath)
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    foreach ($term in @($termsProperty.Value)) {
        $termText = [string]$term
        if ([string]::IsNullOrWhiteSpace($termText)) {
            throw ('Validator check entry has an empty expected term for file: {0}' -f $relativePath)
        }
        if ($text.IndexOf($termText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $relativePath, $termText)
        }
    }
}

$forbiddenAppPath = Join-RepoPath -RelativePath 'apps/migration-admin-ui'
if (-not (Test-Path -LiteralPath $forbiddenAppPath -PathType Container)) {
    throw 'Feature-source Admin UI path is missing; consolidation tracking expects apps/migration-admin-ui to remain as reference until fully migrated.'
}

Write-Host 'P10.2P Admin Web execution worker telemetry client validation passed.'
