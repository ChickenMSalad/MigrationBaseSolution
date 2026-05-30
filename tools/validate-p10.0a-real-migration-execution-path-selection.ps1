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

$requiredFiles = @(
    'docs\p10\P10.0A-Real-Migration-Execution-Path-Selection.md',
    'config-samples\p10-real-migration-execution-candidate.sample.json',
    'profiles\manifests\localstorage-platform-smoke.csv',
    'samples\localstorage\source\animals\cats\ls-cat-001.txt',
    'samples\localstorage\source\animals\dogs\ls-dog-001.txt',
    'samples\localstorage\source\documents\ls-doc-001.txt',
    'tools\runtime\Test-P100RealMigrationExecutionCandidate.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.0A file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-P100RealMigrationExecutionCandidate.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref] $parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$candidateScript = Join-Path $repoRoot 'tools\runtime\Test-P100RealMigrationExecutionCandidate.ps1'
& $candidateScript -RepoRoot $repoRoot

Write-Host 'P10.0A real migration execution path selection validation passed.'
