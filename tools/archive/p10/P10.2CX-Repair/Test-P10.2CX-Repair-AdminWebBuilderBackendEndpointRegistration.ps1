Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script location.'
        }
        $solution = Join-Path $current 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $solution) {
            return $current
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
        }
        $current = $parent
    }
}

function Test-IgnoredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $true }
    if ($Path -match '\\bin\\') { return $true }
    if ($Path -match '\\obj\\') { return $true }
    if ($Path -match '\\node_modules\\') { return $true }
    if ($Path -match '\\.git\\') { return $true }
    if ($Path -match '\\dist\\') { return $true }
    return $false
}

function Read-TextFileSafe {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    return [System.IO.File]::ReadAllText($Path)
}

$repoRoot = Get-RepoRoot
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CX-Repair-AdminWebBuilderBackendEndpointRegistration.md'
if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
$reportContent = Read-TextFileSafe -Path $reportPath
if (-not $reportContent.Contains('Admin API host discovery')) {
    throw 'Report does not contain Admin API host discovery section.'
}
if (-not $reportContent.Contains('Taxonomy Builder')) {
    throw 'Report does not contain Taxonomy Builder section.'
}
if (-not $reportContent.Contains('Mapping Builder')) {
    throw 'Report does not contain Mapping Builder section.'
}

$scriptPaths = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptPaths.Add((Join-Path $PSScriptRoot 'Apply-P10.2CX-Repair-AdminWebBuilderBackendEndpointRegistration.ps1'))
[void]$scriptPaths.Add((Join-Path $PSScriptRoot 'Test-P10.2CX-Repair-AdminWebBuilderBackendEndpointRegistration.ps1'))
foreach ($scriptPath in $scriptPaths) {
    $content = Read-TextFileSafe -Path $scriptPath
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw ('Script content was empty: {0}' -f $scriptPath)
    }
    [void][scriptblock]::Create($content)
}

$programFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter 'Program.cs' | Where-Object { -not (Test-IgnoredPath -Path $_.FullName) }
if ($programFiles.Count -eq 0) {
    throw 'No Program.cs files were found in repository.'
}

Write-Host 'P10.2CX Repair Admin Web builder backend endpoint registration test passed.'
