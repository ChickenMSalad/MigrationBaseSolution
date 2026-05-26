[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$AllowListPath,
    [string]$ReportPath = ".\artifacts\runtime-code-contract-findings.csv"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    param([string]$ProvidedRoot)

    if (-not [string]::IsNullOrWhiteSpace($ProvidedRoot)) {
        return (Resolve-Path -LiteralPath $ProvidedRoot).Path
    }

    if ($PSScriptRoot) {
        return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
    }

    return (Get-Location).Path
}

function Read-AllowList {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return @()
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("AllowListPath not found: {0}" -f $Path)
    }

    $json = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($json)) {
        return @()
    }

    $parsed = $json | ConvertFrom-Json
    if ($null -eq $parsed) {
        return @()
    }

    if ($parsed.PSObject.Properties.Name -contains 'allowedFindings') {
        return @($parsed.allowedFindings)
    }

    return @()
}

function Test-IsAllowed {
    param(
        [object]$Finding,
        [object[]]$AllowList
    )

    foreach ($entry in $AllowList) {
        $pathContains = $null
        $patternId = $null

        if ($entry.PSObject.Properties.Name -contains 'pathContains') { $pathContains = [string]$entry.pathContains }
        if ($entry.PSObject.Properties.Name -contains 'patternId') { $patternId = [string]$entry.patternId }

        $pathMatches = [string]::IsNullOrWhiteSpace($pathContains) -or ($Finding.Path.Replace('\', '/') -like ("*{0}*" -f $pathContains.Replace('\', '/')))
        $patternMatches = [string]::IsNullOrWhiteSpace($patternId) -or ($Finding.PatternId -eq $patternId)

        if ($pathMatches -and $patternMatches) {
            return $true
        }
    }

    return $false
}

$root = Get-RepoRoot -ProvidedRoot $RepoRoot
$inventoryScript = Join-Path $root 'tools\runtime\New-RuntimeCodeContractInventory.ps1'
if (-not (Test-Path -LiteralPath $inventoryScript)) {
    throw ("Inventory script not found: {0}" -f $inventoryScript)
}

$reportFullPath = Join-Path $root $ReportPath
& $inventoryScript -RepoRoot $root -OutputPath $ReportPath | Out-Host

$findings = @()
if (Test-Path -LiteralPath $reportFullPath) {
    $findings = @(Import-Csv -LiteralPath $reportFullPath)
}

$allowListFullPath = $AllowListPath
if (-not [string]::IsNullOrWhiteSpace($allowListFullPath) -and -not [System.IO.Path]::IsPathRooted($allowListFullPath)) {
    $allowListFullPath = Join-Path $root $allowListFullPath
}

$allowList = Read-AllowList -Path $allowListFullPath
$unallowed = New-Object System.Collections.Generic.List[object]

foreach ($finding in $findings) {
    if (-not (Test-IsAllowed -Finding $finding -AllowList $allowList)) {
        $unallowed.Add($finding) | Out-Null
    }
}

if ($unallowed.Count -gt 0) {
    Write-Host ''
    Write-Host 'Runtime code contract violations:'
    $unallowed | Format-Table PatternId, Path, LineNumber, Line -AutoSize | Out-String -Width 4096 | Write-Host
    throw ("Runtime code contract validation failed. Finding count: {0}. Report: {1}" -f $unallowed.Count, $reportFullPath)
}

Write-Host ("Runtime code contract validation passed. Report: {0}" -f $reportFullPath)
