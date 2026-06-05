Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath '..\..\..')
    return $candidate.Path
}

function Test-PathSegment {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Segment
    )

    $parts = @($PathValue -split '[\\/]')
    foreach ($part in $parts) {
        if ([string]::Equals($part, $Segment, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    if (-not (Test-Path -Path $PathValue -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $PathValue)
    }
}

function Assert-TextPresent {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Text
    )

    Assert-FileExists -PathValue $PathValue
    $content = Get-Content -Path $PathValue -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected text missing in {0}: {1}' -f $PathValue, $Text)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath 'src\Admin\Migration.Admin.Web\src'
$reportPath = Join-Path -Path $repoRoot -ChildPath 'docs\P10\P10.2AT-Repair2-CanonicalImportSweepReport.md'
$toolRoot = Join-Path -Path $repoRoot -ChildPath 'tools\p10\P10.2AT-Repair2'

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source directory was not found: {0}' -f $adminSrc)
}

Assert-TextPresent -PathValue $reportPath -Text '# P10.2AT Repair2 - Canonical Admin Web Import Sweep Report'
Assert-TextPresent -PathValue $reportPath -Text 'Checked relative imports:'
Assert-TextPresent -PathValue $reportPath -Text 'Unresolved relative imports:'

$scriptFiles = @(
    Get-ChildItem -Path $toolRoot -File -Filter '*.ps1' |
        Sort-Object -Property FullName
)

if ($scriptFiles.Length -eq 0) {
    throw ('No PowerShell scripts were found in: {0}' -f $toolRoot)
}

foreach ($scriptFile in $scriptFiles) {
    if (-not (Test-Path -Path $scriptFile.FullName -PathType Leaf)) {
        throw ('Script disappeared during validation: {0}' -f $scriptFile.FullName)
    }

    $content = Get-Content -Path $scriptFile.FullName -Raw

    if ($content.IndexOf('$matches.Count', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Unsafe StrictMode match count pattern found in {0}' -f $scriptFile.FullName)
    }

    if ($content.IndexOf('$Label:', [System.StringComparison]::Ordinal) -ge 0 -or $content.IndexOf('$Path:', [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Unsafe variable-colon interpolation pattern found in {0}' -f $scriptFile.FullName)
    }

    if ($content.IndexOf('@(' + [Environment]::NewLine + '    @(', [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Nested validation array pattern found in {0}' -f $scriptFile.FullName)
    }
}

Write-Host 'P10.2AT Repair2 validation passed.'
