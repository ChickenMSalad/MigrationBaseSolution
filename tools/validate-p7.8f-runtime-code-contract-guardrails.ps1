[CmdletBinding()]
param(
    [string]$RepoRoot
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    param([string]$ProvidedRoot)

    if (-not [string]::IsNullOrWhiteSpace($ProvidedRoot)) {
        return (Resolve-Path -LiteralPath $ProvidedRoot).Path
    }

    if ($PSScriptRoot) {
        return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ("Required file is missing: {0}" -f $Path)
    }
}

function Assert-FileContains {
    param(
        [string]$Path,
        [string]$ExpectedText
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($ExpectedText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ("Required text not found in {0}: {1}" -f $Path, $ExpectedText)
    }
}

function Assert-NoOldDefaultInAddedFiles {
    param([string[]]$Paths)

    $forbidden = @(
        'WorkItemsTableName { get; set; } = "MigrationWorkItems"',
        'SchemaName { get; set; } = "dbo"'
    )

    foreach ($path in $Paths) {
        $content = Get-Content -LiteralPath $path -Raw
        foreach ($text in $forbidden) {
            if ($content.IndexOf($text, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw ("Added P7.8F file contains an old runtime default: {0}" -f $path)
            }
        }
    }
}

$root = Get-RepoRoot -ProvidedRoot $RepoRoot

$requiredFiles = @(
    'docs\p7\P7.8F-Runtime-Code-Contract-Guardrails.md',
    'config-samples\runtime-code-contract.allowlist.sample.json',
    'tools\runtime\New-RuntimeCodeContractInventory.ps1',
    'tools\runtime\Test-RuntimeCodeContract.ps1'
)

$fullPaths = @()
foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $root $relativePath
    Assert-FileExists -Path $fullPath
    $fullPaths += $fullPath
}

Assert-FileContains -Path (Join-Path $root 'docs\p7\P7.8F-Runtime-Code-Contract-Guardrails.md') -ExpectedText 'migration.WorkItems'
Assert-FileContains -Path (Join-Path $root 'docs\p7\P7.8F-Runtime-Code-Contract-Guardrails.md') -ExpectedText 'migration.ManifestRows'
Assert-FileContains -Path (Join-Path $root 'tools\runtime\New-RuntimeCodeContractInventory.ps1') -ExpectedText 'GuidWorkItemId'
Assert-FileContains -Path (Join-Path $root 'tools\runtime\Test-RuntimeCodeContract.ps1') -ExpectedText 'Runtime code contract validation'
Assert-NoOldDefaultInAddedFiles -Paths $fullPaths

Write-Host 'P7.8F runtime code contract guardrails drop-in validation passed.'
