[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-stale-artifact-quarantine.sample.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path $RepoRoot 'artifacts\runtime-cleanup\stale-artifact-quarantine-report.md')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRootFullPath = (Resolve-Path $RepoRoot).Path
$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-Path $repoRootFullPath $outputFullPath
}

$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$inventoryScript = Join-Path $PSScriptRoot 'New-RuntimeStaleArtifactInventory.ps1'
$items = @(& $inventoryScript -RepoRoot $repoRootFullPath -ConfigurationPath $ConfigurationPath)

$lines = @()
$lines += '# Runtime Stale Artifact Quarantine Report'
$lines += ''
$lines += ('- Generated UTC: {0}' -f [DateTimeOffset]::UtcNow.ToString('o'))
$lines += ('- Repository root: {0}' -f $repoRootFullPath)
$lines += ('- Candidate count: {0}' -f @($items).Count)
$lines += ''
$lines += 'This report is advisory only. Review each candidate before deleting, moving, or archiving.'
$lines += ''
$lines += '| Path | Bytes | LastWriteTimeUtc | Matched Terms |'
$lines += '| --- | ---: | --- | --- |'

foreach ($item in $items) {
    $matchedTerms = $item.MatchedTerms
    if ([string]::IsNullOrWhiteSpace($matchedTerms)) {
        $matchedTerms = 'path-only candidate'
    }

    $lines += ('| `{0}` | {1} | {2} | {3} |' -f $item.Path, $item.Length, $item.LastWriteTimeUtc, $matchedTerms)
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime stale artifact quarantine report written to {0}' -f $outputFullPath)
