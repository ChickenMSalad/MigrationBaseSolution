[CmdletBinding()]
param(
    [string]$RepoRoot = ".",
    [string]$OutputPath = ".\artifacts\runtime\legacy-runtime-reference-inventory.csv"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-RepoRootPath {
    param([string]$Path)
    return (Resolve-Path -LiteralPath $Path).Path
}

function Test-IgnoredPath {
    param([string]$Path)
    $normalized = $Path -replace '/', '\'
    $parts = $normalized -split '\\'
    return ($parts -contains 'bin' -or $parts -contains 'obj' -or $parts -contains '.git' -or $parts -contains 'artifacts')
}

$resolvedRoot = Resolve-RepoRootPath -Path $RepoRoot
$terms = @(
    'dbo.MigrationWorkItems',
    'migration.MigrationWorkItems',
    'dbo.MigrationManifestRows',
    'migration.MigrationManifestRows',
    'migration.MigrationManifestRecords',
    'MigrationWorkItems',
    'MigrationManifestRows',
    'MigrationManifestRecords'
)

$extensions = @('.cs', '.sql', '.json', '.md', '.ps1', '.yml', '.yaml', '.csproj')
$rows = New-Object System.Collections.Generic.List[object]

Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File | ForEach-Object {
    $file = $_
    if (Test-IgnoredPath -Path $file.FullName) { return }
    if ($extensions -notcontains $file.Extension) { return }

    $relative = $file.FullName.Substring($resolvedRoot.Length).TrimStart('\', '/')
    $lineNumber = 0

    Get-Content -LiteralPath $file.FullName | ForEach-Object {
        $lineNumber++
        $line = $_
        foreach ($term in $terms) {
            if ($line.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $rows.Add([pscustomobject]@{
                    Path = $relative
                    LineNumber = $lineNumber
                    Term = $term
                    Line = $line.Trim()
                }) | Out-Null
            }
        }
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$rows | Sort-Object Path, LineNumber, Term | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8
Write-Host ("Legacy runtime reference inventory written to {0}" -f $OutputPath)
