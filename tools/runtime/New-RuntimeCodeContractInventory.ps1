[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$OutputPath = ".\artifacts\runtime-code-contract-inventory.csv"
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

function Test-IsSkippedPath {
    param([string]$RelativePath)

    $normalized = $RelativePath.Replace('\', '/')

    if ($normalized -like '*/bin/*') { return $true }
    if ($normalized -like '*/obj/*') { return $true }
    if ($normalized -like 'artifacts/*') { return $true }
    if ($normalized -like 'docs/*') { return $true }
    if ($normalized -like 'database/sql/operational/*') { return $true }
    if ($normalized -like 'database/sql/cloud/output.txt') { return $true }
    if ($normalized -like '*.generated.md') { return $true }

    return $false
}

$root = Get-RepoRoot -ProvidedRoot $RepoRoot
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path -Parent $outputFullPath

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$patterns = @(
    @{ Id = 'GuidWorkItemId'; Pattern = '\bGuid\??\s+(WorkItemId|workItemId)\b' },
    @{ Id = 'GuidManifestRowId'; Pattern = '\bGuid\??\s+(ManifestRowId|manifestRowId)\b' },
    @{ Id = 'GuidManifestRecordId'; Pattern = '\bGuid\??\s+(ManifestRecordId|manifestRecordId)\b' },
    @{ Id = 'LegacyTableReference'; Pattern = '\b(dbo|migration)\.MigrationWorkItems\b' },
    @{ Id = 'LegacyTableReference'; Pattern = '\b(dbo|migration)\.MigrationManifestRows\b' },
    @{ Id = 'LegacyTableReference'; Pattern = '\b(dbo|migration)\.MigrationManifestRecords\b' },
    @{ Id = 'LegacyDefaultTableName'; Pattern = 'WorkItemsTableName\s*\{\s*get;\s*set;\s*\}\s*=\s*"MigrationWorkItems"' },
    @{ Id = 'LegacyDefaultSchemaName'; Pattern = 'SchemaName\s*\{\s*get;\s*set;\s*\}\s*=\s*"dbo"' }
)

$patternText = @()
foreach ($pattern in $patterns) {
    $patternText += $pattern.Pattern
}

$extensions = @('.cs', '.csproj', '.json', '.sql', '.ps1', '.props', '.targets')
$results = New-Object System.Collections.Generic.List[object]
$files = Get-ChildItem -LiteralPath $root -Recurse -File

foreach ($file in $files) {
    if ($extensions -notcontains $file.Extension) {
        continue
    }

    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@('\', '/'))
    if (Test-IsSkippedPath -RelativePath $relative) {
        continue
    }

    $matches = Select-String -LiteralPath $file.FullName -Pattern $patternText -AllMatches
    foreach ($match in $matches) {
        foreach ($pattern in $patterns) {
            if ($match.Line -match $pattern.Pattern) {
                $results.Add([pscustomobject]@{
                    PatternId = $pattern.Id
                    Path = $relative
                    LineNumber = $match.LineNumber
                    Line = $match.Line.Trim()
                }) | Out-Null
            }
        }
    }
}

$results |
    Sort-Object Path, LineNumber, PatternId |
    Export-Csv -LiteralPath $outputFullPath -NoTypeInformation -Encoding UTF8

Write-Host ("Runtime code contract inventory written to {0}" -f $outputFullPath)
Write-Host ("Finding count: {0}" -f $results.Count)
