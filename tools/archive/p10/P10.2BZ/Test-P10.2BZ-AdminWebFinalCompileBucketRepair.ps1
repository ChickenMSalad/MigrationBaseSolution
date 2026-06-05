Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$srcRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BZ-AdminWebFinalCompileBucketRepair.Report.md'

if (-not (Test-Path -Path $srcRoot -PathType Container)) {
    throw ('Admin Web src root was not found: {0}' -f $srcRoot)
}

$checks = @(
    [pscustomobject]@{ Path = Join-Path $srcRoot 'api\artifactManifestIndex.ts'; MustContain = @('apiPost<Record<string, never>, ArtifactManifestIndexProbeResponse>') },
    [pscustomobject]@{ Path = Join-Path $srcRoot 'api\artifactStorage.ts'; MustContain = @('apiPost<Record<string, never>, ArtifactStorageProbeResponse>') },
    [pscustomobject]@{ Path = Join-Path $srcRoot 'api\artifactStorageBridge.ts'; MustContain = @('apiPost<string | Blob, ArtifactStorageBridgeUploadResponse>', 'apiDelete<ArtifactStorageBridgeDeleteResponse>') },
    [pscustomobject]@{ Path = Join-Path $srcRoot 'api\cloudBinaryStorage.ts'; MustContain = @('apiPost<Record<string, never>, CloudBinaryStorageProbeResponse>') },
    [pscustomobject]@{ Path = Join-Path $srcRoot 'features\operations\runtimeDashboard\pages\RuntimeRunDetail.tsx'; MustContain = @('const run = detail.run;', 'run.runName ?? run.runKey ?? run.runId', 'StatusPill status={run.status ?? undefined}') }
)

foreach ($check in $checks) {
    if (-not (Test-Path -Path $check.Path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $check.Path)
    }

    $content = [System.IO.File]::ReadAllText($check.Path)
    foreach ($term in $check.MustContain) {
        if ($content.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
            throw ('Expected text missing in {0}: {1}' -f $check.Path, $term)
        }
    }

    if ($content.IndexOf('.tsx"', [System.StringComparison]::Ordinal) -ge 0 -or $content.IndexOf(".tsx'", [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Extension-bearing TSX import found in {0}' -f $check.Path)
    }
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

Write-Host 'P10.2BZ validation passed.'
