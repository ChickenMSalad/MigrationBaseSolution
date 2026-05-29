[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath,

    [Parameter(Mandatory = $false)]
    [string] $NoOpSmokeRunId
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}

if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $repoRoot 'config-samples\runtime-post-clean-baseline-review.sample.json'
}

if (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $repoRoot $ConfigurationPath
}

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot 'artifacts\runtime-post-clean-baseline'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot 'post-clean-baseline-review.md'
}

if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json

$baselineName = 'unknown'
if ($null -ne $config.PSObject.Properties['baselineName']) {
    $baselineName = [string]$config.baselineName
}

$lines = New-Object System.Collections.ArrayList
[void]$lines.Add('# Runtime Post-Clean Baseline Review')
[void]$lines.Add('')
[void]$lines.Add(('- Baseline: {0}' -f $baselineName))
[void]$lines.Add(('- Generated UTC: {0:o}' -f [DateTimeOffset]::UtcNow))
if (-not [string]::IsNullOrWhiteSpace($NoOpSmokeRunId)) {
    [void]$lines.Add(('- NoOp smoke RunId: `{0}`' -f $NoOpSmokeRunId))
}
[void]$lines.Add('')
[void]$lines.Add('## Required evidence')
if ($null -ne $config.PSObject.Properties['requiredEvidence']) {
    foreach ($item in @($config.requiredEvidence)) {
        [void]$lines.Add(('- [ ] {0}' -f $item))
    }
}
[void]$lines.Add('')
[void]$lines.Add('## Allowed remaining MIGRATION_* keys')
if ($null -ne $config.PSObject.Properties['allowedRemainingMigrationPrefixedKeys']) {
    foreach ($item in @($config.allowedRemainingMigrationPrefixedKeys)) {
        [void]$lines.Add(('- `{0}`' -f $item))
    }
}
[void]$lines.Add('')
[void]$lines.Add('## Next decisions')
[void]$lines.Add('- Decide whether to rebuild local DB from the canonical baseline or migrate it forward.')
[void]$lines.Add('- Decide when legacy GUID-era objects can be retired from dev cloud.')
[void]$lines.Add('- Decide when CI gate moves from sample workflow to active workflow.')

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
Write-Host ('Runtime post-clean baseline review written to {0}' -f $OutputPath)
