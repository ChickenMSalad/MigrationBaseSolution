[CmdletBinding()]
param([string]$RepoRoot)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param([string]$RequestedRoot)

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        return (Resolve-Path -LiteralPath $RequestedRoot).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
    }

    return (Resolve-Path -LiteralPath (Get-Location)).Path
}

function Assert-FileExists {
    param([string]$Root, [string]$RelativePath)

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ("Missing expected file: {0}" -f $RelativePath)
    }
}

function Assert-TextContains {
    param([string]$Root, [string]$RelativePath, [string[]]$Terms)

    $path = Join-Path $Root $RelativePath
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($term in $Terms) {
        if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ("{0} is missing expected term: {1}" -f $RelativePath, $term)
        }
    }
}

$root = Resolve-RepoRoot -RequestedRoot $RepoRoot

$requiredFiles = @(
    'docs\p7\P7.8I-Cleanup-Package-Quality-Gates.md',
    'config-samples\runtime-script-quality-rules.sample.json',
    'tools\runtime\Test-RuntimeScriptQuality.ps1',
    'tools\runtime\Test-RuntimeCleanupSet.ps1',
    'tools\validate-p7.8i-cleanup-package-quality-gates.ps1'
)

foreach ($requiredFile in $requiredFiles) {
    Assert-FileExists -Root $root -RelativePath $requiredFile
}

Assert-TextContains -Root $root -RelativePath 'docs\p7\P7.8I-Cleanup-Package-Quality-Gates.md' -Terms @('Purpose', 'Usage', 'Guardrail')
Assert-TextContains -Root $root -RelativePath 'tools\runtime\Test-RuntimeScriptQuality.ps1' -Terms @('PS001', 'scopedVariablePattern', 'FailOnIssue')
Assert-TextContains -Root $root -RelativePath 'tools\runtime\Test-RuntimeCleanupSet.ps1' -Terms @('SetName', 'docs\p7', 'FailOnIssue')

$qualityScript = Join-Path $root 'tools\runtime\Test-RuntimeScriptQuality.ps1'
$issues = @(& $qualityScript -RepoRoot $root -RelativePaths @('tools') | Where-Object { $_.Path -like '*P7.8I*' -or $_.Path -like '*RuntimeScriptQuality*' -or $_.Path -like '*RuntimeCleanupSet*' })
if ($issues.Count -gt 0) {
    $summary = ($issues | ForEach-Object { '{0}:{1} {2}' -f $_.Path, $_.LineNumber, $_.Message }) -join [Environment]::NewLine
    throw ("P7.8I script quality validation failed.{0}{1}" -f [Environment]::NewLine, $summary)
}

Write-Host 'P7.8I cleanup package quality gates validation passed.'
