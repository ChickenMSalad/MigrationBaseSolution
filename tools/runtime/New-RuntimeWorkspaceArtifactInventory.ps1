[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-workspace-artifact-hygiene.sample.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path $RepoRoot 'artifacts\runtime-hygiene\workspace-artifact-inventory.md')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BasePath,

        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return [System.IO.Path]::Combine($BasePath, $Path)
}

function Test-PathHasExcludedSegment {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $false)]
        [string[]] $ExcludedSegments = @()
    )

    $parts = $Path -split '[\\/]+'
    foreach ($part in $parts) {
        foreach ($excluded in @($ExcludedSegments)) {
            if ($part -ieq $excluded) {
                return $true
            }
        }
    }

    return $false
}

$configFullPath = Resolve-FullPath -BasePath $RepoRoot -Path $ConfigurationPath
if (-not (Test-Path -LiteralPath $configFullPath)) {
    throw ('Configuration file not found: {0}' -f $configFullPath)
}

$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json
$roots = @()
foreach ($propertyName in @('artifactRoots', 'publishRoots', 'evidenceRoots')) {
    $property = $config.PSObject.Properties[$propertyName]
    if ($null -ne $property) {
        $roots += @($property.Value)
    }
}

$excludeSegments = @()
$excludeProperty = $config.PSObject.Properties['excludePathFragments']
if ($null -ne $excludeProperty) {
    $excludeSegments = @($excludeProperty.Value)
}

$records = @()
foreach ($root in ($roots | Sort-Object -Unique)) {
    $fullRoot = Resolve-FullPath -BasePath $RepoRoot -Path ([string]$root)
    if (-not (Test-Path -LiteralPath $fullRoot)) {
        continue
    }

    $items = Get-ChildItem -LiteralPath $fullRoot -Recurse -File -ErrorAction SilentlyContinue
    foreach ($item in $items) {
        if (Test-PathHasExcludedSegment -Path $item.FullName -ExcludedSegments $excludeSegments) {
            continue
        }

        $relative = $item.FullName
        if ($relative.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $relative = $relative.Substring($RepoRoot.Length).TrimStart('\', '/')
        }

        $records += [pscustomobject]@{
            Path = $relative
            Length = $item.Length
            LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        }
    }
}

$outputFullPath = Resolve-FullPath -BasePath $RepoRoot -Path $OutputPath
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$lines = New-Object System.Collections.ArrayList
[void]$lines.Add('# Runtime Workspace Artifact Inventory')
[void]$lines.Add('')
[void]$lines.Add(('- Generated UTC: {0}' -f [DateTimeOffset]::UtcNow.ToString('o')))
[void]$lines.Add(('- Repo root: {0}' -f $RepoRoot))
[void]$lines.Add(('- Files found: {0}' -f @($records).Count))
[void]$lines.Add('')
[void]$lines.Add('| Path | Bytes | LastWriteTimeUtc |')
[void]$lines.Add('| --- | ---: | --- |')

foreach ($record in ($records | Sort-Object Path)) {
    [void]$lines.Add(('| `{0}` | {1} | {2} |' -f $record.Path, $record.Length, $record.LastWriteTimeUtc))
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime workspace artifact inventory written to {0}' -f $outputFullPath)
