[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

$repoFullPath = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoFullPath 'artifacts\runtime-closeout'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot 'runtime-stabilization-closeout.md'
}

$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-Path $repoFullPath $outputFullPath
}

$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$expectedFiles = @(
    'docs\p7\P7.10I-Runtime-Stabilization-Closeout.md',
    'docs\operations\runtime-stabilization-closeout.md',
    'config-samples\runtime-stabilization-closeout.sample.json'
)

$lines = New-Object System.Collections.ArrayList
[void]$lines.Add('# Runtime Stabilization Closeout Report')
[void]$lines.Add('')
[void]$lines.Add(('- Generated UTC: {0}' -f ([DateTimeOffset]::UtcNow.ToString('o'))))
[void]$lines.Add(('- Repository root: {0}' -f $repoFullPath))
[void]$lines.Add('')
[void]$lines.Add('| File | Present |')
[void]$lines.Add('| --- | --- |')

foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoFullPath $relativePath
    $present = Test-Path -LiteralPath $fullPath
    [void]$lines.Add(('| `{0}` | {1} |' -f $relativePath, $present))
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime stabilization closeout report written to {0}' -f $outputFullPath)
